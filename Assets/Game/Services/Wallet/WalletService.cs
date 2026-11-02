using Game.Services.Save;
using UniRx;

namespace Game.Services.Wallet
{
    /// <summary>
    /// Default <see cref="IWalletService"/>; mirrors the saved coins into a reactive property.
    /// </summary>
    public sealed class WalletService : ProfileChunk<WalletData>, IWalletService
    {
        private readonly ReactiveProperty<int> _coins = new ReactiveProperty<int>(WalletData.StartingCoins);

        /// <inheritdoc/>
        public override string Key => "wallet";

        /// <inheritdoc/>
        public IReadOnlyReactiveProperty<int> Coins => _coins;

        /// <inheritdoc/>
        public void Add(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            Data.Coins += amount;
            Commit();
        }

        /// <inheritdoc/>
        public bool TrySpend(int amount)
        {
            if (amount < 0 || Data.Coins < amount)
            {
                return false;
            }

            Data.Coins -= amount;
            Commit();
            return true;
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            _coins.Dispose();
            base.Dispose();
        }

        /// <inheritdoc/>
        protected override void OnRestored() => _coins.Value = Data.Coins;

        private void Commit()
        {
            _coins.Value = Data.Coins;
            MarkChanged();
        }
    }
}
