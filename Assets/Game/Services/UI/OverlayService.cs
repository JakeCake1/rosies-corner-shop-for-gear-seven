using UnityEngine;

namespace Game.Services.UI
{
    /// <summary>
    /// <see cref="IOverlayService"/> that forwards to the overlay widgets held by <see cref="UIRoot"/>.
    /// </summary>
    public sealed class OverlayService : IOverlayService
    {
        private readonly UIRoot _root;

        /// <summary>
        /// Creates the service.
        /// </summary>
        /// <param name="root">Holds the toast, coach, effects and background widgets.</param>
        public OverlayService(UIRoot root)
        {
            _root = root;
        }

        /// <inheritdoc/>
        public void Toast(string message, bool isWarning = false) => _root.Toast.Show(message, isWarning);

        /// <inheritdoc/>
        public void Coach(string message, RectTransform anchor, bool isAbove, float seconds)
        {
            if (anchor != null) _root.Coach.ShowBubble(message, anchor, isAbove, seconds);
        }

        /// <inheritdoc/>
        public void PointAt(RectTransform target)
        {
            if (target != null) _root.Coach.ShowHand(target);
        }

        /// <inheritdoc/>
        public void ClearCoach() => _root.Coach.Clear();

        /// <inheritdoc/>
        public void Burst(Vector3 worldPosition, EffectKind kind, int count, Color[] colors = null) =>
            _root.Effects.Burst(worldPosition, kind, count, colors);

        /// <inheritdoc/>
        public void Praise(Vector3 worldPosition, string text, bool isCombo) =>
            _root.Effects.Praise(worldPosition, text, isCombo);

        /// <inheritdoc/>
        public void SetBackground(bool isTricky, bool hasBunting)
        {
            _root.Background.SetTricky(isTricky);
            _root.Background.SetBunting(hasBunting);
        }
    }
}
