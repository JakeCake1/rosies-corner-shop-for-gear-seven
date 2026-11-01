namespace Game.Features.Gameplay.Model
{
    /// <summary>
    /// Kinds of <see cref="LevelEvent"/>.
    /// </summary>
    public enum LevelEventType
    {
        /// <summary>A new shelf was cut from the stockroom and drops into its column.</summary>
        ShelfArrived,

        /// <summary>An empty shelf folded away; the shelves above slide down.</summary>
        ShelfFolded,

        /// <summary>A brass key flew from the folded shelf and cut the twine of a tied shelf.</summary>
        ShelfUnlocked,

        /// <summary>The hammer pulled the boards off the nailed shelf.</summary>
        ShelfUnboarded,

        /// <summary>A frozen shelf thawed.</summary>
        ShelfThawed,

        /// <summary>A paper bag opened and shows its item.</summary>
        JarUnwrapped,

        /// <summary>A new customer basket arrived in a slot.</summary>
        BasketArrived,
    }
}
