using Game.Services.Progress;
using UnityEngine;

namespace Game.Services.Haptics
{
    /// <summary>
    /// Default <see cref="IHapticsService"/>; uses <see cref="Handheld.Vibrate"/> on Android. Unity offers no
    /// pattern API there, so taps (too frequent for a full buzz) are skipped and the bigger moments vibrate once.
    /// </summary>
    public sealed class HapticsService : IHapticsService
    {
        private readonly IProgressService _progress;

        /// <summary>
        /// Creates the service.
        /// </summary>
        /// <param name="progress">Holds the vibration setting.</param>
        public HapticsService(IProgressService progress)
        {
            _progress = progress;
        }

        /// <inheritdoc/>
        public void Pulse(HapticKind kind)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!_progress.Data.IsHapticsOn || kind == HapticKind.Tap) return;

            Handheld.Vibrate();
#endif
        }
    }
}
