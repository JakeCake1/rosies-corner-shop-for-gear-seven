namespace Game.Services.Audio
{
    /// <summary>
    /// Plays sound effects and the background music loop, honoring the player's settings.
    /// </summary>
    public interface IAudioService
    {
        /// <summary>
        /// Plays a sound effect if sound is on.
        /// </summary>
        /// <param name="id">The sound.</param>
        /// <param name="pitch">Playback pitch multiplier (1 = original); the ship sound rises with the combo.</param>
        void Play(SoundId id, float pitch = 1f);

        /// <summary>
        /// Plays the tap sound that matches an item's outline class.
        /// </summary>
        /// <param name="silhouette">"box", "can", "bottle", "stick" or "round"; anything else plays the soft tap.</param>
        void PlayTap(string silhouette);

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
