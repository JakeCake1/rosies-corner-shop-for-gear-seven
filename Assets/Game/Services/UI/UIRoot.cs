using UnityEngine;
using UnityEngine.UI;

namespace Game.Services.UI
{
    /// <summary>
    /// Scene object that only holds the layer containers and the shared overlay widgets; created once from a
    /// prefab by the project scope and kept across scenes. All window logic lives in <see cref="UIService"/>.
    /// </summary>
    public sealed class UIRoot : MonoBehaviour
    {
        [SerializeField] private RectTransform _hud;
        [SerializeField] private RectTransform _screens;
        [SerializeField] private RectTransform _popups;
        [SerializeField] private RectTransform _overlay;
        [Tooltip("Dimmed full-screen blocker kept right behind the top popup.")]
        [SerializeField] private Button _blocker;
        [SerializeField] private ToastView _toast;
        [SerializeField] private CoachView _coach;
        [SerializeField] private EffectsView _effects;
        [SerializeField] private Canvas _mainCanvas;
        [Tooltip("Full-screen background (wallpaper and wainscot) drawn behind every scene.")]
        [SerializeField] private BackgroundView _background;

        /// <summary>Blocker behind the top popup; the UI service moves and toggles it.</summary>
        public Button Blocker => _blocker;

        /// <summary>Toast widget in the overlay layer.</summary>
        public ToastView Toast => _toast;

        /// <summary>Coach bubble and tutorial hand widget.</summary>
        public CoachView Coach => _coach;

        /// <summary>Particle bursts, praise words and flying sprites drawn above everything.</summary>
        public EffectsView Effects => _effects;

        /// <summary>The canvas holding the layers; used to convert screen and world positions.</summary>
        public Canvas MainCanvas => _mainCanvas;

        /// <summary>Wallpaper, wainscot and the tricky-level tint.</summary>
        public BackgroundView Background => _background;

        /// <summary>
        /// Returns the container of a layer.
        /// </summary>
        /// <param name="layer">Layer.</param>
        /// <returns>The container windows of that layer are parented to.</returns>
        public RectTransform GetLayer(UILayer layer)
        {
            switch (layer)
            {
                case UILayer.HUD: return _hud;
                case UILayer.Screens: return _screens;
                case UILayer.Popups: return _popups;
                default: return _overlay;
            }
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }
    }
}
