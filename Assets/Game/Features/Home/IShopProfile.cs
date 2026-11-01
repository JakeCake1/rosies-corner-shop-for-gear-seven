namespace Game.Features.Home
{
    /// <summary>
    /// The shop's name on the home sign; owns the <c>shop</c> profile chunk.
    /// </summary>
    public interface IShopProfile
    {
        /// <summary>
        /// The player's own name; empty until the player names the shop (the sign then shows
        /// <see cref="HomeViewModel.DefaultShopName"/>).
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Stores a new name.
        /// </summary>
        /// <param name="name">Trimmed, non-empty name of at most <see cref="ShopNamePrompt.MaxNameLength"/> characters.</param>
        void Rename(string name);
    }
}
