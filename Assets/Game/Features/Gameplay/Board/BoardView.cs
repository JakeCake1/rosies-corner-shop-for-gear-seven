using System;
using DG.Tweening;
using Game.Features.Gameplay.Model;
using Game.Features.Gameplay.Session;
using Game.Services.Audio;
using Game.Services.Content;
using Game.Services.Localization;
using Game.Services.UI;
using Game.Services.UI.Elements;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Features.Gameplay.Board
{
    /// <summary>
    /// Draws a level: baskets on the ledge, the counter and the cabinet with shelf columns. Reconciles its parts with
    /// <see cref="LevelState"/> after every change and plays the rule events (unwrapping, keys, hammer, thawing).
    /// Items, shelves, baskets and lock buttons are created when they appear and returned to pools when they leave.
    /// </summary>
    public sealed class BoardView : ElementView, IDisposable
    {
        private const string BasketAreaName = "basket-area";
        private const string CounterName = "counter";
        private const string CabinetName = "cabinet";
        private const string FlightName = "flight";

        private readonly BoardLayout _layout = new BoardLayout();
        private readonly GameplayLayouts _layouts;
        private readonly IOverlayService _overlay;
        private readonly IAudioService _audio;
        private readonly ILocalizationService _texts;
        private readonly BoardSprites _sprites;
        private readonly VisualElement _progressTarget;
        private CounterView _counter;
        private CabinetView _cabinet;
        private ItemLayerView _items;
        private BasketLedgeView _baskets;
        private BoardCells _cells;
        private BoardEventEffects _effects;
        private LevelState _state;

        /// <summary>Emits the jar id of a tapped item.</summary>
        public IObservable<int> ItemTapped => _items.Tapped;

        /// <summary>Emits when a locked basket slot's "+" is tapped.</summary>
        public IObservable<Unit> ExtraBasketClicked => _baskets.ExtraBasketClicked;

        /// <summary>The counter panel (anchor for coach tips and the banner).</summary>
        public VisualElement Counter => _counter.Root;

        /// <summary>The basket ledge (anchor for the "ship" tip).</summary>
        public VisualElement BasketArea => _baskets.Root;

        /// <summary>The stock tabs row (anchor for the "stock" tip).</summary>
        public VisualElement StockLabels => _cabinet.StockTabs;

        /// <summary>
        /// Creates the board view.
        /// </summary>
        /// <param name="layouts">Shelf and basket templates.</param>
        /// <param name="overlay">Bursts and praise.</param>
        /// <param name="content">Item catalog for pictures.</param>
        /// <param name="art">Hammer and paper-bag pictures.</param>
        /// <param name="audio">
        /// Sounds tied to animation moments: items landing, shelves folding and dropping in, the key and the hammer.
        /// Sounds of the move itself (tap, ship, boosters) are played by the level session.
        /// </param>
        /// <param name="progressTarget">HUD progress bar that shipped baskets fly into.</param>
        /// <param name="texts">Stock tabs, shelf rows badges and praise words.</param>
        public BoardView(GameplayLayouts layouts, IOverlayService overlay, IContentService content, GameplayArt art,
            IAudioService audio, VisualElement progressTarget, ILocalizationService texts)
        {
            _layouts = layouts;
            _overlay = overlay;
            _audio = audio;
            _texts = texts;
            _sprites = new BoardSprites(content.Catalog, art);
            _progressTarget = progressTarget;
        }

        /// <summary>
        /// Clears the board and draws a new level; the opening shelves drop in column by column.
        /// </summary>
        /// <param name="state">The new level state.</param>
        /// <param name="isAnimated">Drop shelves in (level start) or snap (undo rebuild, resize).</param>
        public void Rebuild(LevelState state, bool isAnimated)
        {
            ClearAll();
            Layout(state);
            Render(state, new BoardChange(isAnimated ? BoardChangeKind.LevelStart : BoardChangeKind.Rebuild));
        }

        /// <summary>
        /// Reconciles all parts with <paramref name="state"/> and animates what changed.
        /// </summary>
        /// <param name="state">Current level state.</param>
        /// <param name="change">What caused the change, with rule events.</param>
        public void Render(LevelState state, BoardChange change)
        {
            _state = state;
            var now = Time.unscaledTime;
            var isInstant = change.Kind == BoardChangeKind.Rebuild;
            if (change.Kind == BoardChangeKind.Layout)
            {
                Layout(state);
            }

            _effects.Play(state, change);
            _baskets.Render(state, _layout, now, isInstant);
            _counter.Render(state);
            _cabinet.Render(state, change, _layout, now, isInstant);
            _items.Render(state, change, now, isInstant, _cells);
        }

        /// <summary>
        /// Shakes the counter and the refused item (counter full).
        /// </summary>
        /// <param name="jarId">The refused item.</param>
        public void ShakeCounter(int jarId)
        {
            _counter.Shake();
            if (_items.TryGet(jarId, out var item))
            {
                item.Nope();
            }
        }

        /// <summary>
        /// Plays the start of a shipment: napkin, sparks and praise over the basket.
        /// </summary>
        /// <param name="notice">The shipment.</param>
        public void PlayShipping(ShipNotice notice) => _baskets.PlayShipping(notice);

        /// <summary>
        /// Highlights (or clears) the hidden-hint item.
        /// </summary>
        /// <param name="jarId">Item to glow; 0 clears every hint.</param>
        public void SetHint(int jarId) => _items.SetHint(jarId);

        /// <summary>
        /// Returns the element of an item on the board, for coach anchors and the tutorial hand.
        /// </summary>
        /// <param name="jarId">Item id.</param>
        /// <returns>The item element, or null.</returns>
        public VisualElement FindItem(int jarId) => _items.TryGet(jarId, out var view) ? view.Root : null;

        /// <summary>
        /// Returns the "+" button of the first locked basket slot, for the extra-basket tip.
        /// </summary>
        /// <returns>The button, or null when all baskets are open.</returns>
        public VisualElement FirstBasketLock() => _baskets.FirstLock();

        /// <inheritdoc/>
        public void Dispose()
        {
            ClearAll();
            _items.Dispose();
            _cabinet.Dispose();
            _baskets.Dispose();
        }

        /// <inheritdoc/>
        protected override void OnAttached()
        {
            _counter = new CounterView();
            _counter.Attach(Q(CounterName));
            _cabinet = new CabinetView(_layouts.Shelf, _sprites, _overlay, _audio, _texts, this);
            _cabinet.Attach(Q(CabinetName));
            _items = new ItemLayerView(_sprites, _audio, _overlay, this);
            _items.Attach(Q(FlightName));
            _baskets = new BasketLedgeView(_layouts.Basket, _sprites, _items, _overlay, _texts, _progressTarget);
            _baskets.Attach(Q(BasketAreaName));
            _cells = new BoardCells(_cabinet, _counter, _baskets, _layout);
            _effects = new BoardEventEffects(_cabinet, _items, _sprites, _overlay, _audio);
            Root.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        private static void Place(VisualElement element, Rect rect)
        {
            element.style.left = rect.x;
            element.style.top = rect.y;
            element.style.width = rect.width;
            element.style.height = rect.height;
        }

        private void OnGeometryChanged(GeometryChangedEvent e)
        {
            // Screen rotated or the window was resized: lay the level out again for the new size.
            if (_state != null && Root.layout.size != _layout.Size)
            {
                Rebuild(_state, false);
            }
        }

        private void ClearAll()
        {
            DOTween.Kill(this);
            _items.Clear();
            _cabinet.Clear();
            _baskets.Clear();
        }

        private void Layout(LevelState state)
        {
            _layout.Compute(state, Root.layout.size);
            Place(_baskets.Root, _layout.BasketArea);
            Place(_counter.Root, _layout.Counter);
            _counter.Layout(state.Capacity, _layout.CounterSlot, _layout.Inner);
            Place(_cabinet.Root, _layout.Cabinet);
            _cabinet.Layout(_layout);
        }
    }
}
