using Game.Services.Settings;
using UnityEngine;

namespace Game.Services.Haptics
{
    /// <summary>
    /// Default <see cref="IHapticsService"/>; uses <see cref="Handheld.Vibrate"/> on Android. Unity offers no
    /// pattern API there, so taps (too frequent for a full buzz) are skipped and the bigger moments vibrate once.
    /// </summary>
    public sealed class HapticsService : IHapticsService
    {
        private readonly ISettingsService _settings;

        /// <summary>
        /// Creates the service.
        /// </summary>
        /// <param name="settings">Vibration on/off.</param>
        public HapticsService(ISettingsService settings)
        {
            _settings = settings;
        }

        /// <inheritdoc/>
        public void Pulse(HapticKind kind)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!_settings.IsHapticsOn || kind == HapticKind.Tap)
            {
                return;
            }

            Handheld.Vibrate();
#endif
        }
    }
}
