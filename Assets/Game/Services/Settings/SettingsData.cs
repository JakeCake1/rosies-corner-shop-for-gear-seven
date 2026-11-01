using Newtonsoft.Json;
using UnityEngine.Scripting;

namespace Game.Services.Settings
{
    /// <summary>
    /// Saved settings chunk (<c>profile_settings.json</c>).
    /// </summary>
    [Preserve]
    public sealed class SettingsData
    {
        /// <summary>Sound effects on.</summary>
        [JsonProperty("sound")] public bool IsSoundOn { get; set; } = true;

        /// <summary>Background music on.</summary>
        [JsonProperty("music")] public bool IsMusicOn { get; set; } = true;

        /// <summary>Vibration on (Android only).</summary>
        [JsonProperty("haptic")] public bool IsHapticsOn { get; set; } = true;
    }
}
