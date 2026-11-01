using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Services.Audio;
using Game.Services.Flow;
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

        private static readonly string[] Ideas = { "Rosie & Me", "Maple Street Market", "The Corner Pantry" };

        private readonly IDialogService _dialogs;
        private readonly IShopProfile _shop;
        private readonly IOnboardingService _onboarding;
        private readonly IAudioService _audio;
        private readonly IOverlayService _overlay;

        /// <summary>
        /// Creates the prompt.
        /// </summary>
        /// <param name="dialogs">Dialog cards.</param>
        /// <param name="shop">Stores the name.</param>
        /// <param name="onboarding">Stores the "asked" flag.</param>
        /// <param name="audio">Fanfare.</param>
        /// <param name="overlay">Confetti.</param>
        public ShopNamePrompt(IDialogService dialogs, IShopProfile shop, IOnboardingService onboarding, IAudioService audio,
            IOverlayService overlay)
        {
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
            var dialog = new DialogViewModel("You're my partner now!", RibbonStyle.Green)
            {
                Text = "Rosie: \"Ten busy days! Let's put your name on the sign. What shall we call the shop?\"",
                Input = new DialogInput("Rosie's Corner Shop", MaxNameLength, Ideas),
            };
            dialog.InputText.Value = _shop.Name;
            dialog.Buttons.Add(new DialogButton("Hang the sign", ButtonStyle.Green, () =>
            {
                var name = (dialog.InputText.Value ?? string.Empty).Trim();
                if (name.Length > MaxNameLength) name = name.Substring(0, MaxNameLength);
                if (name.Length == 0) return;

                _shop.Rename(name);
                _audio.Play(SoundId.Win);
                _overlay.Burst(new Vector3(Screen.width / 2f, Screen.height * 0.8f, 0f), EffectKind.Confetti, 50);
            }));
            dialog.Buttons.Add(new DialogButton("Keep Rosie's name", ButtonStyle.WoodSmall, null));
            await _dialogs.ShowAsync(dialog, ct);
        }
    }
}
