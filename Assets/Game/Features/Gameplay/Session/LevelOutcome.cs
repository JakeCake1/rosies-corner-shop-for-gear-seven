namespace Game.Features.Gameplay.Session
{
    /// <summary>
    /// How a level ended.
    /// </summary>
    public readonly struct LevelOutcome
    {
        /// <summary>True for a win.</summary>
        public bool IsWon { get; }

        /// <summary>Stars earned (wins only).</summary>
        public int Stars { get; }

        /// <summary>Coins already credited (wins only).</summary>
        public int Reward { get; }

        /// <summary>Items not yet packed (losses only), for the near-miss message.</summary>
        public int ItemsLeft { get; }

        private LevelOutcome(bool isWon, int stars, int reward, int itemsLeft)
        {
            IsWon = isWon;
            Stars = stars;
            Reward = reward;
            ItemsLeft = itemsLeft;
        }

        /// <summary>
        /// A win.
        /// </summary>
        /// <param name="stars">Stars earned.</param>
        /// <param name="reward">Coins credited.</param>
        /// <returns>The outcome.</returns>
        public static LevelOutcome Won(int stars, int reward) => new LevelOutcome(true, stars, reward, 0);

        /// <summary>
        /// A loss.
        /// </summary>
        /// <param name="itemsLeft">Items not yet packed.</param>
        /// <returns>The outcome.</returns>
        public static LevelOutcome Lost(int itemsLeft) => new LevelOutcome(false, 0, 0, itemsLeft);
    }
}
