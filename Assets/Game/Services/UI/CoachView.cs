using DG.Tweening;
using Game.Services.UI.Elements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Services.UI
{
    /// <summary>
    /// Coach bubble pointing at an element, and the pulsing tutorial hand. Both follow their target while visible
    /// (shelves drop in, items fly) and exist in the panel only while shown.
    /// </summary>
    public sealed class CoachView : ElementView
    {
        private const string BubbleClass = "coach-bubble";
        private const string TextClass = "coach-bubble__text";
        private const string HandClass = "coach-hand";
        private const float Gap = 10f;
        private const float EdgeMargin = 12f;
        private static readonly Vector2 HandSize = new Vector2(54f, 62f);
        private static readonly Vector2 HandOffset = new Vector2(8f, 18f);

        private VisualElement _bubble;
        private Label _text;
        private VisualElement _hand;
        private VisualElement _bubbleAnchor;
        private VisualElement _handTarget;
        private bool _isAbove;
        private Tween _bubbleTween;
        private Tween _handTween;
        private IVisualElementScheduledItem _follow;

        /// <summary>
        /// Shows a bubble next to <paramref name="anchor"/>.
        /// </summary>
        /// <param name="message">Rich text.</param>
        /// <param name="anchor">Element to point at.</param>
        /// <param name="isAbove">Above the anchor (otherwise below).</param>
        /// <param name="seconds">Seconds before it fades; 0 keeps it until cleared.</param>
        public void ShowBubble(string message, VisualElement anchor, bool isAbove, float seconds)
        {
            _bubbleTween?.Kill();
            if (_bubble == null)
            {
                _bubble = CreateElement(BubbleClass);
                _bubble.pickingMode = PickingMode.Ignore;
                _text = CreateLabel(string.Empty, TextClass);
                _bubble.Add(_text);
            }

            _bubbleAnchor = anchor;
            _isAbove = isAbove;
            _text.text = message;
            Root.Add(_bubble);
            _bubble.BringToFront();
            _bubble.style.opacity = 0f;
            PlaceBubble();
            var seq = DOTween.Sequence().Append(_bubble.DOFade(1f, 0.2f));
            if (seconds > 0f)
            {
                seq.AppendInterval(seconds).Append(_bubble.DOFade(0f, 0.4f)).OnComplete(HideBubble);
            }

            _bubbleTween = seq;
            StartFollowing();
        }

        /// <summary>
        /// Hides the bubble.
        /// </summary>
        public void HideBubble()
        {
            _bubbleTween?.Kill();
            _bubbleAnchor = null;
            _bubble?.RemoveFromHierarchy();
        }

        /// <summary>
        /// Shows the pulsing hand over <paramref name="target"/>.
        /// </summary>
        /// <param name="target">Element to tap.</param>
        public void ShowHand(VisualElement target)
        {
            _handTween?.Kill();
            if (_hand == null)
            {
                _hand = CreateElement(HandClass);
                _hand.pickingMode = PickingMode.Ignore;
            }

            _handTarget = target;
            Root.Add(_hand);
            _hand.BringToFront();
            PlaceHand();
            VisualElementTweens.SetScale(_hand, Vector2.one);
            _handTween = _hand.DOScale(0.88f, 0.45f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
            StartFollowing();
        }

        /// <summary>
        /// Removes the bubble and the hand.
        /// </summary>
        public void Clear()
        {
            HideBubble();
            _handTween?.Kill();
            _handTarget = null;
            _hand?.RemoveFromHierarchy();
            _follow?.Pause();
        }

        /// <inheritdoc/>
        protected override void OnAttached()
        {
        }

        private void StartFollowing()
        {
            if (_follow == null) _follow = Root.schedule.Execute(Follow).Every(0);
            else _follow.Resume();
        }

        private void Follow()
        {
            if (_bubbleAnchor == null && _handTarget == null)
            {
                _follow.Pause();
                return;
            }

            if (_bubbleAnchor != null) PlaceBubble();
            if (_handTarget != null) PlaceHand();
        }

        private void PlaceBubble()
        {
            if (_bubbleAnchor == null || _bubbleAnchor.panel == null) return;

            var bounds = Bounds(_bubbleAnchor);
            var space = Root.layout.size;
            var size = _bubble.layout.size;
            if (float.IsNaN(size.x) || size.x <= 0f) size = new Vector2(270f, 60f);
            var x = Mathf.Clamp(bounds.center.x - size.x / 2f, EdgeMargin, space.x - size.x - EdgeMargin);
            var y = _isAbove ? bounds.yMin - Gap - size.y : bounds.yMax + Gap;
            VisualElementTweens.SetPosition(_bubble, new Vector2(x, y));
        }

        private void PlaceHand()
        {
            if (_handTarget == null || _handTarget.panel == null) return;

            var center = Bounds(_handTarget).center + HandOffset;
            _hand.PlaceCentered(center, HandSize);
        }

        private Rect Bounds(VisualElement element)
        {
            var world = element.worldBound;
            var min = Root.WorldToLocal(world.min);
            var max = Root.WorldToLocal(world.max);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
    }
}
