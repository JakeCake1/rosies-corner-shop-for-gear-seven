using UniRx;

namespace Game.Services.Wallet
{
    /// <summary>
    /// The player's coins; owns the <c>wallet</c> profile chunk and saves it after every change.
    /// </summary>
    public interface IWalletService
    {
        /// <summary>
        /// Current coins; emits on every change (HUD and home coin chips bind to it).
        /// </summary>
        IReadOnlyReactiveProperty<int> Coins { get; }

        /// <summary>
        /// Adds coins.
        /// </summary>
        /// <param name="amount">Coins to add; zero or negative amounts are ignored.</param>
        void Add(int amount);

        /// <summary>
        /// Spends coins if the balance allows.
        /// </summary>
        /// <param name="amount">Price in coins.</param>
        /// <returns>False (and nothing changes) when there are not enough coins or the price is negative.</returns>
        bool TrySpend(int amount);
    }
}
