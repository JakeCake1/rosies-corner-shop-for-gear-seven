using System.Threading;
using Cysharp.Threading.Tasks;

namespace Game.Services.Audio
{
    /// <summary>
    /// Plays sound effects and the background music loop, honoring the player's settings.
    /// </summary>
    public interface IAudioService
    {
        /// <summary>
        /// Loads the <see cref="AudioLibrary"/>; called once by the bootstrap state after Addressables are ready.
        /// </summary>
        /// <param name="ct">Cancelled on app shutdown.</param>
        /// <returns>Completes when every clip is loaded.</returns>
        /// <exception cref="System.OperationCanceledException">Loading was cancelled.</exception>
        /// <remarks>Sounds requested before this completes are skipped.</remarks>
        UniTask LoadAsync(CancellationToken ct);

        /// <summary>
        /// Plays a sound effect if sound is on.
        /// </summary>
        /// <param name="id">The sound.</param>
        void Play(SoundId id);

        /// <summary>
        /// Plays the tap sound that matches an item's outline class (see <see cref="TapSounds.For"/>).
        /// </summary>
        /// <param name="silhouette">The tapped item's catalog silhouette.</param>
        void PlayTap(string silhouette);

        /// <summary>
        /// Plays the basket ship arpeggio, pitched up with the combo, if sound is on.
        /// </summary>
        /// <param name="combo">Combo step of this shipment, from 0; values past the last step use the highest clip.</param>
        void PlayShip(int combo);

        /// <summary>
        /// Starts or stops the music loop to match the settings; called on startup and when settings change.
        /// </summary>
        void ApplySettings();

        /// <summary>
        /// Pauses or resumes music when the app loses or regains focus.
        /// </summary>
        /// <param name="isPaused">True while the app is in the background.</param>
        void SetPaused(bool isPaused);
    }
}
