using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Services.Audio;
using Game.Services.Flow;
using Game.Services.Onboarding;
using Game.Services.Save;
using Game.Services.Settings;
using Game.Services.UI;
using Game.Services.UI.Dialogs;

namespace Game.Features.Settings
{
    /// <summary>
    /// Settings dialog (prototype <c>openSettings</c>): sound, music, vibration; restart and back to the shop
    /// inside a level; rename the shop; reset progress with a second confirmation tap.
    /// </summary>
    public sealed class SettingsScreen : ISettingsScreen
    {
        private readonly IDialogService _dialogs;
        private readonly ISettingsService _settings;
        private readonly IProfileService _profile;
        private readonly IOnboardingService _onboarding;
        private readonly IAudioService _audio;
        private readonly IGameFlow _flow;
        private readonly IShopNamePrompt _namePrompt;
        private readonly IOverlayService _overlay;

        /// <summary>
        /// Creates the screen.
        /// </summary>
        /// <param name="dialogs">Dialog cards.</param>
        /// <param name="settings">Sound, music, vibration.</param>
        /// <param name="profile">Reset progress.</param>
        /// <param name="onboarding">Whether the shop was named (rename button).</param>
        /// <param name="audio">Applies sound and music settings.</param>
        /// <param name="flow">Back to the shop.</param>
        /// <param name="namePrompt">Rename the shop.</param>
        /// <param name="overlay">Reset confirmation toast.</param>
        public SettingsScreen(IDialogService dialogs, ISettingsService settings, IProfileService profile,
            IOnboardingService onboarding, IAudioService audio, IGameFlow flow, IShopNamePrompt namePrompt,
            IOverlayService overlay)
        {
            _overlay = overlay;
            _dialogs = dialogs;
            _settings = settings;
            _profile = profile;
            _onboarding = onboarding;
            _audio = audio;
            _flow = flow;
            _namePrompt = namePrompt;
        }

        /// <inheritdoc/>
        public async UniTask OpenAsync(Action restartLevel, CancellationToken ct)
        {
            var dialog = new DialogViewModel("Settings", RibbonStyle.Green) { IsClosedByBlocker = true };
            dialog.Toggles.Add(new DialogToggle("Sound", _settings.IsSoundOn, on =>
            {
                _settings.SetSoundOn(on);
                _audio.Play(SoundId.Clink);
                return on;
            }));
            dialog.Toggles.Add(new DialogToggle("Music", _settings.IsMusicOn, on =>
            {
                _settings.SetMusicOn(on);
                _audio.ApplySettings();
                return on;
            }));
            dialog.Toggles.Add(new DialogToggle("Vibration", _settings.IsHapticsOn, on =>
            {
                _settings.SetHapticsOn(on);
                return on;
            }));
            if (restartLevel != null)
            {
                dialog.Buttons.Add(new DialogButton("Restart level", ButtonStyle.Wood, restartLevel));
                dialog.Buttons.Add(new DialogButton("Back to the shop", ButtonStyle.Wood, () => _flow.GoHomeAsync().Forget()));
            }

            if (_onboarding.HasFlag(Flags.NameAsked))
            {
                dialog.Buttons.Add(new DialogButton("Rename the shop", ButtonStyle.WoodSmall,
                    () => _namePrompt.AskAsync(ct).Forget()));
            }

            dialog.Buttons.Add(new DialogButton("Close", ButtonStyle.Berry, null));
            var armed = false;
            var reset = new DialogButton("Reset progress", ButtonStyle.WoodSmall, () =>
            {
                if (!armed)
                {
                    armed = true;
                    _overlay.Toast("Tap again to erase everything", true);
                    return;
                }

                dialog.Close();
                _profile.ResetProgress();
                _flow.GoHomeAsync().Forget();
            }, false);
            dialog.Buttons.Add(reset);
            await _dialogs.ShowAsync(dialog, ct);
        }
    }
}
