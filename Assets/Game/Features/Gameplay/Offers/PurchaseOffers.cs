using System;
using Cysharp.Threading.Tasks;
using Game.Features.Gameplay.Hud;
using Game.Features.Gameplay.Session;
using Game.Services.Audio;
using Game.Services.Onboarding;
using Game.Services.UI.Dialogs;

namespace Game.Features.Gameplay.Offers
{
    /// <summary>
    /// Purchase offers of the gameplay screen (more undos, a booster, the extra basket) and the dialog buttons
    /// they share: buy for coins, watch an ad, "Not now".
    /// </summary>
    /// <remarks>
    /// Dialogs and ads are bound to the level token: restarting the level closes them and drops late ad rewards.
    /// </remarks>
    public sealed class PurchaseOffers
    {
        private readonly GameplayContext _context;

        /// <summary>
        /// Creates the offers.
        /// </summary>
        /// <param name="context">Shared gameplay state.</param>
        public PurchaseOffers(GameplayContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Offers more undos after the free ones are used up.
        /// </summary>
        /// <param name="undo">Undoes the last move once the player got more undos.</param>
        /// <param name="declinedWhileOver">
        /// Called when the player declines and the level was already lost when the offer opened, so the loss dialog
        /// comes back.
        /// </param>
        /// <returns>Completes when the dialog closes.</returns>
        public async UniTaskVoid OfferUndoAsync(Action undo, Action declinedWhileOver)
        {
            var session = _context.Session;
            var wasOver = session.IsOver;
            var dialog = CreateDialog(GameplayTexts.BoosterUndo, RibbonStyle.Berry, GameplayTexts.UndoOffer);
            dialog.Icon = _context.Art.Undo;
            dialog.Buttons.Add(BuyButton(GameplayTexts.BuyTwo, GameplayBalance.UndoPackPrice, () =>
            {
                _context.Services.Audio.Play(SoundId.Coin);
                session.AddUndos(GameplayBalance.UndoPackSize);
                undo();
            }));
            AddAdButton(dialog, GameplayTexts.GetOneFree, () =>
            {
                session.AddUndos(1);
                undo();
            });
            dialog.Buttons.Add(NotNowButton(() =>
            {
                if (wasOver && session.IsOver)
                {
                    declinedWhileOver();
                }
            }));
            await _context.Services.Dialogs.ShowAsync(dialog, _context.LevelToken);
        }

        /// <summary>
        /// Offers one booster when the player taps a booster they don't own.
        /// </summary>
        /// <param name="boosterId">Pick, Tidy or Shuffle.</param>
        /// <returns>Completes when the dialog closes.</returns>
        public async UniTaskVoid OfferBoosterAsync(string boosterId)
        {
            var services = _context.Services;
            var title = services.Texts.Get(BoosterTexts.NameOf(boosterId));
            var dialog = CreateDialog(BoosterTexts.NameOf(boosterId), RibbonStyle.Berry, BoosterTexts.InfoOf(boosterId));
            dialog.Icon = _context.Art.IconOf(boosterId);
            dialog.Buttons.Add(BuyButton(GameplayTexts.BuyOne, GameplayBalance.BoosterPrice, () =>
            {
                services.Audio.Play(SoundId.Coin);
                services.Boosters.Add(boosterId, 1);
            }));
            AddAdButton(dialog, GameplayTexts.GetOneFree, () =>
            {
                services.Boosters.Add(boosterId, 1);
                services.Overlay.Toast(services.Texts.Format(GameplayTexts.BoosterAdded, title));
            });
            dialog.Buttons.Add(NotNowButton(null));
            await services.Dialogs.ShowAsync(dialog, _context.LevelToken);
        }

        /// <summary>
        /// Opens the extra basket: the first time for free, later for coins or an ad. Ignored while a popup is open
        /// or the level is over.
        /// </summary>
        /// <returns>Completes when the basket opened or the dialog closed.</returns>
        public async UniTaskVoid OfferExtraBasketAsync()
        {
            var services = _context.Services;
            if (services.UI.IsPopupOpen || _context.Session.IsOver)
            {
                return;
            }

            services.Overlay.ClearCoach();
            if (!services.Onboarding.HasFlag(Flags.FreeBasket))
            {
                OpenFirstBasketForFree();
                return;
            }

            var dialog = CreateDialog(GameplayTexts.BasketTitle, RibbonStyle.Green, GameplayTexts.BasketText);
            dialog.Icon = _context.Art.Basket;
            dialog.Buttons.Add(BuyButton(GameplayTexts.BasketOpen, GameplayBalance.ExtraBasketPrice,
                _context.Session.OpenExtraBasket));
            AddAdButton(dialog, GameplayTexts.BasketOpenFree, _context.Session.OpenExtraBasket);
            dialog.Buttons.Add(NotNowButton(null));
            await services.Dialogs.ShowAsync(dialog, _context.LevelToken);
        }

        /// <summary>
        /// Adds a green "watch an ad" button that grants <paramref name="reward"/> after a completed ad. Does nothing
        /// before the level where ads start.
        /// </summary>
        /// <param name="dialog">The dialog being built.</param>
        /// <param name="labelKey">Key of the button label.</param>
        /// <param name="reward">Granted only when the ad was watched to the end.</param>
        public void AddAdButton(DialogViewModel dialog, string labelKey, Action reward)
        {
            if (!_context.HasAds)
            {
                return;
            }

            var label = _context.Services.Texts.Get(labelKey);
            dialog.Buttons.Add(new DialogButton(label, ButtonStyle.Green, () => RewardAsync(reward).Forget())
            {
                Badge = ButtonBadge.Ad,
            });
        }

        /// <summary>
        /// Creates a gold "buy for coins" button, disabled while the player can't afford it.
        /// </summary>
        /// <param name="labelKey">Key of the button label.</param>
        /// <param name="price">Price in coins.</param>
        /// <param name="onPaid">Called only after the coins were spent.</param>
        /// <returns>The button; the caller adds it to its dialog.</returns>
        public DialogButton BuyButton(string labelKey, int price, Action onPaid)
        {
            var wallet = _context.Services.Wallet;
            var button = new DialogButton(_context.Services.Texts.Get(labelKey), ButtonStyle.Gold, () =>
            {
                if (wallet.TrySpend(price))
                {
                    onPaid();
                }
            }) { Badge = ButtonBadge.Coins, Price = price };
            button.IsEnabled.Value = wallet.Coins.Value >= price;
            return button;
        }

        private DialogViewModel CreateDialog(string titleKey, RibbonStyle ribbon, string textKey)
        {
            var texts = _context.Services.Texts;
            return new DialogViewModel(texts.Get(titleKey), ribbon) { Text = texts.Get(textKey) };
        }

        private DialogButton NotNowButton(Action onClick)
        {
            return new DialogButton(_context.Services.Texts.Get(GameplayTexts.NotNow), ButtonStyle.WoodSmall, onClick);
        }

        private void OpenFirstBasketForFree()
        {
            _context.Services.Onboarding.SetFlag(Flags.FreeBasket);
            _context.Session.OpenExtraBasket();
            _context.Services.Overlay.Toast(_context.Services.Texts.Get(GameplayTexts.BasketFree));
        }

        private async UniTaskVoid RewardAsync(Action reward)
        {
            if (await _context.Services.Ads.ShowAsync(_context.LevelToken))
            {
                reward();
            }
        }
    }
}
