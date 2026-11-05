using System.Collections;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using Game.Services.Save;
using Game.Services.Wallet;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.EditMode.Services.Save
{
    /// <summary>
    /// Saves that must not lose data: unreadable files stay untouched, pause/exit writes are synchronous, and a
    /// broken or unreadable old profile doesn't stop the start.
    /// </summary>
    public sealed class SaveResilienceTests
    {
        private const string TestKey = SaveService.ChunkKeyPrefix + "test";
        private const string WalletKey = SaveService.ChunkKeyPrefix + "wallet";

        private InMemorySaveStorage _storage;
        private SaveService _service;
        private TestChunk _chunk;

        [SetUp]
        public void SetUp()
        {
            _storage = new InMemorySaveStorage();
            _service = new SaveService(_storage);
            _chunk = new TestChunk();
        }

        [TearDown]
        public void TearDown() => _chunk.Dispose();

        [Test]
        public void Save_WritesBeforeReturning()
        {
            _chunk.Set("paused", 3);

            _service.Save(_chunk);

            Assert.That(_storage.Main[TestKey], Does.Contain("paused"));
        }

        [Test]
        public void Save_WhenStoredFileCouldNotBeRead_KeepsIt()
        {
            _storage.HasReadFailure = true;
            _storage.Backup.Clear();
            LogAssert.Expect(LogType.Error, new Regex("could not be read"));
            _service.TryLoad(_chunk);
            _storage.HasReadFailure = false;
            _storage.Main[TestKey] = "{\"version\":2,\"name\":\"real progress\"}";
            LogAssert.Expect(LogType.Warning, new Regex("is not written"));
            _chunk.Set("defaults", 0);

            _service.Save(_chunk);

            Assert.That(_storage.Main[TestKey], Does.Contain("real progress"));
            Assert.That(_storage.Backup.ContainsKey(TestKey), Is.False);
        }

        [UnityTest]
        public IEnumerator SaveAsync_WhenStoredFileCouldNotBeRead_KeepsIt() => UniTask.ToCoroutine(async () =>
        {
            _storage.HasReadFailure = true;
            LogAssert.Expect(LogType.Error, new Regex("could not be read"));
            _service.TryLoad(_chunk);
            _storage.HasReadFailure = false;
            _storage.Main[TestKey] = "{\"version\":2,\"name\":\"real progress\"}";
            LogAssert.Expect(LogType.Warning, new Regex("is not written"));
            _chunk.Set("defaults", 0);

            await _service.SaveAsync(_chunk, default);

            Assert.That(_storage.Main[TestKey], Does.Contain("real progress"));
        });

        [Test]
        public void TryLoad_WhenFileIsMissing_StaysWritable()
        {
            var isLoaded = _service.TryLoad(_chunk);
            _chunk.Set("new player", 1);
            _service.Save(_chunk);

            Assert.That(isLoaded, Is.False);
            Assert.That(_storage.Main[TestKey], Does.Contain("new player"));
        }

        [UnityTest]
        public IEnumerator SaveAsync_WhenAccessIsDenied_LogsAndKeepsPreviousSave() => UniTask.ToCoroutine(async () =>
        {
            _storage.Main[TestKey] = "{\"version\":2,\"name\":\"old\"}";
            _storage.IsDenied = true;
            LogAssert.Expect(LogType.Error, new Regex("failed; the previous save is kept"));
            _chunk.Set("new", 1);

            await _service.SaveAsync(_chunk, default);

            Assert.That(_storage.Main[TestKey], Does.Contain("old"));
        });

        [Test]
        public void Save_WhenAccessIsDenied_LogsInsteadOfThrowing()
        {
            _storage.IsDenied = true;
            LogAssert.Expect(LogType.Error, new Regex("failed; the previous save is kept"));
            _chunk.Set("new", 1);

            Assert.DoesNotThrow(() => _service.Save(_chunk));
        }

        [Test]
        public void TryMigrateLegacy_WhenOneFieldHasWrongType_StartsThatChunkWithDefaultsAndKeepsOldFile()
        {
            _storage.Main[SaveService.LegacyKey] = "{ \"version\": 1, \"coins\": \"many\", \"sound\": false }";
            LogAssert.Expect(LogType.Error, new Regex("Old profile's 'wallet' can't be read"));
            var wallet = new WalletService();
            var settings = new Game.Services.Settings.SettingsService();

            var isMigrated = _service.TryMigrateLegacy(new IProfileChunk[] { wallet, settings });

            Assert.That(isMigrated, Is.True);
            Assert.That(wallet.Coins.Value, Is.EqualTo(WalletData.StartingCoins));
            Assert.That(settings.IsSoundOn, Is.False);
            Assert.That(_storage.Main.ContainsKey(WalletKey), Is.True);
            Assert.That(_storage.Deleted, Is.Empty);
            wallet.Dispose();
            settings.Dispose();
        }

        [Test]
        public void TryMigrateLegacy_WhenOldProfileCouldNotBeRead_DoesNotStartOverItsData()
        {
            _storage.HasReadFailure = true;
            LogAssert.Expect(LogType.Error, new Regex("Old profile could not be read"));

            var isMigrated = _service.TryMigrateLegacy(new IProfileChunk[] { _chunk });
            _storage.HasReadFailure = false;
            LogAssert.Expect(LogType.Warning, new Regex("is not written"));
            _service.Save(_chunk);

            Assert.That(isMigrated, Is.False);
            Assert.That(_storage.Main.ContainsKey(TestKey), Is.False);
        }

        [Test]
        public void Save_WhenMainFileUnreadableButBackupLoads_KeepsTheMainFileUntouched()
        {
            _storage.Backup[TestKey] = "{\"version\":2,\"name\":\"from backup\"}";
            _storage.HasReadFailure = true;
            LogAssert.Expect(LogType.Error, new Regex("could not be read"));
            LogAssert.Expect(LogType.Warning, new Regex("is not written"));

            var isLoaded = _service.TryLoad(_chunk);
            _storage.HasReadFailure = false;
            _chunk.Set("progress", 5);
            _service.Save(_chunk);

            Assert.That(isLoaded, Is.True);
            Assert.That(_chunk.Name, Is.EqualTo("progress"));
            // The unreadable main file may be intact (or newer): the chunk stays read-only for this session.
            Assert.That(_storage.Main.ContainsKey(TestKey), Is.False);
        }

        [Test]
        public void TryMigrateLegacy_WhenChunkIsReadOnly_DoesNotOverwriteItAndKeepsOldFile()
        {
            _storage.Main[SaveService.LegacyKey] = "{ \"version\": 1, \"name\": \"stale\" }";
            _storage.HasReadFailure = true;
            LogAssert.Expect(LogType.Error, new Regex("could not be read"));
            _service.TryLoad(_chunk);
            _storage.HasReadFailure = false;
            _storage.Main[TestKey] = "{\"version\":2,\"name\":\"real progress\"}";

            var isMigrated = _service.TryMigrateLegacy(new IProfileChunk[] { _chunk });

            Assert.That(isMigrated, Is.True);
            Assert.That(_storage.Main[TestKey], Does.Contain("real progress"));
            Assert.That(_storage.Deleted, Is.Empty);
        }

        [Test]
        public void TryMigrateLegacy_WhenOldProfileIsNotJson_ReturnsFalse()
        {
            _storage.Main[SaveService.LegacyKey] = "{ broken";
            LogAssert.Expect(LogType.Error, new Regex("Old profile can't be read"));

            var isMigrated = _service.TryMigrateLegacy(new IProfileChunk[] { _chunk });

            Assert.That(isMigrated, Is.False);
        }
    }
}
