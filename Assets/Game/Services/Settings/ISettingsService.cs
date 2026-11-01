namespace Game.Services.Settings
{
    /// <summary>
    /// Player settings (sound, music, vibration); owns the <c>settings</c> profile chunk, which survives
    /// "reset progress".
    /// </summary>
    public interface ISettingsService
    {
        /// <summary>
        /// Sound effects on.
        /// </summary>
        bool IsSoundOn { get; }

        /// <summary>
        /// Background music on; the audio service applies it.
        /// </summary>
        bool IsMusicOn { get; }

        /// <summary>
        /// Vibration on (has an effect on Android only).
        /// </summary>
        bool IsHapticsOn { get; }

        /// <summary>
        /// Turns sound effects on or off.
        /// </summary>
        /// <param name="isOn">New value.</param>
        void SetSoundOn(bool isOn);

        /// <summary>
        /// Turns the music on or off; call <c>IAudioService.ApplySettings</c> afterwards to start or stop it.
        /// </summary>
        /// <param name="isOn">New value.</param>
        void SetMusicOn(bool isOn);

        /// <summary>
        /// Turns vibration on or off.
        /// </summary>
        /// <param name="isOn">New value.</param>
        void SetHapticsOn(bool isOn);
    }
}
