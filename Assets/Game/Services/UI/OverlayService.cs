using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Services.UI
{
    /// <summary>
    /// Forwards overlay requests to the views held by <see cref="UIRoot"/>.
    /// </summary>
    public sealed class OverlayService : IOverlayService
    {
        private readonly UIRoot _root;

        /// <summary>
        /// Creates the service.
        /// </summary>
        /// <param name="root">UI root with the overlay views.</param>
        public OverlayService(UIRoot root)
        {
            _root = root;
        }

        /// <inheritdoc/>
        public void Toast(string message, bool isWarning = false) => _root.Toast.Show(message, isWarning);

        /// <inheritdoc/>
        public void ToastAbove(string message, VisualElement anchor)
        {
            if (anchor != null)
            {
                _root.Toast.ShowAbove(message, anchor);
            }
        }

        /// <inheritdoc/>
        public void Coach(string message, VisualElement anchor, bool isAbove, float seconds)
        {
            if (anchor != null)
            {
                _root.Coach.ShowBubble(message, anchor, isAbove, seconds);
            }
        }

        /// <inheritdoc/>
        public void PointAt(VisualElement target)
        {
            if (target != null)
            {
                _root.Coach.ShowHand(target);
            }
        }

        /// <inheritdoc/>
        public void ClearCoach() => _root.Coach.Clear();

        /// <inheritdoc/>
        public Vector2 PanelPoint(Vector2 fraction) => _root.Effects.PanelPoint(fraction);

        /// <inheritdoc/>
        public void Burst(Vector2 panelPosition, EffectKind kind, int count, Color[] colors = null) =>
            _root.Effects.Burst(panelPosition, kind, count, colors);

        /// <inheritdoc/>
        public void Praise(Vector2 panelPosition, string text, bool isCombo) =>
            _root.Effects.Praise(panelPosition, text, isCombo);

        /// <inheritdoc/>
        public void SetBackground(bool isTricky, bool hasBunting)
        {
            _root.Background.SetTricky(isTricky);
            _root.Background.SetBunting(hasBunting);
        }
    }
}
