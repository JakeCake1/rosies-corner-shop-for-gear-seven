using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Game.Features.Gameplay.Hud;
using Game.Features.Gameplay.Model;
using Game.Features.Gameplay.Offers;
using Game.Features.Gameplay.Session;
using Game.Services.UI.Dialogs;

namespace Game.Features.Gameplay.Results
{
    /// <summary>
    /// The "Counter full" loss dialog: progress, the items stuck on the counter and the ways out (an ad offer of the
    /// active experiment arm, a free undo, Tidy, start over).
    /// </summary>
    public sealed class LevelLossPresenter
    {
        private const float ManyLeftShare = 0.35f;
        private const string BasketOffer = "basket";
        private const string SlotOffer = "slot";

        private readonly GameplayContext _context;
        private readonly PurchaseOffers _offers;
        private readonly BoosterActions _boosters;
        private readonly Action _restart;

        /// <summary>
        /// Creates the presenter.
        /// </summary>
        /// <param name="context">Shared gameplay state.</param>
        /// <param name="offers">Adds the ad and coin buttons.</param>
        /// <param name="boosters">Runs Undo and Tidy from the dialog.</param>
        /// <param name="restart">Restarts the level in play (Start over).</param>
        public LevelLossPresenter(GameplayContext context, PurchaseOffers offers, BoosterActions boosters,
            Action restart)
        {
            _context = context;
            _offers = offers;
            _boosters = boosters;
            _restart = restart;
        }

        /// <summary>
        /// Shows the dialog. Called when the level is lost and again when the player declines the undo offer of a
        /// lost level.
        /// </summary>
        /// <param name="itemsLeft">Items still to ship; selects the caption.</param>
        /// <returns>Completes when the dialog closes; cancelled with the level.</returns>
        public async UniTask ShowAsync(int itemsLeft)
        {
            var texts = _context.Services.Texts;
            var st = _context.Session.State;
            var dialog = new DialogViewModel(texts.Get(GameplayTexts.LossTitle), RibbonStyle.Berry)
            {
                Progress = st.Collected / (float)st.Total,
                ProgressCaption = texts.Format(CaptionOf(itemsLeft, st.Total), itemsLeft),
            };
            dialog.Items.AddRange(st.Counter.Select(j => _context.Services.Content.GetItemSprite(j.Type)));
            AddAdOffer(dialog);
            AddUndo(dialog);
            AddTidy(dialog);
            dialog.Buttons.Add(new DialogButton(texts.Get(GameplayTexts.StartOver), ButtonStyle.WoodSmall, _restart));
            await _context.Services.Dialogs.ShowAsync(dialog, _context.LevelToken);
        }

        private static string CaptionOf(int itemsLeft, int total)
        {
            if (itemsLeft / (float)total > ManyLeftShare)
            {
                return GameplayTexts.LossLeft;
            }

            return itemsLeft == 1 ? GameplayTexts.LossOneLeft : GameplayTexts.LossFewLeft;
        }

        private string ChooseAdOffer()
        {
            var st = _context.Session.State;
            if (!_context.HasAds)
            {
                return null;
            }

            if (_context.Services.Experiments.LossOffer == BasketOffer && st.UnlockedBaskets < LevelState.MaxBaskets)
            {
                return BasketOffer;
            }

            return st.Capacity < _context.Services.Balance.ExtendedCapacity ? SlotOffer : null;
        }

        private void AddAdOffer(DialogViewModel dialog)
        {
            var services = _context.Services;
            var session = _context.Session;
            switch (ChooseAdOffer())
            {
                case BasketOffer:
                    _offers.AddAdButton(dialog, GameplayTexts.CallCustomer, () =>
                    {
                        session.OpenExtraBasket();
                        services.Overlay.Toast(services.Texts.Get(GameplayTexts.CustomerArrived));
                    });
                    dialog.Note = services.Texts.Get(GameplayTexts.CustomerNote);
                    break;
                case SlotOffer:
                    _offers.AddAdButton(dialog, GameplayTexts.ExtraSpot, () =>
                    {
                        session.ExtendCounter();
                        services.Overlay.Toast(services.Texts.Get(GameplayTexts.SpotAdded));
                    });
                    break;
            }
        }

        private void AddUndo(DialogViewModel dialog)
        {
            var session = _context.Session;
            if (!session.IsUnlocked(BoosterIds.Undo) || !session.CanUndo.Value || session.UndoLeft.Value <= 0)
            {
                return;
            }

            var label = _context.Services.Texts.Get(GameplayTexts.UndoFree);
            dialog.Buttons.Add(new DialogButton(label, ButtonStyle.Wood, _boosters.Undo));
        }

        private void AddTidy(DialogViewModel dialog)
        {
            if (!_context.Session.IsUnlocked(BoosterIds.Tidy))
            {
                return;
            }

            var texts = _context.Services.Texts;
            var owned = _context.Services.Boosters.Get(BoosterIds.Tidy);
            if (owned > 0)
            {
                var label = texts.Format(GameplayTexts.TidyLeft, owned);
                dialog.Buttons.Add(new DialogButton(label, ButtonStyle.Gold, () => _boosters.Use(BoosterIds.Tidy)));
                return;
            }

            dialog.Buttons.Add(_offers.BuyButton(GameplayTexts.BoosterTidy, _context.Services.Balance.BoosterPrice, () =>
            {
                _context.Services.Boosters.Add(BoosterIds.Tidy, 1);
                _boosters.Use(BoosterIds.Tidy);
            }));
        }
    }
}
