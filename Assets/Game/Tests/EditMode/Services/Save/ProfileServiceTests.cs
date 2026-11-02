using System;
using System.Collections.Generic;
using Game.Services.Experiments;
using Game.Services.Save;
using Game.Services.Settings;
using Game.Services.Wallet;
using NUnit.Framework;
using UniRx;

namespace Game.Tests.EditMode.Services.Save
{
    /// <summary>
    /// Loading every chunk, saving only the chunk that changed, reset and duplicate keys.
    /// </summary>
    public sealed class ProfileServiceTests
    {
        private RecordingSaveService _save;
        private WalletService _wallet;
        private SettingsService _settings;
        private ExperimentsService _experiments;
        private ProfileService _profile;

        [SetUp]
        public void SetUp()
        {
            _save = new RecordingSaveService();
            _wallet = new WalletService();
            _settings = new SettingsService();
            _experiments = new ExperimentsService();
            _profile = new ProfileService(_save, new IProfileChunk[] { _wallet, _settings, _experiments });
        }

        [TearDown]
        public void TearDown()
        {
            _profile?.Dispose();
            _wallet.Dispose();
            _settings.Dispose();
            _experiments.Dispose();
        }

        [Test]
        public void Load_WhenMigrationWasPartiallyWritten_RestoresMissingChunksFromLegacy()
        {
            _save.Storage.Main["profile_wallet"] = "{\"version\":1,\"coins\":5}";
            _save.Storage.Main[SaveService.LegacyKey] = "{\"version\":1,\"coins\":77,\"music\":false,\"offer\":\"slot\"}";

            _profile.Load();

            Assert.That(_wallet.Coins.Value, Is.EqualTo(5));
            Assert.That(_settings.IsMusicOn, Is.False);
            Assert.That(_experiments.LossOffer, Is.EqualTo("slot"));
            Assert.That(_save.Storage.Deleted, Does.Contain(SaveService.LegacyKey));
        }

        [Test]
        public void Load_RestoresEachChunkFromItsOwnFile()
        {
            _save.Storage.Main["profile_wallet"] = "{\"version\":1,\"coins\":9}";
            _save.Storage.Main["profile_settings"] = "{\"version\":1,\"music\":false}";

            _profile.Load();

            Assert.That(_wallet.Coins.Value, Is.EqualTo(9));
            Assert.That(_settings.IsMusicOn, Is.False);
            Assert.That(_experiments.Curve, Is.EqualTo("new"));
        }

        [Test]
        public void Load_WhenOneChunkIsBroken_KeepsTheOthers()
        {
            _save.Storage.Main["profile_wallet"] = "{ broken";
            _save.Storage.Main["profile_settings"] = "{\"version\":1,\"sound\":false}";
            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Error,
                new System.Text.RegularExpressions.Regex("profile_wallet"));

            _profile.Load();

            Assert.That(_wallet.Coins.Value, Is.EqualTo(WalletData.StartingCoins));
            Assert.That(_settings.IsSoundOn, Is.False);
        }

        [Test]
        public void Load_WhenNoChunkIsStored_SplitsOldProfile()
        {
            _save.Storage.Main[SaveService.LegacyKey] = "{\"version\":1,\"coins\":77,\"offer\":\"slot\"}";

            _profile.Load();

            Assert.That(_wallet.Coins.Value, Is.EqualTo(77));
            Assert.That(_experiments.LossOffer, Is.EqualTo("slot"));
        }

        [Test]
        public void Load_WhenChunksAreStored_IgnoresOldProfile()
        {
            _save.Storage.Main["profile_wallet"] = "{\"version\":1,\"coins\":5}";
            _save.Storage.Main[SaveService.LegacyKey] = "{\"version\":1,\"coins\":77}";

            _profile.Load();

            Assert.That(_wallet.Coins.Value, Is.EqualTo(5));
        }

        [Test]
        public void ChunkChange_SavesOnlyThatChunkAndNotifies()
        {
            _profile.Load();
            var notified = 0;
            using (_profile.Changed.Subscribe(_ => notified++))
            {
                _wallet.Add(10);
            }

            Assert.That(_save.Saved, Is.EqualTo(new[] { "wallet" }));
            Assert.That(notified, Is.EqualTo(1));
        }

        [Test]
        public void ResetProgress_ResetsProgressButKeepsSettingsAndExperiments()
        {
            _profile.Load();
            _wallet.Add(500);
            _settings.SetMusicOn(false);
            _experiments.SetCurve("orig");
            _save.Saved.Clear();

            _profile.ResetProgress();

            Assert.That(_wallet.Coins.Value, Is.EqualTo(WalletData.StartingCoins));
            Assert.That(_settings.IsMusicOn, Is.False);
            Assert.That(_experiments.Curve, Is.EqualTo("orig"));
            Assert.That(_save.Saved, Is.EqualTo(new[] { "wallet" }));
        }

        [Test]
        public void SaveAll_BeforeLoad_DoesNotOverwriteStoredChunks()
        {
            _profile.SaveAll();
            _wallet.Add(10);

            Assert.That(_save.Saved, Is.Empty);
        }

        [Test]
        public void SaveAll_SavesEveryChunk()
        {
            _profile.Load();

            _profile.SaveAll();

            Assert.That(_save.Saved, Is.EqualTo(new[] { "wallet", "settings", "experiments" }));
        }

        [Test]
        public void Dispose_WhenASaveIsStillQueued_DoesNotCancelIt()
        {
            _profile.Load();
            _wallet.Add(10);

            _profile.Dispose();
            _profile = null;

            Assert.That(_save.Tokens, Has.Count.EqualTo(1));
            Assert.That(_save.Tokens[0].IsCancellationRequested, Is.False,
                "app shutdown must not drop the last change of a chunk waiting behind another write");
        }

        [Test]
        public void Constructor_WhenTwoChunksShareAKey_Throws()
        {
            var duplicate = new WalletService();
            var chunks = new List<IProfileChunk> { _wallet, duplicate };

            Assert.That(() => new ProfileService(_save, chunks), Throws.TypeOf<InvalidOperationException>());
            duplicate.Dispose();
        }
    }
}
