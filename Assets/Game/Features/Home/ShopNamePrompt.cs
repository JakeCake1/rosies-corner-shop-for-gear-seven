using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Services.Audio;
using Game.Services.Flow;
using Game.Services.Localization;
using Game.Services.Onboarding;
using Game.Services.UI;
using Game.Services.UI.Dialogs;
using UnityEngine;

namespace Game.Features.Home
{
    /// <summary>
    /// Default <see cref="IShopNamePrompt"/> (prototype <c>nameShop</c>): name input with three ideas; the name
    /// replaces "Rosie's Corner Shop" on the home sign.
    /// </summary>
    public sealed class ShopNamePrompt : IShopNamePrompt
    {
        /// <summary>
        /// Maximum length of the shop name.
        /// </summary>
        public const int MaxNameLength = 24;

        private static readonly string[] IdeaKeys = { HomeTexts.NameIdea1, HomeTexts.NameIdea2, HomeTexts.NameIdea3 };

        private readonly IDialogService _dialogs;
        private readonly IShopProfile _shop;
        private readonly IOnboardingService _onboarding;
        private readonly IAudioService _audio;
        private readonly IOverlayService _overlay;
        private readonly ILocalizationService _texts;

        /// <summary>
        /// Creates the prompt.
        /// </summary>
        /// <param name="dialogs">Dialog cards.</param>
        /// <param name="shop">Stores the name.</param>
        /// <param name="onboarding">Stores the "asked" flag.</param>
        /// <param name="audio">Fanfare.</param>
        /// <param name="overlay">Confetti.</param>
        /// <param name="texts">Card texts, name ideas and the default name.</param>
        public ShopNamePrompt(IDialogService dialogs, IShopProfile shop, IOnboardingService onboarding, IAudioService audio,
            IOverlayService overlay, ILocalizationService texts)
        {
            _texts = texts;
            _dialogs = dialogs;
            _shop = shop;
            _onboarding = onboarding;
            _audio = audio;
            _overlay = overlay;
        }

        /// <inheritdoc/>
        public async UniTask AskAsync(CancellationToken ct)
        {
            _onboarding.SetFlag(Flags.NameAsked);
            var ideas = IdeaKeys.Select(_texts.Get).ToArray();
            var dialog = new DialogViewModel(_texts.Get(HomeTexts.NameTitle), RibbonStyle.Green)
            {
                Text = _texts.Get(HomeTexts.NameText),
                Input = new DialogInput(_texts.Get(HomeTexts.DefaultShopName), MaxNameLength, ideas),
            };
            dialog.InputText.Value = _shop.Name;
            dialog.Buttons.Add(new DialogButton(_texts.Get(HomeTexts.NameConfirm), ButtonStyle.Green, () =>
            {
                var name = (dialog.InputText.Value ?? string.Empty).Trim();
                if (name.Length > MaxNameLength)
                {
                    name = name.Substring(0, MaxNameLength);
                }
                if (name.Length == 0)
                {
                    return;
                }

                _shop.Rename(name);
                _audio.Play(SoundId.Win);
                _overlay.Burst(_overlay.PanelPoint(new Vector2(0.5f, 0.2f)), EffectKind.Confetti, 50);
            }));
            dialog.Buttons.Add(new DialogButton(_texts.Get(HomeTexts.NameKeep), ButtonStyle.WoodSmall, null));
            await _dialogs.ShowAsync(dialog, ct);
        }
    }
}
