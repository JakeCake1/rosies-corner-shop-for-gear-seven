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
        }

        /// <summary>
        /// Shows the best move once the player has been idle long enough; then waits another idle period.
        /// </summary>
        public void Tick()
        {
            if (!IsAllowed() || Time.unscaledTime - _lastActionTime < _context.Services.Balance.HintIdleSeconds)
            {
                return;
            }

            _lastActionTime = Time.unscaledTime;
            var hint = _context.Session.BestHint();
            if (hint != null)
            {
                _context.Board.SetHint(hint.Id);
            }
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
