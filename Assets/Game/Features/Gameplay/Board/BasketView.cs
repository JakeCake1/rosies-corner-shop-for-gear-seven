using DG.Tweening;
using Game.Services.Pooling;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Features.Gameplay.Board
{
    /// <summary>
    /// A customer's wicker basket with three item slots between its back and front, the order tag below and the
    /// "done" napkin. Pooled by the board.
    /// </summary>
    public sealed class BasketView : MonoBehaviour, IPoolable
    {
        [SerializeField] private RectTransform _rect;
        [SerializeField] private RectTransform[] _slots = new RectTransform[3];
        [SerializeField] private Image _tagIcon;
        [SerializeField] private TMP_Text _tagCount;
        [SerializeField] private RectTransform _napkin;
        [SerializeField] private CanvasGroup _group;

        /// <summary>The basket rect; the board centers it on its slot.</summary>
        public RectTransform Rect => _rect;

        /// <summary>Basket id; 0 while pooled.</summary>
        public int BasketId { get; private set; }

        /// <summary>When the arrival animation ends (unscaled time); items wait for it.</summary>
        public float SettledAt { get; set; }

        /// <summary>
        /// Prepares the basket for an order.
        /// </summary>
        /// <param name="basketId">Basket id.</param>
        /// <param name="itemSprite">Wanted item, shown on the tag.</param>
        public void Setup(int basketId, Sprite itemSprite)
        {
            BasketId = basketId;
            _tagIcon.sprite = itemSprite;
            _napkin.gameObject.SetActive(false);
            _group.alpha = 1f;
            _rect.localScale = Vector3.one;
        }

        /// <summary>
        /// Returns the cell for item <paramref name="index"/> (0..2).
        /// </summary>
        /// <param name="index">Item index in the basket.</param>
        /// <returns>The slot rect.</returns>
        public RectTransform Slot(int index) => _slots[index];

        /// <summary>
        /// Updates the "n/3" tag.
        /// </summary>
        /// <param name="count">Items packed.</param>
        public void SetCount(int count) => _tagCount.SetText("{0}/3", count);

        /// <summary>
        /// Rolls in from the side when a new customer arrives.
        /// </summary>
        /// <param name="fromLeft">Enter from the left edge.</param>
        /// <returns>The animation.</returns>
        public Tween Enter(bool fromLeft)
        {
            var target = _rect.anchoredPosition;
            _rect.anchoredPosition = target + new Vector2(fromLeft ? -60f : 60f, 0f);
            _group.alpha = 0f;
            return DOTween.Sequence().SetLink(gameObject)
                .Append(_rect.DOAnchorPos(target, 0.5f).SetEase(Ease.OutBack))
                .Join(_group.DOFade(1f, 0.25f));
        }

        /// <summary>
        /// Covers the basket with the napkin (shipping started).
        /// </summary>
        public void CoverWithNapkin()
        {
            _napkin.gameObject.SetActive(true);
            _napkin.localScale = new Vector3(1f, 0.2f, 1f);
            _napkin.DOScaleY(1f, 0.25f).SetEase(Ease.OutBack).SetLink(gameObject);
        }

        /// <summary>
        /// Flies the basket into the progress bar and fades it.
        /// </summary>
        /// <param name="worldTarget">Progress bar position.</param>
        /// <returns>The animation; release the basket when it completes.</returns>
        public Tween Leave(Vector3 worldTarget)
        {
            return DOTween.Sequence().SetLink(gameObject)
                .Append(_rect.DOMove(_rect.position + new Vector3(0f, 40f * _rect.lossyScale.y, 0f), 0.18f))
                .Join(_rect.DOScale(1.1f, 0.18f))
                .Append(_rect.DOMove(worldTarget, 0.44f).SetEase(Ease.InCubic))
                .Join(_rect.DOScale(0.25f, 0.44f))
                .Join(_group.DOFade(0.2f, 0.44f));
        }

        /// <inheritdoc/>
        public void OnSpawned()
        {
        }

        /// <inheritdoc/>
        public void OnDespawned()
        {
            _rect.DOKill();
            _napkin.DOKill();
            _group.DOKill();
            BasketId = 0;
        }
    }
}
