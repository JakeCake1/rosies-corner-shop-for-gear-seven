using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Features.Gameplay.Boosters;
using Game.Features.Gameplay.Session;
using Game.Services.Audio;
using Game.Services.Flow;
using Game.Services.Levels;
using Game.Services.Onboarding;
using Game.Services.UI;
using Game.Services.UI.Dialogs;
using Game.Services.Wallet;

namespace Game.Features.Gameplay
{
    /// <summary>
    /// The popup before a level (new curve, level 11+) where the player picks start boosters
    /// (prototype <c>preLevel</c>). Called by the game flow before the gameplay scene loads.
    /// </summary>
    public sealed class StartBoostersPrompt
    {
        private const int ExtraBasketPrice = GameplayBalance.ExtraBasketPrice;
        private const int QuickPickPrice = GameplayBalance.BoosterPrice;
        private const int StartGift = 2;

        private readonly IDialogService _dialogs;
        private readonly IBoosterInventory _boosters;
        private readonly IWalletService _wallet;
        private readonly IOnboardingService _onboarding;
        private readonly ILevelCurveService _curves;
        private readonly IOverlayService _overlay;
        private readonly IAudioService _audio;

        /// <summary>
        /// Creates the prompt.
        /// </summary>
        /// <param name="dialogs">Dialog cards.</param>
        /// <param name="boosters">Owned start boosters.</param>
        /// <param name="wallet">Pays for start boosters the player doesn't own.</param>
        /// <param name="onboarding">First-time gift and popup flags.</param>
        /// <param name="curves">Whether the level is tricky.</param>
        /// <param name="overlay">"Not enough coins" toast.</param>
        /// <param name="audio">Coin and click sounds.</param>
        public StartBoostersPrompt(IDialogService dialogs, IBoosterInventory boosters, IWalletService wallet,
            IOnboardingService onboarding, ILevelCurveService curves, IOverlayService overlay, IAudioService audio)
        {
            _dialogs = dialogs;
            _boosters = boosters;
            _wallet = wallet;
            _onboarding = onboarding;
            _curves = curves;
            _overlay = overlay;
            _audio = audio;
        }

        /// <summary>
        /// Whether the prompt applies to a level.
        /// </summary>
        /// <param name="level">Level number.</param>
        /// <returns>True from level 11 on the new curve.</returns>
        public bool IsNeeded(int level) => level >= GameplayBalance.StartBoostersFromLevel && !_curves.Active.IsOriginal;

        /// <summary>
        /// Shows the prompt. The first time Rosie gives two of each start booster.
        /// </summary>
        /// <param name="level">Level about to start.</param>
        /// <param name="ct">Cancelled with the flow.</param>
        /// <returns>The chosen boosters (already deducted), or null when the player went back to the shop.</returns>
        /// <exception cref="System.OperationCanceledException">The prompt was cancelled.</exception>
        public async UniTask<StartBoosters> AskAsync(int level, CancellationToken ct)
        {
            if (!_onboarding.HasFlag(Flags.StartBoosterGift))
            {
                _boosters.AddStart(BoosterIds.StartBasket, StartGift);
                _boosters.AddStart(BoosterIds.StartQuick, StartGift);
                _onboarding.SetFlag(Flags.StartBoosterGift);
            }

            var isFirst = !_onboarding.HasFlag(Flags.StartBoostersSeen);
            _onboarding.SetFlag(Flags.StartBoostersSeen);
            var isHard = _curves.Get(level).IsHard;
            var useBasket = false;
            var useQuick = false;
            var play = false;
            var dialog = new DialogViewModel(isHard ? "Tricky level " + level : "Level " + level,
                isHard ? RibbonStyle.Berry : RibbonStyle.Green)
            {
                Text = isFirst
                    ? "New: start boosters! Rosie gives you two of each. Pick what you want to begin the level with."
                    : isHard ? "A tricky one. A start booster helps!" : "Get ready. Pick a start booster if you like.",
            };
            dialog.Toggles.Add(new DialogToggle(Label("Extra basket", "start with three baskets", BoosterIds.StartBasket,
                ExtraBasketPrice), false, on => useBasket = Select(on, BoosterIds.StartBasket, ExtraBasketPrice)));
            dialog.Toggles.Add(new DialogToggle(Label("Quick Pick", "one extra for this level", BoosterIds.StartQuick,
                QuickPickPrice), false, on => useQuick = Select(on, BoosterIds.StartQuick, QuickPickPrice)));
            dialog.Buttons.Add(new DialogButton("Play", ButtonStyle.Green, () => play = true));
            dialog.Buttons.Add(new DialogButton("Back to the shop", ButtonStyle.WoodSmall, null));
            await _dialogs.ShowAsync(dialog, ct);
            if (!play) return null;

            if (useBasket) _boosters.AddStart(BoosterIds.StartBasket, -1);
            if (useQuick) _boosters.AddStart(BoosterIds.StartQuick, -1);
            return new StartBoosters(useBasket, useQuick);
        }

        private string Label(string name, string sub, string id, int price)
        {
            var owned = _boosters.GetStart(id);
            var cost = owned > 0 ? $"{owned} left" : $"{price} coins";
            return $"<b>{name}</b>\n<size=75%>{sub} · {cost}</size>";
        }

        private bool Select(bool isOn, string id, int price)
        {
            if (!isOn) return false;
            if (_boosters.GetStart(id) > 0)
            {
                _audio.Play(SoundId.Clink);
                return true;
            }

            if (!_wallet.TrySpend(price))
            {
                _overlay.Toast("Not enough coins", true);
                return false;
            }

            _boosters.AddStart(id, 1);
            _audio.Play(SoundId.Coin);
            return true;
        }
    }
}
