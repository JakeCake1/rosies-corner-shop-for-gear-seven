using System;
using Cysharp.Threading.Tasks;
using Game.Features.Gameplay.Coach;
using Game.Features.Gameplay.Offers;
using Game.Features.Gameplay.Session;

namespace Game.Features.Gameplay.Hud
{
    /// <summary>
    /// What a booster does when used: runs it on the session, or opens its purchase offer when the player owns none.
    /// </summary>
    /// <remarks>
    /// Used by the booster bar buttons and by the loss dialog (Undo, Tidy).
    /// </remarks>
    public sealed class BoosterActions
    {
        private readonly GameplayContext _context;
        private readonly PurchaseOffers _offers;
        private readonly IdleHint _idle;
        private readonly Action _undoDeclinedWhileOver;

        /// <summary>
        /// Creates the actions.
        /// </summary>
        /// <param name="context">Shared gameplay state.</param>
        /// <param name="offers">Opens purchase offers for boosters and undos.</param>
        /// <param name="idle">Restarted on every booster use.</param>
        /// <param name="undoDeclinedWhileOver">Shows the loss dialog again when the undo offer of a lost level is declined.</param>
        public BoosterActions(GameplayContext context, PurchaseOffers offers, IdleHint idle, Action undoDeclinedWhileOver)
        {
            _context = context;
            _offers = offers;
            _idle = idle;
            _undoDeclinedWhileOver = undoDeclinedWhileOver;
        }

        /// <summary>
        /// Runs a booster, or opens its purchase offer when the player has none. Only Tidy works on a finished level.
        /// </summary>
        /// <param name="boosterId">One of <see cref="BoosterIds"/>.</param>
        public void Use(string boosterId)
        {
            _context.Services.Overlay.ClearCoach();
            _idle.Touch();
            if (boosterId == BoosterIds.Undo)
            {
                Undo();
                return;
            }

            if (_context.Session.IsOver && boosterId != BoosterIds.Tidy)
            {
                return;
            }

            if (OwnedCount(boosterId) <= 0)
            {
                _offers.OfferBoosterAsync(boosterId).Forget();
                return;
            }

            Run(boosterId);
        }

        /// <summary>
        /// Undoes the last move, or offers more undos when the free ones are used up.
        /// </summary>
        public void Undo()
        {
            switch (_context.Session.Undo())
            {
                case UndoResult.Done:
                    _context.Services.Dialogs.CloseAllAsync().Forget();
                    Toast(GameplayTexts.UndoDone);
                    break;
                case UndoResult.NothingToUndo:
                    Toast(GameplayTexts.UndoNone);
                    break;
                case UndoResult.NoUndosLeft:
                    _offers.OfferUndoAsync(Undo, _undoDeclinedWhileOver).Forget();
                    break;
            }
        }

        /// <summary>
        /// Returns how many of a booster the player can use now: the inventory plus this level's bonus Quick Picks.
        /// </summary>
        /// <param name="boosterId">Pick, Tidy or Shuffle; Undo counts are kept by the session.</param>
        /// <returns>Zero or more.</returns>
        public int OwnedCount(string boosterId)
        {
            var bonus = boosterId == BoosterIds.Pick ? _context.Session.BonusPick.Value : 0;
            return _context.Services.Boosters.Get(boosterId) + bonus;
        }

        private void Run(string boosterId)
        {
            var session = _context.Session;
            switch (boosterId)
            {
                case BoosterIds.Pick:
                    Toast(session.UseQuickPick() ? GameplayTexts.PickDone : GameplayTexts.PickNone);
                    break;
                case BoosterIds.Tidy:
                    Toast(session.UseTidy() ? GameplayTexts.TidyDone : GameplayTexts.TidyNone);
                    break;
                case BoosterIds.Shuffle:
                    if (!session.UseShuffle())
                    {
                        Toast(GameplayTexts.ShuffleNone);
                    }

                    break;
            }
        }

        private void Toast(string key)
        {
            _context.Services.Overlay.Toast(_context.Services.Texts.Get(key));
        }
    }
}
