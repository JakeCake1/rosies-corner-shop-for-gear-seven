using DG.Tweening;
using Game.Services.UI.Elements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Services.UI
{
    /// <summary>
    /// Short message pill under the HUD, or a white tip that pops up above a button. The elements exist only while
    /// a toast is on screen.
    /// </summary>
    public sealed class ToastView : ElementView
    {
        private const string ToastClass = "toast";
        private const string TextClass = "toast__text";
        private const string WarningClass = "toast--warning";
        private const string TipClass = "toast-tip";
        private const string TipPillClass = "toast-tip__pill";
        private const string TipTextClass = "toast-tip__text";
        private const float TipGap = 0f; // the pill sprite already has 10 px of shadow below its face
        private const float TipRise = 10f;
        private const float ShowSeconds = 1.8f;
        private const float FadeSeconds = 0.2f;

        private VisualElement _toast;
        private Label _text;
        private Sequence _sequence;
        private VisualElement _tip;
        private VisualElement _tipPill;
        private Label _tipText;
        private Sequence _tipSequence;

        /// <summary>
        /// Shows a toast, replacing the current one.
        /// </summary>
        /// <param name="message">Text.</param>
        /// <param name="isWarning">Red warning style.</param>
        public void Show(string message, bool isWarning)
        {
            _sequence?.Kill();
            if (_toast == null)
            {
                _toast = CreateElement(ToastClass);
                _toast.pickingMode = PickingMode.Ignore;
                _text = CreateLabel(string.Empty, TextClass);
                _toast.Add(_text);
            }

            Root.Add(_toast);
            _toast.BringToFront();
            _text.text = message;
            _toast.EnableInClassList(WarningClass, isWarning);
            _toast.style.opacity = 0f;
            VisualElementTweens.SetScale(_toast, Vector2.one * 0.9f);
            _sequence = DOTween.Sequence().SetUpdate(true)
                .Append(_toast.DOFade(1f, FadeSeconds))
                .Join(_toast.DOScale(1f, FadeSeconds).SetEase(Ease.OutBack))
                .AppendInterval(ShowSeconds)
                .Append(_toast.DOFade(0f, FadeSeconds))
                .OnComplete(() => _toast.RemoveFromHierarchy());
        }

        /// <summary>
        /// Pops a white tip up above an element (a locked button), replacing the current tip.
        /// </summary>
        /// <param name="message">Text.</param>
        /// <param name="anchor">Element the tip sits on; the tip's bottom right is the element's top right.</param>
        public void ShowAbove(string message, VisualElement anchor)
        {
            _tipSequence?.Kill();
            if (_tip == null)
            {
                _tip = CreateElement(TipClass);
                _tip.pickingMode = PickingMode.Ignore;
                _tipPill = CreateElement(TipPillClass);
                _tipPill.pickingMode = PickingMode.Ignore;
                _tipText = CreateLabel(string.Empty, TipTextClass);
                _tipPill.Add(_tipText);
                _tip.Add(_tipPill);
            }

            Root.Add(_tip);
            _tip.BringToFront();
            _tipText.text = message;
            var bounds = anchor.worldBound;
            // The pill's right edge lines up with the anchor's right edge, so a button at the screen side keeps
            // its tip on screen.
            var point = Root.WorldToLocal(new Vector2(bounds.xMax, bounds.yMin));
            _tip.style.left = point.x;
            _tip.style.top = point.y - TipGap;
            _tip.style.opacity = 0f;
            VisualElementTweens.SetTranslate(_tip, new Vector2(0f, TipRise));
            VisualElementTweens.SetScale(_tipPill, Vector2.one * 0.6f);
            _tipSequence = DOTween.Sequence().SetUpdate(true)
                .Append(_tip.DOFade(1f, FadeSeconds))
                .Join(_tip.DOTranslate(Vector2.zero, FadeSeconds).SetEase(Ease.OutQuad))
                .Join(_tipPill.DOScale(1f, FadeSeconds).SetEase(Ease.OutBack))
                .AppendInterval(ShowSeconds)
                .Append(_tip.DOFade(0f, FadeSeconds))
                .OnComplete(() => _tip.RemoveFromHierarchy());
        }

        /// <inheritdoc/>
        protected override void OnAttached()
        {
        }
    }
}
