using DG.Tweening;
using Game.Services.UI.Elements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Features.Gameplay
{
    /// <summary>
    /// "Level N" ribbon with an optional tip card, shown over the counter at level start
    /// (<c>Banner.uxml</c>). The elements exist only while the banner is on screen.
    /// </summary>
    public sealed class LevelBannerView : ElementView
    {
        private const string RibbonName = "ribbon";
        private const string TitleName = "title";
        private const string TipName = "tip";
        private const string TipTextName = "tip-text";
        private const string GreenClass = "kit-ribbon-green";
        private const string BerryClass = "kit-ribbon-berry";
        private const float HoldSeconds = 2.4f;
        private const float Width = 320f;
        private const float RibbonHalfHeight = 26f;

        private readonly VisualTreeAsset _layout;
        private readonly VisualElement _parent;
        private VisualElement _ribbon;
        private Label _title;
        private VisualElement _tip;
        private Label _tipText;
        private Sequence _sequence;

        /// <summary>
        /// Creates the banner view.
        /// </summary>
        /// <param name="layout">Banner layout.</param>
        /// <param name="parent">Element the banner is shown in.</param>
        public LevelBannerView(VisualTreeAsset layout, VisualElement parent)
        {
            _layout = layout;
            _parent = parent;
        }

        /// <summary>
        /// Shows the banner centred on <paramref name="anchor"/>, then fades it out.
        /// </summary>
        /// <param name="title">Ribbon text.</param>
        /// <param name="tip">Tip under the ribbon, or empty.</param>
        /// <param name="isTricky">Berry ribbon for tricky levels.</param>
        /// <param name="anchor">Element to centre on (the counter).</param>
        public void Show(string title, string tip, bool isTricky, VisualElement anchor)
        {
            _sequence?.Kill();
            if (Root == null)
            {
                Attach(_layout.Instantiate());
            }

            _parent.Add(Root);
            Root.BringToFront();
            _ribbon.EnableInClassList(BerryClass, isTricky);
            _ribbon.EnableInClassList(GreenClass, !isTricky);
            _title.text = title;
            _tip.SetVisible(!string.IsNullOrEmpty(tip));
            _tipText.text = tip ?? string.Empty;

            Root.style.position = Position.Absolute;
            Root.style.width = Width;
            // Grow from the ribbon's centre, not from the element's corner.
            Root.style.transformOrigin = new TransformOrigin(Length.Percent(50f), RibbonHalfHeight);
            Root.style.opacity = 0f;
            Root.style.visibility = Visibility.Hidden;
            VisualElementTweens.SetScale(Root, Vector2.one * 0.6f);
            // At level start the board was laid out this frame; place the banner once the layout pass has run.
            Root.schedule.Execute(() => Play(anchor));
        }

        /// <summary>
        /// Removes the banner immediately.
        /// </summary>
        public void Hide()
        {
            _sequence?.Kill();
            Root?.RemoveFromHierarchy();
        }

        /// <inheritdoc/>
        protected override void OnAttached()
        {
            Root.pickingMode = PickingMode.Ignore;
            _ribbon = Q(RibbonName);
            _title = Q<Label>(TitleName);
            _tip = Q(TipName);
            _tipText = Q<Label>(TipTextName);
        }

        private void Play(VisualElement anchor)
        {
            // The counter may not have been laid out yet (zero or NaN rect): wait for the next pass instead of
            // placing the banner at the panel's corner.
            var bounds = anchor.worldBound;
            if (float.IsNaN(bounds.width) || bounds.width <= 0f)
            {
                Root.schedule.Execute(() => Play(anchor));
                return;
            }

            var center = _parent.WorldToLocal(bounds.center);
            Root.style.left = center.x - Width / 2f;
            Root.style.top = center.y - RibbonHalfHeight;
            Root.style.visibility = Visibility.Visible;
            _sequence = DOTween.Sequence()
                .Append(Root.DOFade(1f, 0.25f))
                .Join(Root.DOScale(1f, 0.35f).SetEase(Ease.OutBack))
                .AppendInterval(HoldSeconds)
                .Append(Root.DOFade(0f, 0.35f))
                .OnComplete(Root.RemoveFromHierarchy);
        }
    }
}
