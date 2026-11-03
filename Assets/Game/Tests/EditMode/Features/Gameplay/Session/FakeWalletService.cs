using Game.Services.Wallet;
using UniRx;

namespace Game.Tests.EditMode.Features.Gameplay.Session
{
    internal sealed class FakeWalletService : IWalletService
    {
        private readonly ReactiveProperty<int> _coins = new ReactiveProperty<int>();

        public IReadOnlyReactiveProperty<int> Coins => _coins;

        public void Add(int amount) => _coins.Value += amount;

        public bool TrySpend(int amount)
        {
            if (amount > _coins.Value)
            {
                return false;
            }

            _coins.Value -= amount;
            return true;
        }
    }
}
