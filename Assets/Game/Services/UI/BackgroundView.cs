using DG.Tweening;
using Game.Services.UI.Elements;
using UnityEngine.UIElements;

namespace Game.Services.UI
{
    /// <summary>
    /// Full-screen shop background behind every layer: wallpaper, wainscot, bunting and the gingham of tricky levels.
    /// </summary>
    public sealed class BackgroundView : ElementView
    {
        private const string TintName = "tricky-tint";
        private const string BuntingName = "bunting";
        private const float TintSeconds = 0.4f;
        private const float TrickyOpacity = 1f;

        private VisualElement _tint;
        private VisualElement _bunting;

        /// <summary>
        /// Fades the tricky-level tint in or out.
        /// </summary>
        /// <param name="isTricky">Tricky level.</param>
        public void SetTricky(bool isTricky)
        {
            _tint.Kill();
            _tint.DOFade(isTricky ? TrickyOpacity : 0f, TintSeconds);
        }

        /// <summary>
        /// Shows or hides the bunting (home screen only).
        /// </summary>
        /// <param name="isVisible">Show it.</param>
        public void SetBunting(bool isVisible) => _bunting.SetVisible(isVisible);

        /// <inheritdoc/>
        protected override void OnAttached()
        {
            _tint = Q(TintName);
            _bunting = Q(BuntingName);
            _tint.style.opacity = 0f;
        }
    }
}
