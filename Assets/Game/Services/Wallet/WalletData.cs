using Newtonsoft.Json;
using UnityEngine.Scripting;

namespace Game.Services.Wallet
{
    /// <summary>
    /// Saved wallet chunk (<c>profile_wallet.json</c>).
    /// </summary>
    [Preserve]
    public sealed class WalletData
    {
        /// <summary>
        /// Coins a new player starts with.
        /// </summary>
        public const int StartingCoins = 150;

        /// <summary>Soft currency; never negative.</summary>
        [JsonProperty("coins")] public int Coins { get; set; } = StartingCoins;
    }
}
