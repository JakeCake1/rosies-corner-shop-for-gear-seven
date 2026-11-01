using System.Collections;
using Cysharp.Threading.Tasks;
using Game.Services.Save;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.EditMode.Services.Save
{
    /// <summary>
    /// Save/load round trip, recovery from a broken file and refusal of saves from a newer build.
    /// </summary>
    public sealed class SaveServiceTests
    {
        private InMemorySaveStorage _storage;
        private SaveService _service;

        [SetUp]
        public void SetUp()
        {
            _storage = new InMemorySaveStorage();
            _service = new SaveService(_storage);
        }

        [TearDown]
        public void TearDown() => _service.Dispose();

        [Test]
        public void Load_WithoutSave_ReturnsFreshProfile()
        {
            var data = _service.Load();

            Assert.That(data.Coins, Is.EqualTo(PlayerProfileData.StartingCoins));
            Assert.That(data.Version, Is.EqualTo(PlayerProfileData.CurrentVersion));
            Assert.That(data.Curve, Is.EqualTo("new"));
        }

        [UnityTest]
        public IEnumerator SaveAsync_ThenLoad_RestoresData() => UniTask.ToCoroutine(async () =>
        {
            var data = PlayerProfileData.CreateNew();
            data.Coins = 420;
            data.Stars[3] = 2;
            data.Found.Add(17);

            await _service.SaveAsync(data, default);
            var loaded = _service.Load();

            Assert.That(loaded.Coins, Is.EqualTo(420));
            Assert.That(loaded.Stars[3], Is.EqualTo(2));
            Assert.That(loaded.Found, Is.EqualTo(new[] { 17 }));
        });

        [Test]
        public void Load_WhenMainIsBroken_UsesBackupAndQuarantinesMain()
        {
            _storage.Main["profile"] = "{ not json";
            _storage.Backup["profile"] = "{\"version\":1,\"coins\":77}";
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Profile is broken"));

            var data = _service.Load();

            Assert.That(data.Coins, Is.EqualTo(77));
            Assert.That(_storage.Quarantined, Is.EqualTo(1));
        }

        [Test]
        public void TryParse_WhenSaveIsFromNewerBuild_Refuses()
        {
            var data = SaveService.TryParse("{\"version\":99,\"coins\":5}", out var error);

            Assert.That(data, Is.Null);
            Assert.That(error, Does.Contain("newer"));
        }

        [Test]
        public void TryParse_WhenFieldsAreMissing_UsesDefaults()
        {
            var data = SaveService.TryParse("{\"coins\":12}", out _);

            Assert.That(data.Coins, Is.EqualTo(12));
            Assert.That(data.IsMusicOn, Is.True);
            Assert.That(data.Stars, Is.Empty);
        }
    }
}
