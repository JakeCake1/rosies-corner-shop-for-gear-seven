using System.Collections.Generic;

namespace Game.Features.Gameplay.Model
{
    /// <summary>
    /// Where the hammer was placed, so the level banner can tell the player where to dig.
    /// </summary>
    public enum HammerPlace
    {
        /// <summary>No nailed shelf on this level.</summary>
        None,

        /// <summary>On an open shelf from the start.</summary>
        Shelf,

        /// <summary>In a column's stockroom; arrives with that column's next shelf.</summary>
        Stock,

        /// <summary>Inside a tied shelf; the key comes first.</summary>
        Tied,
    }
}
