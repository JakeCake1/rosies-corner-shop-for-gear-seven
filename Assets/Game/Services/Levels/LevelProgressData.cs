using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine.Scripting;

namespace Game.Services.Levels
{
    /// <summary>
    /// Saved level progress chunk (<c>profile_levels.json</c>); levels are stored by number in the active curve.
    /// </summary>
    [Preserve]
    public sealed class LevelProgressData
    {
        /// <summary>Best stars (1..3) per completed level number of the active curve.</summary>
        [JsonProperty("stars")] public Dictionary<int, int> Stars { get; set; } = new Dictionary<int, int>();

        /// <summary>Levels the player has lost at least once; the hidden hint is enabled on them.</summary>
        [JsonProperty("failedLv")] public HashSet<int> FailedLevels { get; set; } = new HashSet<int>();
    }
}
