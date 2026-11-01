using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Game.Services.Save
{
    /// <summary>
    /// Loads and writes the player profile: JSON with migrations, atomic writes with a backup, and recovery from
    /// a broken main file.
    /// </summary>
    public sealed class SaveService : ISaveService, IDisposable
    {
        private const string ProfileKey = "profile";

        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            Culture = System.Globalization.CultureInfo.InvariantCulture,
            TypeNameHandling = TypeNameHandling.None,
        };

        private readonly ISaveStorage _storage;
        private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1, 1);
        private string _pendingJson;

        /// <summary>
        /// Creates the service.
        /// </summary>
        /// <param name="storage">Where save text is kept.</param>
        public SaveService(ISaveStorage storage)
        {
            _storage = storage;
        }

        /// <inheritdoc/>
        public PlayerProfileData Load()
        {
            var main = TryParse(_storage.ReadMain(ProfileKey), out var mainError);
            if (main != null) return main;

            if (mainError != null)
            {
                Debug.LogError("Profile is broken, trying the backup: " + mainError);
                _storage.Quarantine(ProfileKey);
            }

            var backup = TryParse(_storage.ReadBackup(ProfileKey), out var backupError);
            if (backup != null) return backup;

            if (backupError != null) Debug.LogError("Profile backup is broken too, starting fresh: " + backupError);
            return PlayerProfileData.CreateNew();
        }

        /// <inheritdoc/>
        public async UniTask SaveAsync(PlayerProfileData data, CancellationToken ct)
        {
            // Snapshot on the caller's (main) thread; only the latest snapshot is written when saves pile up.
            _pendingJson = JsonConvert.SerializeObject(data, Settings);
            await _writeLock.WaitAsync(ct);
            try
            {
                var json = _pendingJson;
                if (json == null) return;

                _pendingJson = null;
                await UniTask.SwitchToThreadPool();
                _storage.Write(ProfileKey, json);
            }
            catch (System.IO.IOException e)
            {
                Debug.LogError("Saving the profile failed; the previous save is kept. " + e.Message);
            }
            finally
            {
                _writeLock.Release();
                await UniTask.SwitchToMainThread();
            }
        }

        /// <inheritdoc/>
        public void Delete() => _storage.Delete(ProfileKey);

        /// <inheritdoc/>
        public void Dispose() => _writeLock.Dispose();

        /// <summary>
        /// Parses and migrates save text.
        /// </summary>
        /// <param name="json">Raw save text; null or empty means no save.</param>
        /// <param name="error">The parse or version problem, or null.</param>
        /// <returns>The profile, or null when missing or broken.</returns>
        public static PlayerProfileData TryParse(string json, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(json)) return null;

            try
            {
                var root = JObject.Parse(json);
                SaveMigrations.Migrate(root);
                return root.ToObject<PlayerProfileData>(JsonSerializer.Create(Settings));
            }
            catch (Exception e)
            {
                error = e.Message;
                return null;
            }
        }
    }
}
