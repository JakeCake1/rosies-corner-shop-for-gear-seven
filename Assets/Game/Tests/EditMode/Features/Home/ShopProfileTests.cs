using Game.Features.Home;
using NUnit.Framework;
using UniRx;

namespace Game.Tests.EditMode.Features.Home
{
    public sealed class ShopProfileTests
    {
        private ShopProfile _shop;
        private int _changedCount;

        [SetUp]
        public void SetUp()
        {
            _shop = new ShopProfile();
            _changedCount = 0;
            _shop.Changed.Subscribe(_ => _changedCount++);
        }

        [TearDown]
        public void TearDown()
        {
            _shop.Dispose();
        }

        [Test]
        public void Key_Always_IsShop()
        {
            Assert.That(_shop.Key, Is.EqualTo("shop"));
        }

        [Test]
        public void Name_ForNewPlayer_IsEmpty()
        {
            Assert.That(_shop.Name, Is.Empty);
        }

        [Test]
        public void Rename_WithName_StoresNameAndReportsChange()
        {
            _shop.Rename("Lily Place");

            Assert.That(_shop.Name, Is.EqualTo("Lily Place"));
            Assert.That(_changedCount, Is.EqualTo(1));
        }

        [Test]
        public void Restore_WithSavedData_ExposesSavedName()
        {
            _shop.Restore(new ShopProfileData { ShopName = "Saved" });

            Assert.That(_shop.Name, Is.EqualTo("Saved"));
        }

        [Test]
        public void Restore_WithNull_FallsBackToDefaults()
        {
            _shop.Rename("Old");

            _shop.Restore(null);

            Assert.That(_shop.Name, Is.Empty);
        }
    }
}
