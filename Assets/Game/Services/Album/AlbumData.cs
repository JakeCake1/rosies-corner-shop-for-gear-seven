using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine.Scripting;

namespace Game.Services.Album
{
    /// <summary>
    /// Saved album chunk (<c>profile_album.json</c>); items are stored by catalog id.
    /// </summary>
    [Preserve]
    public sealed class AlbumData
    {
        /// <summary>Catalog ids of items shipped at least once, in discovery order.</summary>
        [JsonProperty("found")] public List<int> Found { get; set; } = new List<int>();

        /// <summary>Departments whose completion bonus was paid.</summary>
        [JsonProperty("famDone")] public HashSet<string> CompletedFamilies { get; set; } = new HashSet<string>();

        /// <summary>Items found since the album was last opened; the badge on the Album button.</summary>
        [JsonProperty("albumNew")] public int NewCount { get; set; }
    }
}
