using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Game.Services.Save
{
    /// <summary>
    /// Default <see cref="ISaveService"/>. Each chunk is a JSON object with a <c>version</c> field next to the
    /// chunk's own fields, stored under <c>profile_&lt;key&gt;</c>.
    /// </summary>
    public sealed class SaveService : ISaveService, IDisposable
    {
        /// <summary>
        /// Storage key of the single-file profile written by builds before the profile was split into chunks.
        /// </summary>
        public const string LegacyKey = "profile";

        /// <summary>
        /// Prefix of every chunk's storage key.
        /// </summary>
        public const string ChunkKeyPrefix = "profile_";

        private const string VersionField = "version";
        private const int LegacyVersion = 1;

        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            Culture = CultureInfo.InvariantCulture,
            TypeNameHandling = TypeNameHandling.None,
        };

        private static readonly JsonSerializer Serializer = JsonSerializer.Create(Settings);

        private readonly ISaveStorage _storage;
        private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1, 1);
        private readonly Dictionary<string, string> _pending = new Dictionary<string, string>();

        /// <summary>
        /// Creates the service.
        /// </summary>
        /// <param name="storage">Where save text is kept.</param>
        public SaveService(ISaveStorage storage)
        {
            _storage = storage;
        }

        /// <inheritdoc/>
        public bool TryLoad(IProfileChunk chunk)
        {
            var key = StorageKey(chunk);
            var main = TryParse(chunk, _storage.ReadMain(key), out var mainError);
            if (main != null)
            {
                chunk.Restore(main);
                return true;
            }

            if (mainError != null)
            {
                Debug.LogError($"Save '{key}' is broken, trying the backup: {mainError}");
                _storage.Quarantine(key);
            }

            var backup = TryParse(chunk, _storage.ReadBackup(key), out var backupError);
            if (backup != null)
            {
                chunk.Restore(backup);
                return true;
            }

            if (backupError != null) Debug.LogError($"Save '{key}' backup is broken too: {backupError}");
            return false;
        }

        /// <inheritdoc/>
        public bool TryMigrateLegacy(IReadOnlyList<IProfileChunk> chunks)
        {
            var root = ReadLegacy();
            if (root == null) return false;

            // The chunks keep the field names of the old profile, so each one reads its own fields from the whole
            // object and ignores the rest.
            foreach (var chunk in chunks)
            {
                chunk.Restore(Deserialize(chunk, (JObject)root.DeepClone(), LegacyVersion));
            }

            if (TryWriteAll(chunks)) _storage.Delete(LegacyKey);
            return true;
        }

        /// <inheritdoc/>
        public async UniTask SaveAsync(IProfileChunk chunk, CancellationToken ct)
        {
            var key = StorageKey(chunk);
            // Snapshot on the caller's (main) thread; only the latest snapshot is written when saves pile up.
            var json = Serialize(chunk);
            lock (_pending)
            {
                _pending[key] = json;
            }

            await _writeLock.WaitAsync(ct);
            try
            {
                // The previous writer releases the lock from the thread pool, so this may run there too.
                lock (_pending)
                {
                    if (!_pending.Remove(key, out json)) return;
                }

                await UniTask.SwitchToThreadPool();
                _storage.Write(key, json);
            }
            catch (IOException e)
            {
                Debug.LogError($"Saving '{key}' failed; the previous save is kept. {e.Message}");
            }
            finally
            {
                _writeLock.Release();
                await UniTask.SwitchToMainThread();
            }
        }

        /// <inheritdoc/>
        public void Dispose() => _writeLock.Dispose();

        /// <summary>
        /// Parses, migrates and deserializes stored chunk text.
        /// </summary>
        /// <param name="chunk">Chunk the text belongs to; gives the data type, version and migrations.</param>
        /// <param name="json">Raw stored text; null or empty means nothing is stored.</param>
        /// <param name="error">The parse or version problem, or null.</param>
        /// <returns>An instance of the chunk's data type, or null when missing or broken.</returns>
        public static object TryParse(IProfileChunk chunk, string json, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(json)) return null;

            try
            {
                var data = JObject.Parse(json);
                var version = data.Value<int?>(VersionField) ?? 1;
                data.Remove(VersionField);
                return Deserialize(chunk, data, version);
            }
            catch (Exception e)
            {
                error = e.Message;
                return null;
            }
        }

        /// <summary>
        /// Serializes a chunk's current data with its version.
        /// </summary>
        /// <param name="chunk">Chunk to serialize.</param>
        /// <returns>Indented JSON text.</returns>
        public static string Serialize(IProfileChunk chunk)
        {
            var data = JObject.FromObject(chunk.Data, Serializer);
            data.AddFirst(new JProperty(VersionField, chunk.Version));
            return data.ToString(Formatting.Indented);
        }

        private static string StorageKey(IProfileChunk chunk) => ChunkKeyPrefix + chunk.Key;

        private static object Deserialize(IProfileChunk chunk, JObject data, int version)
        {
            if (version > chunk.Version)
            {
                throw new NotSupportedException(
                    $"'{chunk.Key}' version {version} is newer than {chunk.Version} supported by this build.");
            }

            if (version < chunk.Version) chunk.Migrate(data, version);
            return data.ToObject(chunk.DataType, Serializer);
        }

        private JObject ReadLegacy()
        {
            foreach (var json in new[] { _storage.ReadMain(LegacyKey), _storage.ReadBackup(LegacyKey) })
            {
                if (string.IsNullOrWhiteSpace(json)) continue;

                try
                {
                    var root = JObject.Parse(json);
                    var version = root.Value<int?>(VersionField) ?? LegacyVersion;
                    if (version > LegacyVersion) throw new NotSupportedException($"Profile version {version} is unknown.");

                    root.Remove(VersionField);
                    return root;
                }
                catch (Exception e)
                {
                    Debug.LogError("Old profile can't be read: " + e.Message);
                }
            }

            return null;
        }

        private bool TryWriteAll(IReadOnlyList<IProfileChunk> chunks)
        {
            // One-time migration at startup: written synchronously so the old file is deleted only after every
            // chunk is safely on disk.
            try
            {
                foreach (var chunk in chunks)
                {
                    _storage.Write(StorageKey(chunk), Serialize(chunk));
                }

                return true;
            }
            catch (IOException e)
            {
                Debug.LogError("Splitting the old profile failed; it is kept for the next start. " + e.Message);
                return false;
            }
        }
    }
}
