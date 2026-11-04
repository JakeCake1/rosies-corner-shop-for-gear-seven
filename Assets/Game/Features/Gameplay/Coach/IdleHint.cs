using Game.Features.Gameplay.Session;
using UnityEngine;

namespace Game.Features.Gameplay.Coach
{
    /// <summary>
    /// The hidden hint: after a few idle seconds the best move glows (levels 1–5 and levels the player has lost).
    /// </summary>
    /// <remarks>
    /// Ticked every frame by <see cref="GameplayPresenter"/>; every tap and booster use calls <see cref="Touch"/>.
    /// </remarks>
    public sealed class IdleHint
    {
        private readonly GameplayContext _context;
        private float _lastActionTime;
        private bool _isHintShown;

        /// <summary>
        /// Creates the hint.
        /// </summary>
        /// <param name="context">Shared gameplay state.</param>
        public IdleHint(GameplayContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Restarts the idle timer; called on level start, every tap and every booster use.
        /// </summary>
        public void Touch()
        {
            _lastActionTime = Time.unscaledTime;
            if (_isHintShown)
            {
                _isHintShown = false;
                _context.Board.SetHint(0);
            }
        }

        /// <summary>
        /// Moves the glow to the best move of the changed board (a shipment or booster changed it, or a resize
        /// rebuilt the board and dropped the glow). Does nothing while no hint is shown.
        /// </summary>
        public void Refresh()
        {
            if (!_isHintShown)
            {
                return;
            }

            var hint = _context.Session.BestHint();
            _isHintShown = hint != null;
            _context.Board.SetHint(hint?.Id ?? 0);
        }

        /// <summary>
        /// Shows the best move once the player has been idle long enough; it stays lit until the next <see cref="Touch"/>.
        /// </summary>
        public void Tick()
        {
            if (_isHintShown || !IsAllowed() || Time.unscaledTime - _lastActionTime < _context.Services.Balance.HintIdleSeconds)
            {
                return;
            }

            var hint = _context.Session.BestHint();
            if (hint == null)
            {
                // Nothing to show: wait another idle period before looking again.
                _lastActionTime = Time.unscaledTime;
                return;
            }

            _isHintShown = true;
            _context.Board.SetHint(hint.Id);
        }

        private bool IsAllowed()
        {
            var session = _context.Session;
            if (session.State == null || session.IsOver || _context.Services.UI.IsPopupOpen)
            {
                return false;
            }

            var level = _context.Level;
            return level <= _context.Services.Balance.HintUntilLevel || _context.Services.Levels.HasFailed(level);
        }
    }
}
