using System.Collections.Generic;

namespace Game.Features.Gameplay.Model
{
    /// <summary>
    /// One of the three cabinet columns: shelves stacked bottom-first plus the hidden stockroom queue.
    /// </summary>
    public sealed class Column
    {
        /// <summary>
        /// Visible shelves, bottom first.
        /// </summary>
        public List<Block> Blocks { get; } = new List<Block>();

        /// <summary>
        /// Items waiting in the stockroom; new shelves are cut from the front of this queue.
        /// </summary>
        public List<Jar> Queue { get; } = new List<Jar>();

        /// <summary>
        /// How many shelves this column has produced; part of the seed for the next shelf's height.
        /// </summary>
        public int SpawnCount { get; set; }
    }
}
