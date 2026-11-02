using System.Linq;
using Cysharp.Threading.Tasks;
using Game.Features.Gameplay.Session;

namespace Game.Features.Gameplay.Hud
{
    /// <summary>
    /// Rosie's gifts: on a booster's unlock level the player gets it for free once per profile, its button glows and
    /// a coach tip explains it. The Undo tip waits for the first move, because Undo means nothing before it.
    /// </summary>
    public sealed class BoosterGifts
    {
        private const int GiftDelayMs = 2400;
        private const int GiftAfterMoveDelayMs = 650;
        private const float GiftTipSeconds = 4f;

        private readonly GameplayContext _context;
        private readonly BoosterBarPresenter _bar;
        private string _pendingGift;

        /// <summary>
        /// Creates the gifts.
        /// </summary>
        /// <param name="context">Shared gameplay state.</param>
        /// <param name="bar">Marks the gifted button and anchors the tip.</param>
        public BoosterGifts(GameplayContext context, BoosterBarPresenter bar)
        {
            _context = context;
            _bar = bar;
        }

        /// <summary>
        /// Gifts the booster unlocked on the level that just started. Called after the booster bar was rebuilt.
        /// </summary>
        public void OnLevelStarted()
        {
            _pendingGift = null;
            var inventory = _context.Services.Boosters;
            var unlocks = new[]
            {
                (Id: BoosterIds.Undo, Level: _context.Services.Balance.UndoUnlockLevel),
                (Id: BoosterIds.Tidy, Level: _context.Services.Balance.TidyUnlockLevel),
                (Id: BoosterIds.Pick, Level: _context.Services.Balance.PickUnlockLevel),
            };
            foreach (var unlock in unlocks.Where(u => u.Level == _context.Level && !inventory.IsGifted(u.Id)))
            {
                Gift(unlock.Id);
            }
        }

        /// <summary>
        /// Shows the gift tip that waits for the first move. Called after every accepted tap.
        /// </summary>
        public void OnMoveMade()
        {
            if (_pendingGift == null)
            {
                return;
            }

            var gift = _pendingGift;
            _pendingGift = null;
            ShowGiftAsync(gift, GiftAfterMoveDelayMs).Forget();
        }

        private void Gift(string boosterId)
        {
            var inventory = _context.Services.Boosters;
            inventory.MarkGifted(boosterId);
            if (boosterId == BoosterIds.Undo)
            {
                _pendingGift = boosterId;
                return;
            }

            inventory.Add(boosterId, _context.Services.Balance.BoosterGift);
            ShowGiftAsync(boosterId, GiftDelayMs).Forget();
        }

        private async UniTaskVoid ShowGiftAsync(string boosterId, int delayMs)
        {
            await UniTask.Delay(delayMs, cancellationToken: _context.LevelToken);
            if (_context.Services.UI.IsPopupOpen || !_bar.TryMarkGift(boosterId, out var button))
            {
                return;
            }

            var tip = _context.Services.Texts.Get(BoosterTexts.GiftOf(boosterId));
            _context.Services.Overlay.Coach(tip, button, true, GiftTipSeconds);
        }
    }
}
