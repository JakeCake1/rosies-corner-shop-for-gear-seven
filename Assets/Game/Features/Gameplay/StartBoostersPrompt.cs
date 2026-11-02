using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Features.Gameplay.Boosters;
using Game.Features.Gameplay.Session;
using Game.Services.Assets;
using Game.Services.Audio;
using Game.Services.Balance;
using Game.Services.Flow;
using Game.Services.Levels;
using Game.Services.Localization;
using Game.Services.Onboarding;
using Game.Services.UI;
using Game.Services.UI.Dialogs;
using Game.Services.Wallet;
using UnityEngine;

namespace Game.Features.Gameplay
{
    /// <summary>
    /// The popup before a level (new curve, level 11+) where the player picks start boosters
    /// (prototype <c>preLevel</c>). Called by the game flow before the gameplay scene loads.
    /// </summary>
    public sealed class StartBoostersPrompt
    {
        private const int StartGift = 2;

        private readonly IDialogService _dialogs;
        private readonly IBoosterInventory _boosters;
        private readonly IWalletService _wallet;
        private readonly IOnboardingService _onboarding;
        private readonly ILevelCurveService _curves;
        private readonly IOverlayService _overlay;
        private readonly IAudioService _audio;
        private readonly ILocalizationService _texts;
        private readonly IAssetProvider _assets;
        private readonly GameBalanceConfig _balance;

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
        /// <param name="texts">Card and tile texts.</param>
        /// <param name="assets">Loads the tile icons (<see cref="GameplayArt"/>).</param>
        /// <param name="balance">Start booster prices and the level the prompt starts at.</param>
        public StartBoostersPrompt(IDialogService dialogs, IBoosterInventory boosters, IWalletService wallet,
            IOnboardingService onboarding, ILevelCurveService curves, IOverlayService overlay, IAudioService audio,
            ILocalizationService texts, IAssetProvider assets, GameBalanceConfig balance)
        {
            _texts = texts;
            _assets = assets;
            _balance = balance;
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
        public bool IsNeeded(int level) => level >= _balance.StartBoostersFromLevel && !_curves.Active.IsOriginal;

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

            var art = await _assets.LoadAsync<GameplayArt>(GameplayArt.AddressKey, ct);
            var isFirst = !_onboarding.HasFlag(Flags.StartBoostersSeen);
            _onboarding.SetFlag(Flags.StartBoostersSeen);
            var isHard = _curves.Get(level).IsHard;
            var useBasket = false;
            var useQuick = false;
            var play = false;
            var title = _texts.Format(isHard ? GameplayTexts.StartTrickyTitle : GameplayTexts.StartTitle, level);
            var dialog = new DialogViewModel(title, isHard ? RibbonStyle.Berry : RibbonStyle.Green)
            {
                Text = _texts.Get(isFirst ? GameplayTexts.StartFirst
                    : isHard ? GameplayTexts.StartTricky : GameplayTexts.StartNormal),
            };
            dialog.Tiles.Add(Tile(art.Basket, GameplayTexts.StartBasketName, GameplayTexts.StartBasketInfo,
                BoosterIds.StartBasket, _balance.ExtraBasketPrice,
                on => useBasket = Select(on, BoosterIds.StartBasket, _balance.ExtraBasketPrice)));
            dialog.Tiles.Add(Tile(art.Pick, GameplayTexts.StartQuickName, GameplayTexts.StartQuickInfo,
                BoosterIds.StartQuick, _balance.BoosterPrice,
                on => useQuick = Select(on, BoosterIds.StartQuick, _balance.BoosterPrice)));
            dialog.Buttons.Add(new DialogButton(_texts.Get(GameplayTexts.StartPlay), ButtonStyle.Green, () => play = true));
            dialog.Buttons.Add(new DialogButton(_texts.Get(GameplayTexts.StartBack), ButtonStyle.WoodSmall, null));
            await _dialogs.ShowAsync(dialog, ct);
            if (!play)
            {
                return null;
            }

            if (useBasket)
            {
                _boosters.AddStart(BoosterIds.StartBasket, -1);
            }
            if (useQuick)
            {
                _boosters.AddStart(BoosterIds.StartQuick, -1);
            }
            return new StartBoosters(useBasket, useQuick);
        }

        /// <summary>
        /// A start booster tile: the badge shows the owned count, or the price in coins when none is owned
        /// (prototype <c>.preSlot .pc</c>).
        /// </summary>
        private DialogTile Tile(Sprite icon, string nameKey, string infoKey, string id, int price,
            Func<bool, bool> changed)
        {
            return new DialogTile(icon, _texts.Get(nameKey), _texts.Get(infoKey), changed,
                () => _boosters.GetStart(id) > 0 ? _boosters.GetStart(id).ToString() : price.ToString(),
                () => _boosters.GetStart(id) <= 0);
        }

        private bool Select(bool isOn, string id, int price)
        {
            if (!isOn)
            {
                return false;
            }
            if (_boosters.GetStart(id) > 0)
            {
                _audio.Play(SoundId.Clink);
                return true;
            }

            if (!_wallet.TrySpend(price))
            {
                _overlay.Toast(_texts.Get(GameplayTexts.NotEnoughCoins), true);
                return false;
            }

            _boosters.AddStart(id, 1);
            _audio.Play(SoundId.Coin);
            return true;
        }
    }
}
