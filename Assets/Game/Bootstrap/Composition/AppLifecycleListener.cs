using Game.Services.Audio;
using Game.Services.Save;
using UnityEngine;
using VContainer;

namespace Game.Bootstrap.Composition
{
    /// <summary>
    /// Forwards app pause and focus to the services that care: saves progress (mobile OSes may kill a paused app
    /// without calling quit) and pauses the music.
    /// </summary>
    public sealed class AppLifecycleListener : MonoBehaviour
    {
        private IProfileService _profile;
        private IAudioService _audio;

        /// <summary>
        /// Receives dependencies from the project scope.
        /// </summary>
        /// <param name="profile">Saved on pause.</param>
        /// <param name="audio">Music paused in the background.</param>
        [Inject]
        public void Construct(IProfileService profile, IAudioService audio)
        {
            _profile = profile;
            _audio = audio;
        }

        private void OnApplicationPause(bool isPaused)
        {
            if (_profile == null)
            {
                return;
            }

            if (isPaused)
            {
                _profile.SaveAll();
            }
            _audio.SetPaused(isPaused);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (_profile == null || hasFocus)
            {
                return;
            }

            _profile.SaveAll();
        }
    }
}
