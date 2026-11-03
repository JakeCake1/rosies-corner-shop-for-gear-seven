using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using Game.Services.Save;
using Game.Services.Settings;
using Game.Services.Wallet;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.EditMode.Services.Save
{
    /// <summary>
    /// Per-chunk round trip, migrations, recovery from broken files and splitting the old single-file profile.
    /// </summary>
    public sealed class SaveServiceTests
    {
        private const string TestKey = SaveService.ChunkKeyPrefix + "test";

        // A profile.json written by builds before the profile was split into chunks.
        private const string LegacyProfileV1 = @"{
  ""version"": 1, ""coins"": 420, ""stars"": { ""1"": 3, ""2"": 1 }, ""found"": [17, 4], ""boost"": { ""pick"": 2 },
  ""pre"": { ""basket"": 1 }, ""got"": [""undo""], ""failedLv"": [2], ""tips"": [""counter""], ""famDone"": [],
  ""flags"": [""freeBasket""], ""albumNew"": 2, ""shopName"": ""Maple"", ""curve"": ""orig"", ""offer"": ""slot"",
  ""sound"": false, ""music"": true, ""haptic"": false
}";

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
        public void TryLoad_WithoutSave_ReturnsFalseAndKeepsDefaults()
        {
            var isLoaded = _service.TryLoad(_chunk);

            Assert.That(isLoaded, Is.False);
            Assert.That(_chunk.Name, Is.EqualTo("none"));
        }

        [Test]
        public void TryLoad_WhenMainReadFails_UsesBackup()
        {
            _storage.HasReadFailure = true;
            _storage.Backup[TestKey] = "{\"version\":2,\"name\":\"backup\"}";
            LogAssert.Expect(LogType.Error, new Regex("could not be read"));

            var isLoaded = _service.TryLoad(_chunk);

            Assert.That(isLoaded, Is.True);
            Assert.That(_chunk.Name, Is.EqualTo("backup"));
        }

        [Test]
        public void TryLoad_WhenQuarantineFails_UsesBackup()
        {
            _storage.Main[TestKey] = "{ broken";
            _storage.HasQuarantineFailure = true;
            _storage.Backup[TestKey] = "{\"version\":2,\"name\":\"backup\"}";
            LogAssert.Expect(LogType.Error, new Regex("is broken, trying the backup"));
            LogAssert.Expect(LogType.Error, new Regex("could not be quarantined"));

            var isLoaded = _service.TryLoad(_chunk);

            Assert.That(isLoaded, Is.True);
            Assert.That(_chunk.Name, Is.EqualTo("backup"));
        }

        [UnityTest]
        public IEnumerator SaveAsync_ThenTryLoad_RestoresChunk() => UniTask.ToCoroutine(async () =>
        {
            _chunk.Set("shop", 7);

            await _service.SaveAsync(_chunk, default);
            var loaded = new TestChunk();
            _service.TryLoad(loaded);

            Assert.That(loaded.Name, Is.EqualTo("shop"));
            Assert.That(loaded.Count, Is.EqualTo(7));
            Assert.That(_storage.Main[TestKey], Does.Contain("\"version\": 2"));
            loaded.Dispose();
        });

        [UnityTest]
        public IEnumerator SaveAsync_WhenDiskIsFull_KeepsPreviousSaveAndLogs() => UniTask.ToCoroutine(async () =>
        {
            _storage.Main[TestKey] = "{\"version\":2,\"name\":\"old\"}";
            _storage.IsFull = true;
            LogAssert.Expect(LogType.Error, new Regex("failed; the previous save is kept"));
            _chunk.Set("new", 1);

            await _service.SaveAsync(_chunk, default);

            Assert.That(_storage.Main[TestKey], Does.Contain("old"));
        });

        [Test]
        public void TryLoad_WhenSaveIsOlder_MigratesIt()
        {
            _storage.Main[TestKey] = "{\"version\":1,\"title\":\"legacy\",\"count\":3}";

            _service.TryLoad(_chunk);

            Assert.That(_chunk.Name, Is.EqualTo("legacy"));
            Assert.That(_chunk.Count, Is.EqualTo(3));
        }

        [Test]
        public void TryLoad_WhenMainIsBroken_UsesBackupAndQuarantinesMain()
        {
            _storage.Main[TestKey] = "{ not json";
            _storage.Backup[TestKey] = "{\"version\":2,\"name\":\"backup\"}";
            LogAssert.Expect(LogType.Error, new Regex("is broken, trying the backup"));

            var isLoaded = _service.TryLoad(_chunk);

            Assert.That(isLoaded, Is.True);
            Assert.That(_chunk.Name, Is.EqualTo("backup"));
            Assert.That(_storage.Quarantined, Is.EqualTo(new[] { TestKey }));
        }

        [Test]
        public void TryLoad_WhenMainIsEmpty_QuarantinesItAndUsesBackup()
        {
            _storage.Main[TestKey] = "  ";
            _storage.Backup[TestKey] = "{\"version\":2,\"name\":\"backup\"}";
            LogAssert.Expect(LogType.Error, new Regex("is broken, trying the backup"));

            var isLoaded = _service.TryLoad(_chunk);

            Assert.That(isLoaded, Is.True);
            Assert.That(_chunk.Name, Is.EqualTo("backup"));
            Assert.That(_storage.Quarantined, Is.EqualTo(new[] { TestKey }));
        }

        [UnityTest]
        public IEnumerator SaveAsync_WhenBrokenMainCouldNotBeQuarantined_DoesNotOverwriteIt() => UniTask.ToCoroutine(async () =>
        {
            const string newerSave = "{\"version\":99,\"name\":\"future\"}";
            _storage.Main[TestKey] = newerSave;
            _storage.HasQuarantineFailure = true;
            _storage.Backup[TestKey] = "{\"version\":2,\"name\":\"backup\"}";
            LogAssert.Expect(LogType.Error, new Regex("is broken, trying the backup"));
            LogAssert.Expect(LogType.Error, new Regex("could not be quarantined"));
            LogAssert.Expect(LogType.Warning, new Regex("is not written"));
            _service.TryLoad(_chunk);
            _chunk.Set("new", 1);

            await _service.SaveAsync(_chunk, default);

            Assert.That(_storage.Main[TestKey], Is.EqualTo(newerSave));
        });

        [UnityTest]
        public IEnumerator SaveAsync_WhenSavesOverlap_WritesInCallOrder() => UniTask.ToCoroutine(async () =>
        {
            var wallet = new WalletService();
            var settings = new SettingsService();

            await UniTask.WhenAll(_service.SaveAsync(wallet, default), _service.SaveAsync(settings, default),
                _service.SaveAsync(wallet, default));

            Assert.That(_storage.WriteOrder.First(), Is.EqualTo("profile_wallet"));
            Assert.That(_storage.WriteOrder.IndexOf("profile_settings"), Is.GreaterThan(0));
            wallet.Dispose();
            settings.Dispose();
        });

        [Test]
        public void TryParse_WhenSaveIsFromNewerBuild_Refuses()
        {
            var data = SaveService.TryParse(_chunk, "{\"version\":99,\"name\":\"future\"}", out var error);

            Assert.That(data, Is.Null);
            Assert.That(error, Does.Contain("newer"));
        }

        [Test]
        public void TryParse_WhenFieldsAreMissing_UsesDefaults()
        {
            var data = (TestChunkPayload)SaveService.TryParse(_chunk, "{\"count\":12}", out _);

            Assert.That(data.Count, Is.EqualTo(12));
            Assert.That(data.Name, Is.EqualTo("none"));
        }

        [Test]
        public void TryMigrateLegacy_SplitsOldProfileIntoChunksAndDeletesIt()
        {
            _storage.Main[SaveService.LegacyKey] = LegacyProfileV1;
            var wallet = new WalletService();
            var settings = new SettingsService();

            var isMigrated = _service.TryMigrateLegacy(new IProfileChunk[] { wallet, settings });

            Assert.That(isMigrated, Is.True);
            Assert.That(wallet.Coins.Value, Is.EqualTo(420));
            Assert.That(settings.IsSoundOn, Is.False);
            Assert.That(settings.IsHapticsOn, Is.False);
            Assert.That(_storage.Main.Keys, Is.EquivalentTo(new[] { "profile_wallet", "profile_settings" }));
            Assert.That(_storage.Deleted, Is.EqualTo(new[] { SaveService.LegacyKey }));
            wallet.Dispose();
            settings.Dispose();
        }

        [Test]
        public void TryMigrateLegacy_WhenWritingFails_KeepsOldProfile()
        {
            _storage.Main[SaveService.LegacyKey] = LegacyProfileV1;
            _storage.IsFull = true;
            LogAssert.Expect(LogType.Error, new Regex("Splitting the old profile failed"));

            var isMigrated = _service.TryMigrateLegacy(new IProfileChunk[] { _chunk });

            Assert.That(isMigrated, Is.True);
            Assert.That(_storage.Main.ContainsKey(SaveService.LegacyKey), Is.True);
        }

        [Test]
        public void TryMigrateLegacy_WithoutOldProfile_ReturnsFalse()
        {
            var isMigrated = _service.TryMigrateLegacy(new IProfileChunk[] { _chunk });

            Assert.That(isMigrated, Is.False);
        }
    }
}
