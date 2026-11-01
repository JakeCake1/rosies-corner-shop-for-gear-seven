using Game.Services.Save;

namespace Game.Services.Settings
{
    /// <summary>
    /// Default <see cref="ISettingsService"/>.
    /// </summary>
    public sealed class SettingsService : ProfileChunk<SettingsData>, ISettingsService
    {
        /// <inheritdoc/>
        public override string Key => "settings";

        /// <inheritdoc/>
        public override bool IsKeptOnReset => true;

        /// <inheritdoc/>
        public bool IsSoundOn => Data.IsSoundOn;

        /// <inheritdoc/>
        public bool IsMusicOn => Data.IsMusicOn;

        /// <inheritdoc/>
        public bool IsHapticsOn => Data.IsHapticsOn;

        /// <inheritdoc/>
        public void SetSoundOn(bool isOn)
        {
            Data.IsSoundOn = isOn;
            MarkChanged();
        }

        /// <inheritdoc/>
        public void SetMusicOn(bool isOn)
        {
            Data.IsMusicOn = isOn;
            MarkChanged();
        }

        /// <inheritdoc/>
        public void SetHapticsOn(bool isOn)
        {
            Data.IsHapticsOn = isOn;
            MarkChanged();
        }
    }
}
