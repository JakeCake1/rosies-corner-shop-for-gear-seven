using Newtonsoft.Json;
using UnityEngine.Scripting;

namespace Game.Services.Experiments
{
    /// <summary>
    /// Saved reviewer choices chunk (<c>profile_experiments.json</c>).
    /// </summary>
    [Preserve]
    public sealed class ExperimentsData
    {
        /// <summary>Active level curve id ("new" or "orig").</summary>
        [JsonProperty("curve")] public string Curve { get; set; } = "new";

        /// <summary>A/B variant of the rewarded offer after a loss ("basket" or "slot").</summary>
        [JsonProperty("offer")] public string LossOffer { get; set; } = "basket";
    }
}
