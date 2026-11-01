using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine.Scripting;

namespace Game.Services.Onboarding
{
    /// <summary>
    /// Saved onboarding chunk (<c>profile_onboarding.json</c>).
    /// </summary>
    [Preserve]
    public sealed class OnboardingData
    {
        /// <summary>One-time milestones and onboarding flags (see <see cref="Game.Services.Onboarding.Flags"/>).</summary>
        [JsonProperty("flags")] public HashSet<string> Flags { get; set; } = new HashSet<string>();

        /// <summary>Coach tips already shown ("counter", "ship", "stock").</summary>
        [JsonProperty("tips")] public HashSet<string> SeenTips { get; set; } = new HashSet<string>();
    }
}
