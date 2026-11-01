using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine.Scripting;

namespace Game.Features.Gameplay.Boosters
{
    /// <summary>
    /// Saved boosters chunk (<c>profile_boosters.json</c>).
    /// </summary>
    [Preserve]
    public sealed class BoosterData
    {
        /// <summary>Owned in-level boosters by id ("pick", "tidy", "shuffle").</summary>
        [JsonProperty("boost")] public Dictionary<string, int> Owned { get; set; } = new Dictionary<string, int>();

        /// <summary>Owned start boosters by id ("basket", "quick").</summary>
        [JsonProperty("pre")] public Dictionary<string, int> StartOwned { get; set; } = new Dictionary<string, int>();

        /// <summary>Boosters whose unlock gift was already given ("undo", "tidy", "pick").</summary>
        [JsonProperty("got")] public HashSet<string> Gifted { get; set; } = new HashSet<string>();
    }
}
