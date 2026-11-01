using DG.Tweening;
using Game.Services.Pooling;
using Game.Services.UI.Elements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Features.Gameplay.Board
{
    /// <summary>
    /// A customer's basket (<c>Basket.uxml</c>): three item slots between the back and front of the basket, a tag
    /// with the wanted item and count, and the napkin when it ships.
    /// </summary>
    public sealed class BasketView : PooledElementView
    {
        private const string TagIconName = "tag-icon";
        private const string TagCountName = "tag-count";
        private const string NapkinName = "napkin";
        private const string SlotsName = "slots";

        private readonly VisualElement[] _slots = new VisualElement[3];
        private VisualElement _tagIcon;
        private Label _tagCount;
        private VisualElement _napkin;

        /// <summary>Basket id shown by this view (0 when pooled).</summary>
        public int BasketId { get; private set; }

        /// <summary>Time (unscaled) when the basket stops moving.</summary>
        public float SettledAt { get; set; }

        /// <summary>
        /// Prepares the basket.
        /// </summary>
        /// <param name="basketId">Basket id.</param>
        /// <param name="itemSprite">Wanted item.</param>
        public void Setup(int basketId, Sprite itemSprite)
        {
            BasketId = basketId;
            _tagIcon.SetSprite(itemSprite);
            _napkin.SetVisible(false);
        }

        /// <summary>
        /// Returns an item slot.
        /// </summary>
        /// <param name="index">0..2.</param>
        /// <returns>Slot element.</returns>
        public VisualElement Slot(int index) => _slots[index];

        /// <summary>
        /// Shows how many items are in.
        /// </summary>
        /// <param name="count">Items in the basket.</param>
        public void SetCount(int count) => _tagCount.text = count + "/3";

        /// <summary>
        /// Slides the basket in from the side.
        /// </summary>
        /// <param name="fromLeft">Enter from the left.</param>
        /// <returns>The animation.</returns>
        public Tween Enter(bool fromLeft)
        {
            VisualElementTweens.SetTranslate(Root, new Vector2(fromLeft ? -60f : 60f, 0f));
            Root.style.opacity = 0f;
            return DOTween.Sequence().SetId(this)
                .Append(Root.DOTranslate(Vector2.zero, 0.5f).SetEase(Ease.OutBack))
                .Join(Root.DOFade(1f, 0.25f));
        }

        /// <summary>
        /// Covers the full basket with the napkin.
        /// </summary>
        public void CoverWithNapkin()
        {
            _napkin.SetVisible(true);
            VisualElementTweens.SetScale(_napkin, new Vector2(1f, 0.2f));
            _napkin.DOScale(Vector2.one, 0.25f).SetEase(Ease.OutBack).SetId(this);
        }

        /// <summary>
        /// Lifts the basket and flies it into the progress bar.
        /// </summary>
        /// <param name="panelTarget">Target in panel space.</param>
        /// <returns>The animation.</returns>
        public Tween Leave(Vector2 panelTarget)
        {
            var parent = Root.hierarchy.parent;
            var delta = parent.WorldToLocal(panelTarget) - parent.WorldToLocal(Root.worldBound.center);
            var lift = new Vector2(0f, -40f);
            return DOTween.Sequence().SetId(this)
                .Append(Root.DOTranslate(lift, 0.18f))
                .Join(Root.DOScale(1.1f, 0.18f))
                .Append(Root.DOTranslate(delta, 0.44f).SetEase(Ease.InCubic))
                .Join(Root.DOScale(0.25f, 0.44f))
                .Join(Root.DOFade(0.2f, 0.44f));
        }

        /// <inheritdoc/>
        public override void OnDespawned()
        {
            base.OnDespawned();
            _napkin.Kill();
            ResetTransform(_napkin);
            BasketId = 0;
        }

        /// <inheritdoc/>
        protected override void OnAttached()
        {
            var slots = Q(SlotsName);
            for (var i = 0; i < _slots.Length; i++)
            {
                _slots[i] = slots[i];
            }

            _tagIcon = Q(TagIconName);
            _tagCount = Q<Label>(TagCountName);
            _napkin = Q(NapkinName);
        }
    }
}
