using Game.Services.Audio;
using Game.Services.Progress;
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
        private IProgressService _progress;
        private IAudioService _audio;

        /// <summary>
        /// Receives dependencies from the project scope.
        /// </summary>
        /// <param name="progress">Saved on pause.</param>
        /// <param name="audio">Music paused in the background.</param>
        [Inject]
        public void Construct(IProgressService progress, IAudioService audio)
        {
            _progress = progress;
            _audio = audio;
        }

        private void OnApplicationPause(bool isPaused)
        {
            if (_progress == null) return;

            if (isPaused) _progress.Save();
            _audio.SetPaused(isPaused);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (_progress == null || hasFocus) return;

            _progress.Save();
        }
    }
}
