using Game.Services.Wallet;
using UniRx;

namespace Game.Tests.EditMode.Features.Gameplay.Session
{
    internal sealed class FakeWalletService : IWalletService
    {
        private readonly ReactiveProperty<int> _coins = new ReactiveProperty<int>();

        public IReadOnlyReactiveProperty<int> Coins => _coins;

        public void Add(int amount)
        {
            // Like WalletService: only positive amounts, saturating.
            if (amount > 0)
            {
                _coins.Value = (int)System.Math.Min((long)_coins.Value + amount, int.MaxValue);
            }
        }

        public bool TrySpend(int amount)
        {
            if (amount < 0 || amount > _coins.Value)
            {
                return false;
            }

            _coins.Value -= amount;
            return true;
        }
    }
}
