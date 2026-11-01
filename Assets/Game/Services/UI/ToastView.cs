using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Services.UI
{
    /// <summary>
    /// Short message pill near the top of the screen ("The counter is full", "+50 coins"); one at a time, the
    /// newest replaces the previous.
    /// </summary>
    public sealed class ToastView : MonoBehaviour
    {
        private const float ShowSeconds = 1.8f;
        private const float FadeSeconds = 0.2f;

        [SerializeField] private CanvasGroup _group;
        [SerializeField] private Image _background;
        [SerializeField] private TMP_Text _text;
        [SerializeField] private Color _normalColor = new Color(0.23f, 0.16f, 0.11f, 0.92f);
        [SerializeField] private Color _warningColor = new Color(0.78f, 0.24f, 0.31f, 0.95f);

        private Sequence _sequence;

        /// <summary>
        /// Shows a message; called by <see cref="ToastService"/>.
        /// </summary>
        /// <param name="message">Text, rich text allowed.</param>
        /// <param name="isWarning">Berry background for problems (counter full, not enough coins).</param>
        public void Show(string message, bool isWarning)
        {
            _sequence?.Kill();
            _text.SetText(message);
            _background.color = isWarning ? _warningColor : _normalColor;
            var rt = (RectTransform)transform;
            rt.localScale = Vector3.one * 0.9f;
            _sequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject)
                .Append(_group.DOFade(1f, FadeSeconds))
                .Join(rt.DOScale(1f, FadeSeconds).SetEase(Ease.OutBack))
                .AppendInterval(ShowSeconds)
                .Append(_group.DOFade(0f, FadeSeconds));
        }

        private void Awake()
        {
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
        }

        private void OnDestroy()
        {
            _sequence?.Kill();
        }
    }
}
