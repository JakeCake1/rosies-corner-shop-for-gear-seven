using Game.Features.Gameplay.Boosters;
using NUnit.Framework;
using UniRx;

namespace Game.Tests.EditMode.Features.Gameplay.Boosters
{
    public sealed class BoosterInventoryTests
    {
        private BoosterInventory _inventory;

        [SetUp]
        public void SetUp() => _inventory = new BoosterInventory();

        [TearDown]
        public void TearDown() => _inventory.Dispose();

        [Test]
        public void Get_WhenNeverOwned_ReturnsZero()
        {
            Assert.That(_inventory.Get("pick"), Is.EqualTo(0));
            Assert.That(_inventory.GetStart("basket"), Is.EqualTo(0));
        }

        [Test]
        public void Add_WhenDeltaPositive_IncreasesCount()
        {
            _inventory.Add("pick", 2);
            _inventory.Add("pick", 3);

            Assert.That(_inventory.Get("pick"), Is.EqualTo(5));
        }

        [Test]
        public void Add_WhenDeltaExceedsOwned_ClampsAtZero()
        {
            _inventory.Add("tidy", 1);
            _inventory.Add("tidy", -5);

            Assert.That(_inventory.Get("tidy"), Is.EqualTo(0));
        }

        [Test]
        public void Add_KeepsOwnedAndStartCountsSeparate()
        {
            _inventory.Add("pick", 1);
            _inventory.AddStart("pick", 4);

            Assert.That(_inventory.Get("pick"), Is.EqualTo(1));
            Assert.That(_inventory.GetStart("pick"), Is.EqualTo(4));
        }

        [Test]
        public void AddStart_WhenDeltaExceedsOwned_ClampsAtZero()
        {
            _inventory.AddStart("quick", -1);

            Assert.That(_inventory.GetStart("quick"), Is.EqualTo(0));
        }

        [Test]
        public void MarkGifted_WhenCalledTwice_ReportsChangeOnce()
        {
            var changes = 0;
            using (_inventory.Changed.Subscribe(_ => changes++))
            {
                _inventory.MarkGifted("undo");
                _inventory.MarkGifted("undo");
            }

            Assert.That(_inventory.IsGifted("undo"), Is.True);
            Assert.That(_inventory.IsGifted("tidy"), Is.False);
            Assert.That(changes, Is.EqualTo(1));
        }

        [Test]
        public void Add_ReportsChange()
        {
            var changes = 0;
            using (_inventory.Changed.Subscribe(_ => changes++))
            {
                _inventory.Add("pick", 1);
                _inventory.AddStart("basket", 1);
            }

            Assert.That(changes, Is.EqualTo(2));
        }

        [Test]
        public void Restore_ReplacesCounts()
        {
            _inventory.Add("pick", 3);
            var data = new BoosterData();
            data.Owned["pick"] = 7;
            data.Gifted.Add("pick");

            _inventory.Restore(data);

            Assert.That(_inventory.Get("pick"), Is.EqualTo(7));
            Assert.That(_inventory.IsGifted("pick"), Is.True);
        }

        [Test]
        public void Key_IsBoosters()
        {
            Assert.That(_inventory.Key, Is.EqualTo("boosters"));
        }
    }
}
