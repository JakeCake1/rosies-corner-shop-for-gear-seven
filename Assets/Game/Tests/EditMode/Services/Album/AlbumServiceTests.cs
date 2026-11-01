using System.Linq;
using Game.Services.Album;
using Game.Services.Content;
using Game.Services.Wallet;
using Game.Tests.EditMode.Features.Gameplay;
using Game.Tests.EditMode.Services.Content;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode.Services.Album
{
    public sealed class AlbumServiceTests
    {
        private ItemCatalog _catalog;
        private WalletService _wallet;
        private AlbumService _album;

        [SetUp]
        public void SetUp()
        {
            _catalog = ScriptableObject.CreateInstance<ItemCatalog>();
            _catalog.SetContent(TestContent.Items.ToList(), TestContent.Families.ToList());
            _wallet = new WalletService();
            _album = new AlbumService(new FakeContentService(_catalog), _wallet);
        }

        [TearDown]
        public void TearDown()
        {
            _album.Dispose();
            _wallet.Dispose();
            Object.DestroyImmediate(_catalog);
        }

        [Test]
        public void RegisterFound_WhenDepartmentCompletes_PaysBonusOnce()
        {
            var home = _catalog.Items.Where(i => i.Family == "home").Select(i => i.Id).ToList();

            var completed = home.Select(_album.RegisterFound).ToList();
            _album.RegisterFound(home[0]);

            Assert.That(completed.Last(), Is.EqualTo("home"));
            Assert.That(completed.Count(f => f != null), Is.EqualTo(1));
            Assert.That(_wallet.Coins.Value, Is.EqualTo(WalletData.StartingCoins + AlbumService.FamilyBonus));
            Assert.That(_album.NewCount, Is.EqualTo(home.Count));
            Assert.That(_album.IsFamilyCompleted("home"), Is.True);
        }

        [Test]
        public void MarkSeen_ClearsNewCountButKeepsFound()
        {
            var id = _catalog.Items[0].Id;
            _album.RegisterFound(id);

            _album.MarkSeen();

            Assert.That(_album.NewCount, Is.Zero);
            Assert.That(_album.Found, Is.EqualTo(new[] { id }));
        }
    }
}
