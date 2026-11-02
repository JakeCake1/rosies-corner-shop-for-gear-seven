using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Services.UI
{
    /// <summary>
    /// Holds the UI document and its layer containers (see <c>Root.uxml</c>). Contains no window logic: windows
    /// are opened by <see cref="UIService"/>, overlays are driven by <see cref="OverlayService"/>.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class UIRoot : MonoBehaviour
    {
        private const string BackgroundName = "background";
        private const string SafeName = "safe";
        private const string SceneName = "scene";
        private const string HudName = "hud";
        private const string ScreensName = "screens";
        private const string PopupsName = "popups";
        private const string OverlayName = "overlay";
        private const string BlockerName = "blocker";

        [SerializeField] private UIDocument _document;

        private VisualElement _safe;
        private VisualElement _scene;
        private VisualElement _hud;
        private VisualElement _screens;
        private VisualElement _popups;
        private VisualElement _overlay;
        private VisualElement _blocker;
        private BackgroundView _background;
        private ToastView _toast;
        private CoachView _coach;
        private EffectsView _effects;
        private Rect _appliedSafeArea;
        private Vector2 _appliedPanelSize;

        /// <summary>Dimmed full-screen blocker kept right behind the top popup.</summary>
        public VisualElement Blocker
        {
            get
            {
                EnsureReady();
                return _blocker;
            }
        }

        /// <summary>Wallpaper, wainscot, bunting and tricky tint.</summary>
        public BackgroundView Background
        {
            get
            {
                EnsureReady();
                return _background;
            }
        }

        /// <summary>Toast notifications.</summary>
        public ToastView Toast
        {
            get
            {
                EnsureReady();
                return _toast;
            }
        }

        /// <summary>Coach bubbles and the tutorial hand.</summary>
        public CoachView Coach
        {
            get
            {
                EnsureReady();
                return _coach;
            }
        }

        /// <summary>Particles and praise words.</summary>
        public EffectsView Effects
        {
            get
            {
                EnsureReady();
                return _effects;
            }
        }

        /// <summary>
        /// Returns the container of a layer.
        /// </summary>
        /// <param name="layer">Layer.</param>
        /// <returns>The layer element.</returns>
        public VisualElement GetLayer(UILayer layer)
        {
            EnsureReady();
            switch (layer)
            {
                case UILayer.Scene: return _scene;
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

        private void Update()
        {
            if (_safe == null)
            {
                return;
            }

            var panelSize = _document.rootVisualElement.layout.size;
            if (Screen.safeArea == _appliedSafeArea && panelSize == _appliedPanelSize)
            {
                return;
            }

            ApplySafeArea(panelSize);
        }

        private void EnsureReady()
        {
            if (_safe != null)
            {
                return;
            }

            var root = _document.rootVisualElement;
            root.pickingMode = PickingMode.Ignore;
            _safe = root.Q(SafeName);
            _scene = root.Q(SceneName);
            _hud = root.Q(HudName);
            _screens = root.Q(ScreensName);
            _popups = root.Q(PopupsName);
            _overlay = root.Q(OverlayName);
            _blocker = root.Q(BlockerName);
            _background = new BackgroundView();
            _background.Attach(root.Q(BackgroundName));
            _toast = new ToastView();
            _toast.Attach(_overlay);
            _coach = new CoachView();
            _coach.Attach(_overlay);
            _effects = new EffectsView();
            _effects.Attach(_overlay);
        }

        private void ApplySafeArea(Vector2 panelSize)
        {
            _appliedSafeArea = Screen.safeArea;
            _appliedPanelSize = panelSize;
            var panel = _document.rootVisualElement.panel;
            if (panel == null || Screen.width <= 0 || Screen.height <= 0)
            {
                return;
            }

            // Screen space has y up, panel space has y down.
            var area = Screen.safeArea;
            var topLeft = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(area.xMin, Screen.height - area.yMax));
            var bottomRight = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(area.xMax, Screen.height - area.yMin));
            _safe.style.left = Mathf.Max(0f, topLeft.x);
            _safe.style.top = Mathf.Max(0f, topLeft.y);
            _safe.style.right = Mathf.Max(0f, panelSize.x - bottomRight.x);
            _safe.style.bottom = Mathf.Max(0f, panelSize.y - bottomRight.y);
        }
    }
}
