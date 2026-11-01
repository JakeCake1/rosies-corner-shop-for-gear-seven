namespace Game.Features.Gameplay.Model
{
    /// <summary>
    /// A jar on a shelf together with where it sits.
    /// </summary>
    public readonly struct JarLocation
    {
        /// <summary>
        /// Creates a location.
        /// </summary>
        /// <param name="block">Shelf holding the jar.</param>
        /// <param name="column">Column index of the shelf.</param>
        /// <param name="slot">Cell index on the shelf.</param>
        /// <param name="jar">The jar in that cell when the location was taken.</param>
        public JarLocation(Block block, int column, int slot, Jar jar)
        {
            Block = block;
            Column = column;
            Slot = slot;
            Jar = jar;
        }

        /// <summary>Shelf holding the jar.</summary>
        public Block Block { get; }

        /// <summary>Column index of the shelf.</summary>
        public int Column { get; }

        /// <summary>Cell index on the shelf; row = slot / jarsPerRow.</summary>
        public int Slot { get; }

        /// <summary>The jar in that cell when the location was listed; compare by reference to detect changes.</summary>
        public Jar Jar { get; }
    }
}
