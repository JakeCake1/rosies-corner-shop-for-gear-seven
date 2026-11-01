using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Services.Content;
using Game.Services.Progress;
using Game.Tests.EditMode.Features.Gameplay;
using Game.Tests.EditMode.Services.Save;
using Game.Services.Save;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode.Services.Progress
{
    /// <summary>
    /// Coins, stars and the album's department bonus.
    /// </summary>
    public sealed class ProgressServiceTests
    {
        private ItemCatalog _catalog;
        private SaveService _save;
        private ProgressService _progress;

        [SetUp]
        public void SetUp()
        {
            _catalog = ScriptableObject.CreateInstance<ItemCatalog>();
            _catalog.SetContent(TestContent.Items.ToList(), TestContent.Families.ToList(), TestContent.Families.ToList());
            _save = new SaveService(new InMemorySaveStorage());
            _progress = new ProgressService(_save, new FakeContent(_catalog));
            _progress.Load();
        }

        [TearDown]
        public void TearDown()
        {
            _progress.Dispose();
            _save.Dispose();
            Object.DestroyImmediate(_catalog);
        }

        [Test]
        public void TrySpend_WhenNotEnoughCoins_KeepsBalance()
        {
            var spent = _progress.TrySpend(PlayerProfileData.StartingCoins + 1);

            Assert.That(spent, Is.False);
            Assert.That(_progress.Coins.Value, Is.EqualTo(PlayerProfileData.StartingCoins));
        }

        [Test]
        public void CompleteLevel_KeepsBestStars()
        {
            _progress.CompleteLevel(4, 3);
            _progress.CompleteLevel(4, 1);

            Assert.That(_progress.Data.Stars[4], Is.EqualTo(3));
        }

        [Test]
        public void RegisterFound_WhenDepartmentCompletes_PaysBonusOnce()
        {
            var home = _catalog.Items.Where(i => i.Family == "home").Select(i => i.Id).ToList();
            string completed = null;

            foreach (var id in home)
            {
                completed = _progress.RegisterFound(id) ?? completed;
            }

            _progress.RegisterFound(home[0]);

            Assert.That(completed, Is.EqualTo("home"));
            Assert.That(_progress.Coins.Value, Is.EqualTo(PlayerProfileData.StartingCoins + ProgressService.FamilyBonus));
            Assert.That(_progress.Data.AlbumNew, Is.EqualTo(home.Count));
        }

        [Test]
        public void ResetProgress_KeepsSettingsAndCurve()
        {
            _progress.Data.IsMusicOn = false;
            _progress.Data.Curve = "orig";
            _progress.AddCoins(500);

            _progress.ResetProgress();

            Assert.That(_progress.Coins.Value, Is.EqualTo(PlayerProfileData.StartingCoins));
            Assert.That(_progress.Data.IsMusicOn, Is.False);
            Assert.That(_progress.Data.Curve, Is.EqualTo("orig"));
        }

        private sealed class FakeContent : IContentService
        {
            public FakeContent(ItemCatalog catalog)
            {
                Catalog = catalog;
            }

            public ItemCatalog Catalog { get; }

            public UniTask LoadAsync(CancellationToken ct) => UniTask.CompletedTask;
        }
    }
}
