using System.Collections.Generic;

namespace Game.Features.Gameplay.Model
{
    /// <summary>
    /// Mechanic budgets of a level; frozen shelves are spent as new shelves appear.
    /// </summary>
    public sealed class LevelMechanics
    {
        /// <summary>Chance (0..1) that a new item is hidden in a paper bag.</summary>
        public double Wrap { get; set; }

        /// <summary>Chance (0..1) that a new item gets a second-row item behind it.</summary>
        public double Deep { get; set; }

        /// <summary>Frozen shelves still to place.</summary>
        public int Frozen { get; set; }

        /// <summary>Number of nailed shelves (0 or 1).</summary>
        public int Boarded { get; set; }

        /// <summary>Baskets needed to thaw a frozen shelf.</summary>
        public int Thaw { get; set; }

        /// <summary>Hammer placement mode: 0 open shelf, 1 stockroom, 2 tied shelf.</summary>
        public int HammerMode { get; set; }

        /// <summary>
        /// Copies the budgets for an undo snapshot.
        /// </summary>
        /// <returns>A new instance with the same values.</returns>
        public LevelMechanics Clone() => (LevelMechanics)MemberwiseClone();
    }
}
