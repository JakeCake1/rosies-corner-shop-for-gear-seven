using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Features.Gameplay
{
    /// <summary>
    /// The "Level N" ribbon with the level tip that slides across the counter when a level starts.
    /// </summary>
    public sealed class LevelBannerView : MonoBehaviour
    {
        private const float HoldSeconds = 2.4f;

        [SerializeField] private RectTransform _rect;
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private Image _ribbon;
        [SerializeField] private Sprite _ribbonBerry;
        [SerializeField] private Sprite _ribbonGreen;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private GameObject _subCard;
        [SerializeField] private TMP_Text _sub;

        private Sequence _sequence;

        /// <summary>
        /// Shows the banner centered on <paramref name="anchor"/> (the counter).
        /// </summary>
        /// <param name="title">"Level 5" or "Tricky Level".</param>
        /// <param name="tip">Tip under the ribbon; empty hides the card.</param>
        /// <param name="isTricky">Berry ribbon for tricky levels, green otherwise.</param>
        /// <param name="anchor">Element to center on vertically.</param>
        public void Show(string title, string tip, bool isTricky, RectTransform anchor)
        {
            _sequence?.Kill();
            _ribbon.sprite = isTricky ? _ribbonBerry : _ribbonGreen;
            _title.SetText(title);
            _subCard.SetActive(!string.IsNullOrEmpty(tip));
            if (!string.IsNullOrEmpty(tip)) _sub.SetText(tip);
            _rect.position = new Vector3(_rect.position.x, anchor.position.y, _rect.position.z);
            _group.alpha = 0f;
            _rect.localScale = new Vector3(0.6f, 0.6f, 1f);
            _sequence = DOTween.Sequence().SetLink(gameObject)
                .Append(_group.DOFade(1f, 0.25f))
                .Join(_rect.DOScale(1f, 0.35f).SetEase(Ease.OutBack))
                .AppendInterval(HoldSeconds)
                .Append(_group.DOFade(0f, 0.35f));
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
