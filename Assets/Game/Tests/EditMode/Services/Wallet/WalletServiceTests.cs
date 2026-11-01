using System.Collections.Generic;
using Game.Services.Wallet;
using NUnit.Framework;
using UniRx;

namespace Game.Tests.EditMode.Services.Wallet
{
    public sealed class WalletServiceTests
    {
        private WalletService _wallet;

        [SetUp]
        public void SetUp() => _wallet = new WalletService();

        [TearDown]
        public void TearDown() => _wallet.Dispose();

        [Test]
        public void TrySpend_WhenNotEnoughCoins_KeepsBalance()
        {
            var isSpent = _wallet.TrySpend(WalletData.StartingCoins + 1);

            Assert.That(isSpent, Is.False);
            Assert.That(_wallet.Coins.Value, Is.EqualTo(WalletData.StartingCoins));
        }

        [Test]
        public void Restore_UpdatesCoins()
        {
            var values = new List<int>();
            using (_wallet.Coins.Subscribe(values.Add))
            {
                _wallet.Restore(new WalletData { Coins = 3 });
            }

            Assert.That(values, Is.EqualTo(new[] { WalletData.StartingCoins, 3 }));
        }

        [Test]
        public void Add_ReportsChange()
        {
            var changes = 0;
            using (_wallet.Changed.Subscribe(_ => changes++))
            {
                _wallet.Add(5);
                _wallet.Add(0);
            }

            Assert.That(changes, Is.EqualTo(1));
            Assert.That(_wallet.Coins.Value, Is.EqualTo(WalletData.StartingCoins + 5));
        }
    }
}
