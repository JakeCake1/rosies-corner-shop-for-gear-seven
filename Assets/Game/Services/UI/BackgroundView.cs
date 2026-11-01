using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Services.UI
{
    /// <summary>
    /// The shop interior behind every screen: wallpaper, wainscot, bunting (home only) and the red tint of tricky
    /// levels.
    /// </summary>
    public sealed class BackgroundView : MonoBehaviour
    {
        private const float TintSeconds = 0.4f;

        [SerializeField] private Image _trickyTint;
        [SerializeField] private GameObject _bunting;
        [Tooltip("Alpha of the red wash on tricky levels.")]
        [SerializeField] private float _trickyAlpha = 0.16f;

        /// <summary>
        /// Switches the red tricky-level wash on or off.
        /// </summary>
        /// <param name="isTricky">True on tricky levels.</param>
        public void SetTricky(bool isTricky)
        {
            _trickyTint.DOKill();
            _trickyTint.DOFade(isTricky ? _trickyAlpha : 0f, TintSeconds).SetLink(gameObject);
        }

        /// <summary>
        /// Shows the bunting along the top edge (home and album only).
        /// </summary>
        /// <param name="isVisible">Whether the bunting is visible.</param>
        public void SetBunting(bool isVisible) => _bunting.SetActive(isVisible);
    }
}
