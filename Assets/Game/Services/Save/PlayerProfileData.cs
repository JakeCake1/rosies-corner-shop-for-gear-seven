using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine.Scripting;

namespace Game.Services.Save
{
    /// <summary>
    /// Everything that persists between sessions (the prototype's <c>save</c> object), as a plain JSON DTO.
    /// </summary>
    /// <remarks>
    /// Every field has a safe default so older saves without it still load. Items are stored by catalog id and
    /// levels by number, never by asset reference. Bump <see cref="CurrentVersion"/> and add a migration step in
    /// <see cref="SaveMigrations"/> whenever the structure changes.
    /// </remarks>
    [Preserve]
    public sealed class PlayerProfileData
    {
        /// <summary>
        /// Version written by this build; saves from a newer build are not loaded over.
        /// </summary>
        public const int CurrentVersion = 1;

        /// <summary>
        /// Coins a new player starts with.
        /// </summary>
        public const int StartingCoins = 150;

        /// <summary>Format version of this save.</summary>
        [JsonProperty("version")] public int Version { get; set; } = CurrentVersion;

        /// <summary>Soft currency; never negative.</summary>
        [JsonProperty("coins")] public int Coins { get; set; } = StartingCoins;

        /// <summary>Best stars (1..3) per completed level number of the active curve.</summary>
        [JsonProperty("stars")] public Dictionary<int, int> Stars { get; set; } = new Dictionary<int, int>();

        /// <summary>Catalog ids of items shipped at least once (the album), in discovery order.</summary>
        [JsonProperty("found")] public List<int> Found { get; set; } = new List<int>();

        /// <summary>Owned in-level boosters by id ("pick", "tidy", "shuffle").</summary>
        [JsonProperty("boost")] public Dictionary<string, int> Boosters { get; set; } = new Dictionary<string, int>();

        /// <summary>Owned start boosters by id ("basket", "quick").</summary>
        [JsonProperty("pre")] public Dictionary<string, int> PreBoosters { get; set; } = new Dictionary<string, int>();

        /// <summary>Boosters whose unlock gift was already given ("undo", "tidy", "pick").</summary>
        [JsonProperty("got")] public HashSet<string> GiftedBoosters { get; set; } = new HashSet<string>();

        /// <summary>Levels the player has lost at least once; the hidden hint is enabled on them.</summary>
        [JsonProperty("failedLv")] public HashSet<int> FailedLevels { get; set; } = new HashSet<int>();

        /// <summary>Coach tips already shown ("counter", "ship", "stock").</summary>
        [JsonProperty("tips")] public HashSet<string> SeenTips { get; set; } = new HashSet<string>();

        /// <summary>Departments whose completion bonus was paid.</summary>
        [JsonProperty("famDone")] public HashSet<string> CompletedFamilies { get; set; } = new HashSet<string>();

        /// <summary>One-time milestones and onboarding flags ("freeBasket", "preGift", "albumIntro", ...).</summary>
        [JsonProperty("flags")] public HashSet<string> Flags { get; set; } = new HashSet<string>();

        /// <summary>Items found since the album was last opened; the badge on the Album button.</summary>
        [JsonProperty("albumNew")] public int AlbumNew { get; set; }

        /// <summary>The player's own shop name; empty keeps "Rosie's Corner Shop".</summary>
        [JsonProperty("shopName")] public string ShopName { get; set; } = string.Empty;

        /// <summary>Active level curve id ("new" or "orig").</summary>
        [JsonProperty("curve")] public string Curve { get; set; } = "new";

        /// <summary>A/B variant of the rewarded offer after a loss ("basket" or "slot").</summary>
        [JsonProperty("offer")] public string LossOffer { get; set; } = "basket";

        /// <summary>Sound effects on.</summary>
        [JsonProperty("sound")] public bool IsSoundOn { get; set; } = true;

        /// <summary>Background music on.</summary>
        [JsonProperty("music")] public bool IsMusicOn { get; set; } = true;

        /// <summary>Vibration on (Android only).</summary>
        [JsonProperty("haptic")] public bool IsHapticsOn { get; set; } = true;

        /// <summary>
        /// Creates the profile of a brand-new player.
        /// </summary>
        /// <returns>A fresh profile with starting coins and default settings.</returns>
        public static PlayerProfileData CreateNew() => new PlayerProfileData();
    }
}
