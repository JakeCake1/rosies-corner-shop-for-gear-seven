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
    /// <remarks>Not disposable on purpose: a write may still be running on the thread pool when the app
    /// shuts down, and the write lock (a plain monitor, no wait handle) needs no disposal.</remarks>
    public sealed class SaveService : ISaveService
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
        // Held while a file is written; a monitor, not a semaphore: the main thread takes it synchronously in Save,
        // and a semaphore's waiters resume on the main thread, which a flush on pause would block forever.
        private readonly object _ioLock = new object();
        private readonly Dictionary<string, string> _pending = new Dictionary<string, string>();
        // Keys in the order they were saved: a monitor is not fair, so writes follow this queue, not the lock order.
        private readonly Queue<string> _order = new Queue<string>();
        // Chunks whose stored data could not be read this session: saving them would overwrite data that may be fine.
        private readonly HashSet<string> _readOnly = new HashSet<string>();
        private readonly HashSet<string> _warned = new HashSet<string>();

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
            var mainText = ReadSafely(key, false);
            var main = TryParse(chunk, mainText, out var mainError);
            if (main != null)
            {
                chunk.Restore(main);
                return true;
            }

            if (mainText != null && string.IsNullOrWhiteSpace(mainText))
            {
                // An empty file is a broken save too: left in place, the first write would move it over the backup.
                mainError = "the file is empty";
            }

            if (mainError != null)
            {
                Debug.LogError($"Save '{key}' is broken, trying the backup: {mainError}");
                try
                {
                    _storage.Quarantine(key);
                }
                catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
                {
                    // The file stays where it is. It may come from a newer build (or be locked), so the first save
                    // of this session must not overwrite it.
                    _readOnly.Add(key);
                    Debug.LogError($"Save '{key}' could not be quarantined, it won't be overwritten this session: {e.Message}");
                }
            }

            var backup = TryParse(chunk, ReadSafely(key, true), out var backupError);
            if (backup != null)
            {
                // Stays read-only when the main file could not be read or moved aside: it may be intact (or newer)
                // and a first write would replace it.
                chunk.Restore(backup);
                return true;
            }

            if (backupError != null)
            {
                Debug.LogError($"Save '{key}' backup is broken too: {backupError}");
            }
            return false;
        }

        /// <inheritdoc/>
        public bool TryMigrateLegacy(IReadOnlyList<IProfileChunk> chunks)
        {
            var root = ReadLegacy(chunks);
            if (root == null)
            {
                return false;
            }

            // The chunks keep the field names of the old profile, so each one reads its own fields from the whole
            // object and ignores the rest. A chunk that can't be read starts with defaults; the others are not
            // affected, and the old file is kept for investigation.
            var isComplete = true;
            foreach (var chunk in chunks)
            {
                if (_readOnly.Contains(StorageKey(chunk)))
                {
                    // Its stored file is unreadable: old data in memory would differ from what the next start loads.
                    chunk.Restore(null);
                    continue;
                }

                try
                {
                    chunk.Restore(Deserialize(chunk, (JObject)root.DeepClone(), LegacyVersion));
                }
                catch (Exception e)
                {
                    isComplete = false;
                    Debug.LogError($"Old profile's '{chunk.Key}' can't be read, starting it with defaults: {e.Message}");
                    chunk.Restore(null);
                }
            }

            var isWritten = TryWriteAll(chunks, out var isSkipped);
            if (isWritten && !isSkipped)
            {
                // An incomplete migration moves the file aside instead of keeping it as a source: a chunk that goes
                // missing later (or is added by a future build) must start with defaults, not with old coins.
                RetireLegacy(isComplete);
            }
            return true;
        }

        /// <inheritdoc/>
        public async UniTask SaveAsync(IProfileChunk chunk, CancellationToken ct)
        {
            var key = StorageKey(chunk);
            if (IsReadOnly(key))
            {
                return;
            }

            // Snapshot on the caller's (main) thread; only the latest snapshot is written when saves pile up.
            var json = Serialize(chunk);
            lock (_pending)
            {
                _pending[key] = json;
                _order.Enqueue(key);
            }

            // A write of its own per call, never queued behind the main thread: after the app is paused the OS may
            // stop the player loop, and a continuation waiting for it would never run.
            await UniTask.RunOnThreadPool(WritePending, cancellationToken: ct);
        }

        /// <inheritdoc/>
        public void Save(IProfileChunk chunk)
        {
            var key = StorageKey(chunk);
            if (IsReadOnly(key))
            {
                return;
            }

            var json = Serialize(chunk);
            lock (_ioLock)
            {
                // Newer than anything queued for this key.
                lock (_pending)
                {
                    _pending.Remove(key);
                }

                TryWrite(key, json);
            }
        }

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
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

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

        private string ReadSafely(string key, bool isBackup)
        {
            try
            {
                return isBackup ? _storage.ReadBackup(key) : _storage.ReadMain(key);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                // Not the same as a missing file: the data may be intact, so it must not be replaced by defaults.
                _readOnly.Add(key);
                Debug.LogError($"Save '{key}' could not be read (backup: {isBackup}), it won't be overwritten this " +
                               $"session: {e.Message}");
                return null;
            }
        }

        private bool IsReadOnly(string key)
        {
            if (!_readOnly.Contains(key))
            {
                return false;
            }

            if (_warned.Add(key))
            {
                Debug.LogWarning($"Save '{key}' is not written: it could not be read at startup.");
            }
            return true;
        }

        // Writes everything queued so far, oldest first, so a chunk saved before another one (the wallet before the
        // album that marks its bonus done) reaches the disk before it, whichever thread gets the lock first.
        private void WritePending()
        {
            lock (_ioLock)
            {
                while (true)
                {
                    string key;
                    string json;
                    lock (_pending)
                    {
                        // Taken inside the I/O lock: a snapshot can't be written after a newer one.
                        if (!_order.TryDequeue(out key))
                        {
                            return;
                        }

                        if (!_pending.Remove(key, out json))
                        {
                            continue;
                        }
                    }

                    TryWrite(key, json);
                }
            }
        }

        private void TryWrite(string key, string json)
        {
            try
            {
                _storage.Write(key, json);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Debug.LogError($"Saving '{key}' failed; the previous save is kept. {e.Message}");
            }
        }

        private static string StorageKey(IProfileChunk chunk) => ChunkKeyPrefix + chunk.Key;

        private static object Deserialize(IProfileChunk chunk, JObject data, int version)
        {
            if (version > chunk.Version)
            {
                throw new NotSupportedException(
                    $"'{chunk.Key}' version {version} is newer than {chunk.Version} supported by this build.");
            }

            if (version < chunk.Version)
            {
                chunk.Migrate(data, version);
            }
            return data.ToObject(chunk.DataType, Serializer);
        }

        private JObject ReadLegacy(IReadOnlyList<IProfileChunk> chunks)
        {
            foreach (var isBackup in new[] { false, true })
            {
                string json;
                try
                {
                    json = isBackup ? _storage.ReadBackup(LegacyKey) : _storage.ReadMain(LegacyKey);
                }
                catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
                {
                    // The old profile may be intact: keep the chunks from writing defaults over its data.
                    Debug.LogError($"Old profile could not be read, chunks are not saved this session: {e.Message}");
                    foreach (var chunk in chunks)
                    {
                        _readOnly.Add(StorageKey(chunk));
                    }
                    return null;
                }

                if (string.IsNullOrWhiteSpace(json))
                {
                    continue;
                }

                try
                {
                    var root = JObject.Parse(json);
                    var version = root.Value<int?>(VersionField) ?? LegacyVersion;
                    if (version > LegacyVersion)
                    {
                        throw new NotSupportedException($"Profile version {version} is unknown.");
                    }

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

        private bool TryWriteAll(IReadOnlyList<IProfileChunk> chunks, out bool isSkipped)
        {
            isSkipped = false;
            // One-time migration at startup: written synchronously so the old file is deleted only after every
            // chunk is safely on disk.
            try
            {
                foreach (var chunk in chunks)
                {
                    var key = StorageKey(chunk);
                    if (_readOnly.Contains(key))
                    {
                        // Its stored file is unreadable, not missing: the old profile must not overwrite it.
                        isSkipped = true;
                        continue;
                    }

                    _storage.Write(key, Serialize(chunk));
                }

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError("Splitting the old profile failed; it is kept for the next start. " + e.Message);
                return false;
            }
        }

        private void RetireLegacy(bool isComplete)
        {
            try
            {
                if (isComplete)
                {
                    _storage.Delete(LegacyKey);
                }
                else
                {
                    _storage.Quarantine(LegacyKey);
                }
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                // Every chunk file exists now; a chunk that later goes missing would be refilled from this file, so
                // it stays a leftover to clean up.
                Debug.LogError("The old profile could not be retired: " + e.Message);
            }
        }
    }
}
