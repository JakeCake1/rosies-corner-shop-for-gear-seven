using DG.Tweening;
using Game.Services.UI.Elements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Services.UI
{
    /// <summary>
    /// Short message pill under the HUD. The element exists only while a toast is on screen.
    /// </summary>
    public sealed class ToastView : ElementView
    {
        private const string ToastClass = "toast";
        private const string TextClass = "toast__text";
        private const string WarningClass = "toast--warning";
        private const float ShowSeconds = 1.8f;
        private const float FadeSeconds = 0.2f;

        private VisualElement _toast;
        private Label _text;
        private Sequence _sequence;

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

        /// <inheritdoc/>
        protected override void OnAttached()
        {
        }
    }
}
