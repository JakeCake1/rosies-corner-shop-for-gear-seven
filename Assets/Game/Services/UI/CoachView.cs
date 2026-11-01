using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Game.Services.UI
{
    /// <summary>
    /// Cream speech bubble anchored next to a UI element, plus the tutorial pointing hand. Used for coach tips,
    /// booster gifts and the level 1 tutorial.
    /// </summary>
    public sealed class CoachView : MonoBehaviour
    {
        private const float Gap = 10f;
        private const float EdgeMargin = 12f;

        [SerializeField] private RectTransform _bubble;
        [SerializeField] private CanvasGroup _bubbleGroup;
        [SerializeField] private TMP_Text _text;
        [SerializeField] private RectTransform _hand;

        private Tween _bubbleTween;
        private Tween _handTween;

        /// <summary>
        /// Shows the bubble next to <paramref name="anchor"/>, clamped inside the screen.
        /// </summary>
        /// <param name="message">Rich text (bold for booster names).</param>
        /// <param name="anchor">Element the bubble points at.</param>
        /// <param name="isAbove">Place above the anchor instead of below.</param>
        /// <param name="seconds">Auto-hide delay; 0 keeps it until <see cref="HideBubble"/>.</param>
        public void ShowBubble(string message, RectTransform anchor, bool isAbove, float seconds)
        {
            _bubbleTween?.Kill();
            _text.SetText(message);
            _bubble.gameObject.SetActive(true);
            Canvas.ForceUpdateCanvases();
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(_bubble);

            var parent = (RectTransform)_bubble.parent;
            var bounds = AnchorBounds(anchor, parent);
            var size = _bubble.rect.size;
            var x = Mathf.Clamp(bounds.center.x, parent.rect.xMin + size.x / 2 + EdgeMargin,
                parent.rect.xMax - size.x / 2 - EdgeMargin);
            var y = isAbove ? bounds.yMax + Gap + size.y / 2 : bounds.yMin - Gap - size.y / 2;
            _bubble.localPosition = new Vector3(x, y, 0f);

            _bubbleGroup.alpha = 0f;
            var seq = DOTween.Sequence().SetLink(gameObject).Append(_bubbleGroup.DOFade(1f, 0.2f));
            if (seconds > 0f) seq.AppendInterval(seconds).Append(_bubbleGroup.DOFade(0f, 0.4f));
            _bubbleTween = seq;
        }

        /// <summary>
        /// Hides the bubble now.
        /// </summary>
        public void HideBubble()
        {
            _bubbleTween?.Kill();
            _bubbleGroup.alpha = 0f;
            _bubble.gameObject.SetActive(false);
        }

        /// <summary>
        /// Shows the tutorial hand tapping at <paramref name="target"/>.
        /// </summary>
        /// <param name="target">Element to point at (the first item to tap on level 1).</param>
        public void ShowHand(RectTransform target)
        {
            _handTween?.Kill();
            var parent = (RectTransform)_hand.parent;
            var b = AnchorBounds(target, parent);
            _hand.gameObject.SetActive(true);
            _hand.localPosition = new Vector3(b.center.x + 8f, b.center.y - 18f, 0f);
            _hand.localScale = Vector3.one;
            _handTween = _hand.DOScale(0.88f, 0.45f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine)
                .SetLink(gameObject);
        }

        /// <summary>
        /// Hides bubble and hand (on any tap, level change or popup).
        /// </summary>
        public void Clear()
        {
            HideBubble();
            _handTween?.Kill();
            _hand.gameObject.SetActive(false);
        }

        private static Rect AnchorBounds(RectTransform anchor, RectTransform space)
        {
            var corners = new Vector3[4];
            anchor.GetWorldCorners(corners);
            var min = space.InverseTransformPoint(corners[0]);
            var max = space.InverseTransformPoint(corners[2]);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private void Awake()
        {
            Clear();
        }

        private void OnDestroy()
        {
            _bubbleTween?.Kill();
            _handTween?.Kill();
        }
    }
}
