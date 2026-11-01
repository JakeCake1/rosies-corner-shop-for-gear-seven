using Newtonsoft.Json;
using UnityEngine.Scripting;

namespace Game.Features.Home
{
    /// <summary>
    /// Saved shop chunk (<c>profile_shop.json</c>).
    /// </summary>
    [Preserve]
    public sealed class ShopProfileData
    {
        /// <summary>The player's own shop name; empty keeps "Rosie's Corner Shop".</summary>
        [JsonProperty("shopName")] public string ShopName { get; set; } = string.Empty;
    }
}
