namespace Game.Features.Gameplay.Session
{
    /// <summary>
    /// Causes of a <see cref="BoardChange"/>.
    /// </summary>
    public enum BoardChangeKind
    {
        /// <summary>A new level started: shelves drop in column by column.</summary>
        LevelStart,

        /// <summary>The state was replaced (undo): redraw without animation.</summary>
        Rebuild,

        /// <summary>An item was tapped.</summary>
        Tap,

        /// <summary>A basket shipped; a new order may have arrived.</summary>
        Ship,

        /// <summary>Quick Pick filled a basket.</summary>
        QuickPick,

        /// <summary>Tidy Up returned counter items to a stockroom.</summary>
        Tidy,

        /// <summary>Shuffle mixed the open shelves.</summary>
        Shuffle,

        /// <summary>Baskets or the counter changed size (extra basket, extra counter spot).</summary>
        Layout,
    }
}
