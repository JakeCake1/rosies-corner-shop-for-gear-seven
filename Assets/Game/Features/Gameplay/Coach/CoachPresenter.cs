using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Game.Features.Gameplay.Model;
using Game.Features.Gameplay.Session;
using Game.Services.Album;
using Game.Services.Onboarding;
using UnityEngine.UIElements;

namespace Game.Features.Gameplay.Coach
{
    /// <summary>
    /// Shows the session's notices (toasts and first-time coach tips), the level 1 tutorial and the extra-basket tip.
    /// </summary>
    /// <remarks>
    /// Coach tips are queued so two never overlap, wait while a popup is open and are shown once per profile.
    /// Everything delayed is bound to the level token, so nothing of an old level shows over a new one.
    /// </remarks>
    public sealed class CoachPresenter
    {
        private const string FamilyNoticePrefix = "family:";
        private const float TipSeconds = 4.5f;
        private const float TipGapSeconds = 0.5f;
        private const int TutorialDelayMs = 2300;
        private const int BasketTipDelayMs = 5200;
        private const float BasketTipSeconds = 6f;

        private readonly GameplayContext _context;
        private readonly Queue<(string Id, string Text, VisualElement Anchor, bool IsAbove)> _tipQueue =
            new Queue<(string, string, VisualElement, bool)>();
        private int _tipGeneration;
        private bool _isTipLoopRunning;
        private bool _isTutorialShown;

        /// <summary>
        /// Creates the presenter.
        /// </summary>
        /// <param name="context">Shared gameplay state.</param>
        public CoachPresenter(GameplayContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Hides the coach and drops the tips of the previous level. Called before the session starts a level.
        /// </summary>
        public void Reset()
        {
            _context.Services.Overlay.ClearCoach();
            _tipQueue.Clear();
            _tipGeneration++;
            _isTipLoopRunning = false;
            _isTutorialShown = false;
        }

        /// <summary>
        /// Schedules the tutorial (level 1) and the extra-basket tip (the level that introduces the extra basket).
        /// Called right after the session started a level.
        /// </summary>
        public void OnLevelStarted()
        {
            if (_context.Level == 1)
            {
                ShowTutorialAsync().Forget();
            }

            if (_context.Level == _context.Services.Balance.ExtraBasketSlotLevel
                && !_context.Services.Onboarding.HasFlag(Flags.FreeBasket))
            {
                ShowBasketTipAsync().Forget();
            }
        }

        /// <summary>
        /// Shows one notice of <see cref="LevelSession.Notices"/>: a toast or a first-time coach tip.
        /// </summary>
        /// <param name="notice">Notice id sent by the session ("oneLeft", "counter", "family:dairy", ...).</param>
        public void OnNotice(string notice)
        {
            var board = _context.Board;
            switch (notice)
            {
                case "oneLeft":
                    Toast(GameplayTexts.OneSpotLeft, true);
                    break;
                case "counter":
                    QueueTip("counter", GameplayTexts.TipCounter, board.Counter, false);
                    break;
                case "stock":
                    QueueTip("stock", GameplayTexts.TipStock, board.StockLabels, false);
                    break;
                case "ship":
                    QueueTip("ship", GameplayTexts.TipShip, board.BasketArea, false);
                    break;
                default:
                    if (notice.StartsWith(FamilyNoticePrefix))
                    {
                        ShowFamilyComplete(notice.Substring(FamilyNoticePrefix.Length));
                    }

                    break;
            }
        }

        private void Toast(string key, bool isWarning)
        {
            _context.Services.Overlay.Toast(_context.Services.Texts.Get(key), isWarning);
        }

        private void ShowFamilyComplete(string familyId)
        {
            var texts = _context.Services.Texts;
            var family = texts.GetFamilyName(familyId);
            _context.Services.Overlay.Toast(texts.Format(GameplayTexts.FamilyComplete, family, AlbumService.FamilyBonus));
        }

        private void QueueTip(string tipId, string textKey, VisualElement anchor, bool isAbove)
        {
            var onboarding = _context.Services.Onboarding;
            if (_context.Level > _context.Services.Balance.CoachTipsUntilLevel || onboarding.HasSeenTip(tipId))
            {
                return;
            }

            if (_tipQueue.Any(tip => tip.Id == tipId))
            {
                return;
            }

            _tipQueue.Enqueue((tipId, _context.Services.Texts.Get(textKey), anchor, isAbove));
            if (!_isTipLoopRunning)
            {
                _isTipLoopRunning = true;
                ShowNextTipAsync(_tipGeneration).Forget();
            }
        }

        // One loop at a time: the running flag is set synchronously by the caller, so two notices of one tap
        // (counter + stock) queue behind each other instead of starting two loops that overwrite each other's tip.
        // The generation keeps a loop cancelled by Reset from clearing the flag of the next level's loop.
        private async UniTaskVoid ShowNextTipAsync(int generation)
        {
            var ct = _context.LevelToken;
            try
            {
                while (_tipQueue.Count > 0)
                {
                    await UniTask.WaitUntil(() => !_context.Services.UI.IsPopupOpen, cancellationToken: ct);
                    var (id, text, anchor, isAbove) = _tipQueue.Dequeue();
                    _context.Services.Onboarding.MarkTipSeen(id);
                    _context.Services.Overlay.Coach(text, anchor, isAbove, TipSeconds);
                    await UniTask.Delay(TimeSpan.FromSeconds(TipSeconds + TipGapSeconds), cancellationToken: ct);
                }
            }
            finally
            {
                if (generation == _tipGeneration)
                {
                    _isTipLoopRunning = false;
                }
            }
        }

        private async UniTaskVoid ShowTutorialAsync()
        {
            await UniTask.Delay(TutorialDelayMs, cancellationToken: _context.LevelToken);
            var session = _context.Session;
            if (_isTutorialShown || session.State.Moves > 0 || session.LevelNumber != 1)
            {
                return;
            }

            var basket = session.State.Baskets.FirstOrDefault(b => b != null);
            if (basket == null)
            {
                return;
            }

            var target = LevelRules.VisibleJars(session.State, false).FirstOrDefault(v => v.Jar.Type == basket.Type);
            var rect = target.Jar != null ? _context.Board.FindItem(target.Jar.Id) : null;
            if (rect == null)
            {
                return;
            }

            _isTutorialShown = true;
            PointAtFirstItem(target.Jar.Id, rect, basket.Type);
        }

        private void PointAtFirstItem(int jarId, VisualElement rect, int itemType)
        {
            var texts = _context.Services.Texts;
            var overlay = _context.Services.Overlay;
            _context.Board.SetHint(jarId);
            overlay.PointAt(rect);
            overlay.Coach(texts.Format(GameplayTexts.Tutorial, texts.GetItemName(itemType)), rect, true, 0f);
        }

        private async UniTaskVoid ShowBasketTipAsync()
        {
            await UniTask.Delay(BasketTipDelayMs, cancellationToken: _context.LevelToken);
            var plus = _context.Board.FirstBasketLock();
            if (plus == null || _context.Services.UI.IsPopupOpen || _context.Level != _context.Services.Balance.ExtraBasketSlotLevel)
            {
                return;
            }

            var text = _context.Services.Texts.Get(GameplayTexts.BasketTip);
            _context.Services.Overlay.Coach(text, plus, false, BasketTipSeconds);
        }
    }
}
