using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Services.Audio;
using Game.Services.Flow;
using Game.Services.Localization;
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
        private readonly ILocalizationService _texts;

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
        /// <param name="texts">Card texts.</param>
        public SettingsScreen(IDialogService dialogs, ISettingsService settings, IProfileService profile,
            IOnboardingService onboarding, IAudioService audio, IGameFlow flow, IShopNamePrompt namePrompt,
            IOverlayService overlay, ILocalizationService texts)
        {
            _texts = texts;
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
            var dialog = new DialogViewModel(_texts.Get(SettingsTexts.Title), RibbonStyle.Green)
            {
                IsClosedByBlocker = true,
            };
            dialog.Toggles.Add(new DialogToggle(_texts.Get(SettingsTexts.Sound), _settings.IsSoundOn, on =>
            {
                _settings.SetSoundOn(on);
                _audio.Play(SoundId.Clink);
                return on;
            }));
            dialog.Toggles.Add(new DialogToggle(_texts.Get(SettingsTexts.Music), _settings.IsMusicOn, on =>
            {
                _settings.SetMusicOn(on);
                _audio.ApplySettings();
                return on;
            }));
            dialog.Toggles.Add(new DialogToggle(_texts.Get(SettingsTexts.Vibration), _settings.IsHapticsOn, on =>
            {
                _settings.SetHapticsOn(on);
                return on;
            }));
            if (restartLevel != null)
            {
                dialog.Buttons.Add(new DialogButton(_texts.Get(SettingsTexts.RestartLevel), ButtonStyle.Wood,
                    restartLevel));
                dialog.Buttons.Add(new DialogButton(_texts.Get(SettingsTexts.BackHome), ButtonStyle.Wood,
                    () => _flow.GoHomeAsync().Forget()));
            }

            if (_onboarding.HasFlag(Flags.NameAsked))
            {
                dialog.Buttons.Add(new DialogButton(_texts.Get(SettingsTexts.Rename), ButtonStyle.WoodSmall,
                    () => _namePrompt.AskAsync(ct).Forget()));
            }

            dialog.Buttons.Add(new DialogButton(_texts.Get(SettingsTexts.Close), ButtonStyle.Berry, null));
            var armed = false;
            var reset = new DialogButton(_texts.Get(SettingsTexts.Reset), ButtonStyle.WoodSmall, () =>
            {
                if (!armed)
                {
                    armed = true;
                    _overlay.Toast(_texts.Get(SettingsTexts.ResetConfirm), true);
                    return;
                }

                dialog.Close();
                ResetAndGoHomeAsync(restartLevel != null).Forget();
            }, false);
            dialog.Buttons.Add(reset);
            await _dialogs.ShowAsync(dialog, ct);
        }

        private async UniTask ResetAndGoHomeAsync(bool isInLevel)
        {
            // In a level the reset waits for the way home: Profile.Changed must not reach the level's live scope.
            // On the home screen the reset comes first so the reloaded home shows the fresh profile.
            if (isInLevel)
            {
                await _flow.GoHomeAsync();
                _profile.ResetProgress();
                return;
            }

            _profile.ResetProgress();
            await _flow.GoHomeAsync();
        }
    }
}
