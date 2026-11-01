namespace Game.Features.Gameplay.Session
{
    /// <summary>
    /// Stable booster ids used in the save and the booster bar.
    /// </summary>
    public static class BoosterIds
    {
        /// <summary>Undo the last move.</summary>
        public const string Undo = "undo";

        /// <summary>Tidy Up: counter items back to a stockroom.</summary>
        public const string Tidy = "tidy";

        /// <summary>Quick Pick: fill a basket.</summary>
        public const string Pick = "pick";

        /// <summary>Shuffle the open shelves (original curve).</summary>
        public const string Shuffle = "shuffle";

        /// <summary>Start booster: open the third basket.</summary>
        public const string StartBasket = "basket";

        /// <summary>Start booster: one extra Quick Pick.</summary>
        public const string StartQuick = "quick";
    }
}
