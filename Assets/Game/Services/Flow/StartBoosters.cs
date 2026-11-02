namespace Game.Services.Flow
{
    /// <summary>
    /// Start boosters chosen before a level (already paid for when this object exists).
    /// </summary>
    public sealed class StartBoosters
    {
        /// <summary>
        /// No start boosters; also means "the player already saw the start-boosters popup".
        /// </summary>
        public static readonly StartBoosters None = new StartBoosters(false, false);

        /// <summary>Start with the third basket open.</summary>
        public bool HasExtraBasket { get; }

        /// <summary>One bonus Quick Pick usable only in this level.</summary>
        public bool HasQuickPick { get; }

        /// <summary>
        /// Creates a selection.
        /// </summary>
        /// <param name="hasExtraBasket">Start with three baskets.</param>
        /// <param name="hasQuickPick">One extra Quick Pick for this level.</param>
        public StartBoosters(bool hasExtraBasket, bool hasQuickPick)
        {
            HasExtraBasket = hasExtraBasket;
            HasQuickPick = hasQuickPick;
        }
    }
}
