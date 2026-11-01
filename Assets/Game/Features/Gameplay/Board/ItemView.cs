using System;
using DG.Tweening;
using Game.Services.Pooling;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Features.Gameplay.Board
{
    /// <summary>
    /// One grocery item (or the hammer, or a paper bag) on a shelf, on the counter or in a basket. Pooled by the
    /// board; parented to the cell it currently sits in.
    /// </summary>
    public sealed class ItemView : MonoBehaviour, IPointerDownHandler, IPoolable
    {
        private const float HintPulseSeconds = 0.5f;

        [SerializeField] private RectTransform _rect;
        [SerializeField] private Image _image;
        [Tooltip("Soft gold glow shown behind the item for the hidden hint and the tutorial.")]
        [SerializeField] private Image _glow;

        private Action<ItemView> _tapped;
        private Tween _hintTween;

        /// <summary>The item's rect; the board moves it between cells.</summary>
        public RectTransform Rect => _rect;

        /// <summary>Jar id this view shows; 0 while pooled.</summary>
        public int JarId { get; private set; }

        /// <summary>True while it shows a paper bag instead of its item.</summary>
        public bool IsWrapped { get; private set; }

        /// <summary>The cell the item belongs to now (set before a flight ends); null while pooled.</summary>
        public RectTransform Destination { get; set; }

        /// <summary>
        /// Prepares a pooled view for a jar. Called by the board right after taking it from the pool.
        /// </summary>
        /// <param name="jarId">Jar id.</param>
        /// <param name="sprite">Item, hammer or paper bag art.</param>
        /// <param name="isWrapped">Shows a paper bag.</param>
        /// <param name="tapped">Called on pointer down; the board forwards it to the session.</param>
        public void Setup(int jarId, Sprite sprite, bool isWrapped, Action<ItemView> tapped)
        {
            JarId = jarId;
            IsWrapped = isWrapped;
            _image.sprite = sprite;
            _tapped = tapped;
            SetHint(false);
        }

        /// <summary>
        /// Opens the paper bag: swaps the art with a small "land" pop.
        /// </summary>
        /// <param name="sprite">The real item art.</param>
        public void Unwrap(Sprite sprite)
        {
            IsWrapped = false;
            _image.sprite = sprite;
            _rect.DOKill(true);
            _rect.localScale = Vector3.one * 0.8f;
            _rect.DOScale(1f, 0.25f).SetEase(Ease.OutBack).SetLink(gameObject);
        }

        /// <summary>
        /// Switches the pulsing hint glow.
        /// </summary>
        /// <param name="isOn">True to glow.</param>
        public void SetHint(bool isOn)
        {
            _hintTween?.Kill();
            _glow.gameObject.SetActive(isOn);
            if (!isOn) return;

            _glow.color = new Color(1f, 0.86f, 0.4f, 0.9f);
            _hintTween = _glow.DOFade(0.25f, HintPulseSeconds).SetLoops(-1, LoopType.Yoyo).SetLink(gameObject);
        }

        /// <summary>
        /// Shakes the item when the counter is full ("nope").
        /// </summary>
        public void Nope()
        {
            _image.rectTransform.DOKill(true);
            _image.rectTransform.DOShakeAnchorPos(0.35f, new Vector2(6f, 0f), 14, 0f).SetLink(gameObject);
        }

        /// <summary>
        /// Places the item inside a cell at rest (no flight).
        /// </summary>
        /// <param name="cell">Shelf, counter or basket cell.</param>
        public void Snap(RectTransform cell)
        {
            Destination = cell;
            _rect.DOKill();
            _rect.SetParent(cell, false);
            Fill();
        }

        /// <summary>
        /// Stretches the item over its parent cell.
        /// </summary>
        public void Fill()
        {
            _rect.anchorMin = Vector2.zero;
            _rect.anchorMax = Vector2.one;
            _rect.offsetMin = Vector2.zero;
            _rect.offsetMax = Vector2.zero;
            _rect.localScale = Vector3.one;
            _rect.localRotation = Quaternion.identity;
        }

        /// <inheritdoc/>
        public void OnPointerDown(PointerEventData eventData) => _tapped?.Invoke(this);

        /// <inheritdoc/>
        public void OnSpawned()
        {
        }

        /// <inheritdoc/>
        public void OnDespawned()
        {
            _rect.DOKill();
            _image.rectTransform.DOKill();
            _hintTween?.Kill();
            _glow.gameObject.SetActive(false);
            _tapped = null;
            Destination = null;
            JarId = 0;
            IsWrapped = false;
            _image.rectTransform.anchoredPosition = Vector2.zero;
        }
    }
}
