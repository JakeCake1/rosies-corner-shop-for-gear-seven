using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Game.Features.Gameplay.Model;
using Game.Features.Gameplay.Session;
using Game.Services.Audio;
using Game.Services.Content;
using Game.Services.Localization;
using Game.Services.Pooling;
using Game.Services.UI;
using Game.Services.UI.Elements;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Features.Gameplay.Board
{
    /// <summary>
    /// Draws a level: baskets on the ledge, the counter and the cabinet with shelf columns. Reconciles the views
    /// with <see cref="LevelState"/> after every change and animates what moved. Items, shelves, baskets and
    /// lock buttons are created when they appear and returned to pools when they leave.
    /// </summary>
    public sealed class BoardView : ElementView, IDisposable
    {
        private const string BasketAreaName = "basket-area";
        private const string BasketLayerName = "baskets";
        private const string CounterName = "counter";
        private const string CounterSlotsName = "counter-slots";
        private const string CabinetName = "cabinet";
        private const string ColumnsName = "columns";
        private const string TabsName = "stock-tabs";
        private const string FlightName = "flight";
        private const string ColumnClass = "cabinet__column";
        private const string TabTextClass = "stock-tab__text";
        private const string TabHammerClass = "stock-tab__hammer";
        private const string HotDoilyClass = "counter-slot__doily--hot";

        private const float ItemAspect = 0.625f;
        private const float MaxContentWidth = 430f;
        private const float SidePadding = 16f;
        private const float TopPadding = 8f;
        private const float HudHeight = 48f;
        private const float BasketAreaHeight = 104f;
        private const float BasketWidth = 108f;
        private const float BasketHeight = 92f;
        private const float CounterTopGap = 6f;
        private const float CounterPadding = 30f;
        private const float CounterSlotGap = 8f;
        private const float CounterSlotBottom = 24f;
        private const float CabinetMinGap = 38f;
        private const float CabinetOverhead = 72f;
        private const float ColumnInset = 9f;
        private const float ColumnGap = 2f;
        private const float BoosterBarHeight = 100f;
        private const float MinRowHeight = 40f;
        private const float ShelfDropSeconds = 0.52f;
        private const float FlightSeconds = 0.43f;
        private const float CounterHopSeconds = 0.22f;
        private static readonly Color[] IceColors = { Color.white, new Color(0.75f, 0.89f, 1f), new Color(0.5f, 0.75f, 0.94f) };

        private readonly Dictionary<int, ItemView> _items = new Dictionary<int, ItemView>();
        private readonly Dictionary<int, ShelfView> _shelves = new Dictionary<int, ShelfView>();
        private readonly Dictionary<int, BasketView> _baskets = new Dictionary<int, BasketView>();
        private readonly HashSet<ItemView> _leavingItems = new HashSet<ItemView>();
        private readonly HashSet<ShelfView> _leavingShelves = new HashSet<ShelfView>();
        private readonly HashSet<BasketView> _leavingBaskets = new HashSet<BasketView>();
        private readonly VisualElement[] _locks = new VisualElement[LevelState.MaxBaskets];
        private readonly List<VisualElement> _counterSlots = new List<VisualElement>();
        private readonly List<VisualElement> _doilies = new List<VisualElement>();
        private readonly VisualElement[] _columns = new VisualElement[LevelState.ColumnCount];
        private readonly Label[] _stockLabels = new Label[LevelState.ColumnCount];
        private readonly VisualElement[] _stockHammers = new VisualElement[LevelState.ColumnCount];
        private readonly Subject<int> _itemTapped = new Subject<int>();
        private readonly Subject<Unit> _extraBasketClicked = new Subject<Unit>();
        private readonly GameplayLayouts _layouts;
        private readonly IOverlayService _overlay;
        private readonly IAudioService _audio;
        private readonly ILocalizationService _texts;
        private readonly ItemCatalog _catalog;
        private readonly GameplayArt _art;
        private readonly VisualElement _progressTarget;
        private VisualElement _basketArea;
        private VisualElement _basketLayer;
        private VisualElement _counter;
        private VisualElement _counterSlotLayer;
        private VisualElement _cabinet;
        private VisualElement _columnsRoot;
        private VisualElement _tabs;
        private VisualElement _flight;
        private ElementPool<ItemView> _itemPool;
        private ElementPool<ShelfView> _shelfPool;
        private ElementPool<BasketView> _basketPool;
        private Vector2 _itemSize;
        private float _rowHeight;
        private float _columnWidth;
        private int _visibleRows;
        private int _jarsPerRow;
        private LevelState _state;
        private Vector2 _laidOutSize;

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
            _texts = texts;
            _layouts = layouts;
            _overlay = overlay;
            _catalog = content.Catalog;
            _art = art;
            _audio = audio;
            _progressTarget = progressTarget;
        }

        /// <summary>Emits the jar id of a tapped item.</summary>
        public IObservable<int> ItemTapped => _itemTapped;

        /// <summary>Emits when a locked basket slot's "+" is tapped.</summary>
        public IObservable<Unit> ExtraBasketClicked => _extraBasketClicked;

        /// <summary>The counter panel (anchor for coach tips and the banner).</summary>
        public VisualElement Counter => _counter;

        /// <summary>The basket ledge (anchor for the "ship" tip).</summary>
        public VisualElement BasketArea => _basketArea;

        /// <summary>The stock tabs row (anchor for the "stock" tip).</summary>
        public VisualElement StockLabels => _tabs;

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
        /// Reconciles all views with <paramref name="state"/> and animates what changed.
        /// </summary>
        /// <param name="state">Current level state.</param>
        /// <param name="change">What caused the change, with rule events.</param>
        public void Render(LevelState state, BoardChange change)
        {
            _state = state;
            var now = Time.unscaledTime;
            var instant = change.Kind == BoardChangeKind.Rebuild;
            if (change.Kind == BoardChangeKind.Layout)
            {
                Layout(state);
            }

            PlayEvents(state, change);
            RenderBaskets(state, now, instant);
            RenderCounter(state);
            RenderColumns(state, change, now, instant);
            RenderItems(state, change, now, instant);
        }

        /// <summary>
        /// Shakes the counter and the refused item (counter full).
        /// </summary>
        /// <param name="jarId">The refused item.</param>
        public void ShakeCounter(int jarId)
        {
            _counter.Kill(true);
            _counter.DOShake(0.4f, new Vector2(10f, 0f), 18);
            if (_items.TryGetValue(jarId, out var item))
            {
                item.Nope();
            }
        }

        /// <summary>
        /// Plays the start of a shipment: napkin, sparks and praise over the basket.
        /// </summary>
        /// <param name="notice">The shipment.</param>
        public void PlayShipping(ShipNotice notice)
        {
            if (!_baskets.TryGetValue(notice.BasketId, out var basket))
            {
                return;
            }

            basket.CoverWithNapkin();
            var at = basket.Root.worldBound.center;
            _overlay.Burst(at, EffectKind.Spark, 20 + notice.Combo * 6);
            _overlay.Praise(at + Vector2.down * 30f, _texts.Get(notice.WordKey), notice.Combo > 0);
        }

        /// <summary>
        /// Highlights (or clears) the hidden-hint item.
        /// </summary>
        /// <param name="jarId">Item to glow; 0 clears every hint.</param>
        public void SetHint(int jarId)
        {
            foreach (var item in _items.Values)
            {
                item.SetHint(item.JarId == jarId && jarId != 0);
            }
        }

        /// <summary>
        /// Returns the element of an item on the board, for coach anchors and the tutorial hand.
        /// </summary>
        /// <param name="jarId">Item id.</param>
        /// <returns>The item element, or null.</returns>
        public VisualElement FindItem(int jarId) => _items.TryGetValue(jarId, out var v) ? v.Root : null;

        /// <summary>
        /// Returns the "+" button of the first locked basket slot, for the level 3 tip.
        /// </summary>
        /// <returns>The button, or null when all baskets are open.</returns>
        public VisualElement FirstBasketLock() => _locks.FirstOrDefault(l => l != null && l.parent != null);

        /// <inheritdoc/>
        public void Dispose()
        {
            ClearAll();
            _itemPool?.Dispose();
            _shelfPool?.Dispose();
            _basketPool?.Dispose();
            _itemTapped.Dispose();
            _extraBasketClicked.Dispose();
        }

        /// <inheritdoc/>
        protected override void OnAttached()
        {
            _basketArea = Q(BasketAreaName);
            _basketLayer = Q(BasketLayerName);
            _counter = Q(CounterName);
            _counterSlotLayer = Q(CounterSlotsName);
            _cabinet = Q(CabinetName);
            _columnsRoot = Q(ColumnsName);
            _tabs = Q(TabsName);
            _flight = Q(FlightName);
            var columns = _columnsRoot.Query(className: ColumnClass).ToList();
            var texts = _tabs.Query<Label>(className: TabTextClass).ToList();
            var hammers = _tabs.Query(className: TabHammerClass).ToList();
            for (var c = 0; c < LevelState.ColumnCount; c++)
            {
                _columns[c] = columns[c];
                _stockLabels[c] = texts[c];
                _stockHammers[c] = hammers[c];
            }

            _itemPool = new ElementPool<ItemView>(ItemView.Create, 80, 200);
            _shelfPool = new ElementPool<ShelfView>(() => CreateFromLayout<ShelfView>(_layouts.Shelf), 24, 60);
            _basketPool = new ElementPool<BasketView>(() => CreateFromLayout<BasketView>(_layouts.Basket), 4, 8);
            Root.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        private static T CreateFromLayout<T>(VisualTreeAsset layout) where T : ElementView, new()
        {
            var view = new T();
            var element = layout.Instantiate();
            element.pickingMode = PickingMode.Ignore;
            view.Attach(element);
            return view;
        }

        private void OnGeometryChanged(GeometryChangedEvent e)
        {
            // Screen rotated or the window was resized: lay the level out again for the new size.
            if (_state != null && Root.layout.size != _laidOutSize)
            {
                Rebuild(_state, false);
            }
        }

        private void ClearAll()
        {
            DOTween.Kill(this);
            foreach (var v in _items.Values.Concat(_leavingItems))
            {
                _itemPool.Release(v);
            }

            foreach (var v in _shelves.Values.Concat(_leavingShelves))
            {
                _shelfPool.Release(v);
            }

            foreach (var v in _baskets.Values.Concat(_leavingBaskets))
            {
                _basketPool.Release(v);
            }

            _items.Clear();
            _shelves.Clear();
            _baskets.Clear();
            _leavingItems.Clear();
            _leavingShelves.Clear();
            _leavingBaskets.Clear();
            _flight.Clear();
        }

        private void Layout(LevelState state)
        {
            var size = Root.layout.size;
            _laidOutSize = size;
            var inner = Mathf.Min(MaxContentWidth, size.x) - SidePadding * 2f;
            var left = (size.x - inner) / 2f;
            _visibleRows = state.VisibleRows;
            _jarsPerRow = state.JarsPerRow;

            var slot = Mathf.Floor(Mathf.Min(58f, (inner - 60f) / state.Capacity));
            var counterHeight = slot * 1.3f + CounterPadding;
            var y = TopPadding + HudHeight;
            Place(_basketArea, left, y, inner, BasketAreaHeight);
            y += BasketAreaHeight + CounterTopGap;
            Place(_counter, left, y, inner, counterHeight);
            LayoutCounter(state.Capacity, slot, inner);
            y += counterHeight;

            var columnWidth = (inner - 24f) / 3f;
            var jwMax = (columnWidth - 10f) / state.JarsPerRow;
            var available = size.y - y - BoosterBarHeight - CabinetMinGap - 8f - CabinetOverhead;
            _rowHeight = Mathf.Max(MinRowHeight, Mathf.Min(jwMax / ItemAspect + 10f, available / state.VisibleRows));
            var itemHeight = _rowHeight - 10f;
            _itemSize = new Vector2(Mathf.Min(itemHeight * ItemAspect, jwMax), itemHeight);

            var cabinetHeight = CabinetOverhead + state.VisibleRows * _rowHeight;
            var spare = Mathf.Max(0f, available - state.VisibleRows * _rowHeight);
            Place(_cabinet, left, y + CabinetMinGap + spare / 2f, inner, cabinetHeight);
            _columnsRoot.style.height = state.VisibleRows * _rowHeight;
            _columnWidth = (inner - ColumnInset * 2f) / LevelState.ColumnCount - ColumnGap * 2f;
        }

        private void LayoutCounter(int capacity, float slot, float inner)
        {
            while (_counterSlots.Count < capacity)
            {
                var cell = CreateElement("counter-slot");
                cell.pickingMode = PickingMode.Ignore;
                var doily = CreateElement("counter-slot__doily");
                doily.pickingMode = PickingMode.Ignore;
                cell.Add(doily);
                _counterSlots.Add(cell);
                _doilies.Add(doily);
            }

            var count = Mathf.Max(capacity, 5);
            var total = count * slot + (count - 1) * CounterSlotGap;
            for (var i = 0; i < _counterSlots.Count; i++)
            {
                var cell = _counterSlots[i];
                if (i >= capacity)
                {
                    cell.RemoveFromHierarchy();
                    continue;
                }

                _counterSlotLayer.Add(cell);
                cell.style.width = slot;
                cell.style.height = slot / ItemAspect * 0.62f;
                cell.style.left = (inner - total) / 2f + i * (slot + CounterSlotGap);
                cell.style.bottom = CounterSlotBottom;
            }
        }

        private static void Place(VisualElement element, float left, float top, float width, float height)
        {
            element.style.left = left;
            element.style.top = top;
            element.style.width = width;
            element.style.height = height;
        }

        private float SlotLeft(int slot) => _basketArea.style.width.value.value * (slot * 2 + 1) / 6f - BasketWidth / 2f;

        private void PlayEvents(LevelState state, BoardChange change)
        {
            foreach (var e in change.Events)
            {
                switch (e.Type)
                {
                    case LevelEventType.JarUnwrapped:
                        if (_items.TryGetValue(e.Id, out var item))
                        {
                            var jar = FindJar(state, e.Id);
                            if (jar != null)
                            {
                                item.Unwrap(SpriteOf(jar.Type));
                            }
                        }

                        break;
                    case LevelEventType.ShelfUnlocked:
                        FlyKey(e.Other, e.Id);
                        break;
                    case LevelEventType.ShelfUnboarded:
                        if (_shelves.TryGetValue(e.Id, out var nailed))
                        {
                            _overlay.Burst(nailed.Root.worldBound.center, EffectKind.Dust, 18);
                            nailed.KnockBoardsOff();
                            _audio.Play(SoundId.Snip);
                            _audio.Play(SoundId.Fold);
                        }

                        break;
                    case LevelEventType.ShelfThawed:
                        if (_shelves.TryGetValue(e.Id, out var thawed))
                        {
                            _overlay.Burst(thawed.Root.worldBound.center, EffectKind.Spark, 22, IceColors);
                        }

                        break;
                }
            }
        }

        private void FlyKey(int fromBlock, int toBlock)
        {
            if (!_shelves.TryGetValue(toBlock, out var target))
            {
                return;
            }

            var start = _shelves.TryGetValue(fromBlock, out var source) ? source.KeyCenter() : target.Root.worldBound.center;
            var key = CreateElement("key-flyer");
            key.pickingMode = PickingMode.Ignore;
            _flight.Add(key);
            var size = new Vector2(20f, 26f);
            var from = _flight.WorldToLocal(start);
            key.PlaceCentered(from, size);
            DOTween.To(() => 0f, t =>
                {
                    var to = _flight.WorldToLocal(target.Root.worldBound.center);
                    var p = Vector2.Lerp(from, to, t) + Vector2.down * (80f * 4f * t * (1f - t));
                    key.PlaceCentered(p, size);
                }, 1f, 0.55f)
                .SetId(this)
                .OnComplete(() =>
                {
                    key.RemoveFromHierarchy();
                    _audio.Play(SoundId.Snip);
                    _overlay.Burst(target.Root.worldBound.center, EffectKind.Spark, 16);
                });
        }

        private void RenderBaskets(LevelState state, float now, bool instant)
        {
            var live = new HashSet<int>();
            for (var i = 0; i < LevelState.MaxBaskets; i++)
            {
                var b = state.Baskets[i];
                var isLockedSlot = i >= state.UnlockedBaskets;
                SetLock(i, isLockedSlot && (state.LevelNumber >= GameplayBalance.ExtraBasketSlotLevel || state.LevelNumber == 0));
                if (b == null)
                {
                    continue;
                }

                live.Add(b.Id);
                if (!_baskets.TryGetValue(b.Id, out var view))
                {
                    view = _basketPool.Get();
                    _basketLayer.Add(view.Root);
                    view.Setup(b.Id, SpriteOf(b.Type));
                    _baskets[b.Id] = view;
                    view.SettledAt = now;
                    if (!instant)
                    {
                        view.Enter(i == 0);
                        view.SettledAt = now + 0.52f;
                    }
                }

                view.Place(new Vector2(SlotLeft(i), 0f), new Vector2(BasketWidth, BasketHeight));
                view.SetCount(b.Jars.Count);
            }

            foreach (var id in _baskets.Keys.Where(id => !live.Contains(id)).ToList())
            {
                var view = _baskets[id];
                _baskets.Remove(id);
                var inside = ForgetItemsInside(view.Root);
                if (instant)
                {
                    ReleaseItems(inside);
                    _basketPool.Release(view);
                    continue;
                }

                _leavingBaskets.Add(view);
                view.Leave(_progressTarget.worldBound.center).OnComplete(() =>
                {
                    _overlay.Burst(_progressTarget.worldBound.center, EffectKind.Spark, 10);
                    _progressTarget.Kill(true);
                    _progressTarget.DOPunchScale(0.08f, 0.3f, 6);
                    ReleaseItems(inside);
                    _leavingBaskets.Remove(view);
                    _basketPool.Release(view);
                });
            }
        }

        private void SetLock(int slot, bool isVisible)
        {
            if (!isVisible)
            {
                _locks[slot]?.RemoveFromHierarchy();
                return;
            }

            if (_locks[slot] == null)
            {
                _locks[slot] = CreateLock();
            }
            var element = _locks[slot];
            _basketArea.Add(element);
            element.style.left = SlotLeft(slot);
        }

        private VisualElement CreateLock()
        {
            var button = new Button();
            button.AddToClassList("btn");
            button.AddToClassList("basket-lock");
            var ghost = CreateElement("basket-lock__ghost");
            ghost.pickingMode = PickingMode.Ignore;
            var plus = CreateElement("basket-lock__plus", "kit-circle");
            plus.pickingMode = PickingMode.Ignore;
            plus.Add(CreateLabel("+", "basket-lock__plus-text"));
            button.Add(ghost);
            button.Add(plus);
            button.OnTapAsObservable().Subscribe(_ => _extraBasketClicked.OnNext(Unit.Default));
            return button;
        }

        private List<ItemView> ForgetItemsInside(VisualElement container)
        {
            var inside = _items.Values.Where(i => container.Contains(i.Root)).ToList();
            foreach (var item in inside)
            {
                _items.Remove(item.JarId);
                _leavingItems.Add(item);
            }

            return inside;
        }

        private void ReleaseItems(List<ItemView> items)
        {
            foreach (var item in items)
            {
                if (_leavingItems.Remove(item))
                {
                    _itemPool.Release(item);
                }
            }
        }

        private void RenderCounter(LevelState state)
        {
            var isHot = state.Counter.Count >= state.Capacity - 1;
            for (var i = 0; i < _doilies.Count; i++)
            {
                _doilies[i].EnableInClassList(HotDoilyClass, isHot && i < state.Counter.Count);
            }
        }

        private void RenderColumns(LevelState state, BoardChange change, float now, bool instant)
        {
            var live = new HashSet<int>();
            var isStart = change.Kind == BoardChangeKind.LevelStart;
            var dropBase = isStart ? 0.15f : 0.42f;
            for (var c = 0; c < LevelState.ColumnCount; c++)
            {
                var col = state.Columns[c];
                var stock = col.Queue.Count;
                var hasHammer = col.Queue.Any(j => j.IsTool);
                var tiedHammer = col.Blocks.Any(b => b.IsLocked && b.Slots.Any(j => j != null && j.IsTool));
                if (hasHammer)
                {
                    stock--;
                }
                _stockLabels[c].text = stock > 0
                    ? _texts.Format(GameplayTexts.Stock, stock)
                    : _texts.Get(GameplayTexts.StockEmpty);
                _stockHammers[c].SetVisible(hasHammer || tiedHammer);
                var offset = 0;
                var fresh = 0;
                foreach (var b in col.Blocks)
                {
                    live.Add(b.Id);
                    var bottom = offset * _rowHeight;
                    if (!_shelves.TryGetValue(b.Id, out var view))
                    {
                        view = _shelfPool.Get();
                        _columns[c].Add(view.Root);
                        view.Root.AddToClassList("shelf-slot");
                        view.Root.style.height = b.Height * _rowHeight - 3f;
                        var rowsText = b.Height > 1 ? _texts.Format(GameplayTexts.ShelfRows, b.Height) : null;
                        view.Setup(b.Id, b.Height, _jarsPerRow, _columnWidth, _rowHeight, _itemSize, rowsText);
                        _shelves[b.Id] = view;
                        if (instant)
                        {
                            view.Root.style.bottom = bottom;
                            view.SettledAt = now;
                        }
                        else
                        {
                            var delay = dropBase + (isStart ? c * 0.12f : 0f) + fresh * 0.1f;
                            view.Root.style.bottom = _visibleRows * _rowHeight + 20f + fresh * _rowHeight * 0.5f;
                            var drop = view.Root.DOBottom(bottom, ShelfDropSeconds).SetDelay(delay).SetEase(Ease.OutBack, 1.3f).SetId(this);
                            // The level session plays one roll for the whole opening drop; later shelves each get their own.
                            if (!isStart)
                            {
                                drop.OnStart(() => _audio.Play(SoundId.Roll));
                            }
                            view.SettledAt = now + delay + ShelfDropSeconds;
                            fresh++;
                        }
                    }
                    else if (!Mathf.Approximately(view.Root.style.bottom.value.value, bottom))
                    {
                        view.Root.Kill();
                        if (instant)
                        {
                            view.Root.style.bottom = bottom;
                        }
                        else
                        {
                            view.Root.DOBottom(bottom, 0.4f).SetDelay(0.3f).SetEase(Ease.OutBack, 1.1f).SetId(this);
                            view.SettledAt = now + 0.75f;
                        }
                    }

                    view.SetState(b.IsLocked, b.KeyTargetId != 0, b.Frozen, b.IsBoarded);
                    for (var i = 0; i < b.Slots.Length; i++)
                    {
                        var back = b.Slots[i]?.Back;
                        view.SetGhost(i, back != null ? SpriteOf(back.Type) : null);
                    }

                    offset += b.Height;
                }
            }

            foreach (var id in _shelves.Keys.Where(id => !live.Contains(id)).ToList())
            {
                var view = _shelves[id];
                _shelves.Remove(id);
                if (instant)
                {
                    _shelfPool.Release(view);
                    continue;
                }

                _leavingShelves.Add(view);
                _overlay.Burst(view.Root.worldBound.center, EffectKind.Dust, 16);
                _audio.Play(SoundId.Fold);
                view.Root.Kill();
                view.Root.DOScale(new Vector2(1f, 0f), 0.3f).SetEase(Ease.InBack).SetId(this)
                    .OnComplete(() =>
                    {
                        _leavingShelves.Remove(view);
                        _shelfPool.Release(view);
                    });
            }
        }

        private void RenderItems(LevelState state, BoardChange change, float now, bool instant)
        {
            var wanted = new Dictionary<int, (VisualElement Cell, Jar Jar, float Ready, bool IsCounter, SoundId? Landing)>();
            foreach (var col in state.Columns)
            {
                foreach (var b in col.Blocks)
                {
                    var shelf = _shelves[b.Id];
                    for (var i = 0; i < b.Slots.Length; i++)
                    {
                        var j = b.Slots[i];
                        if (j != null)
                        {
                            wanted[j.Id] = (shelf.Cell(i), j, shelf.SettledAt, false, null);
                        }
                    }
                }
            }

            for (var i = 0; i < state.Counter.Count; i++)
            {
                wanted[state.Counter[i].Id] = (_counterSlots[i], state.Counter[i], 0f, true, SoundId.Counter);
            }

            foreach (var b in state.Baskets)
            {
                if (b == null || !_baskets.TryGetValue(b.Id, out var basket))
                {
                    continue;
                }

                for (var i = 0; i < b.Jars.Count; i++)
                {
                    wanted[b.Jars[i].Id] = (basket.Slot(i), b.Jars[i], basket.SettledAt, false, SoundId.Clink);
                }
            }

            foreach (var kv in wanted)
            {
                var (cell, jar, ready, isCounter, landing) = kv.Value;
                if (!_items.TryGetValue(kv.Key, out var view))
                {
                    view = _itemPool.Get();
                    view.Setup(jar.Id, jar.IsTool ? _art.Hammer : jar.IsWrapped ? _art.PaperBag : SpriteOf(jar.Type),
                        jar.IsWrapped, OnItemTapped);
                    _items[kv.Key] = view;
                    if (change.FromStock.TryGetValue(jar.Id, out var column))
                    {
                        Detach(view, _stockLabels[column].worldBound.center, _itemSize);
                        Fly(view, cell, Mathf.Max(0f, ready - now) + 0.06f, 70f, FlightSeconds, landing);
                    }
                    else
                    {
                        view.Snap(cell);
                    }

                    continue;
                }

                if (view.Destination == cell)
                {
                    continue;
                }

                if (instant)
                {
                    view.Snap(cell);
                    continue;
                }

                var fromCounter = view.Destination != null && _counterSlots.Contains(view.Destination);
                var hop = isCounter && fromCounter;
                var lift = isCounter ? 46f : 72f;
                if (change.Kind == BoardChangeKind.Shuffle)
                {
                    lift = 34f;
                }
                Detach(view, view.Root.worldBound.center, view.Root.layout.size);
                // Items sliding along the counter after one leaves are not new arrivals: no landing sound.
                Fly(view, cell, Mathf.Max(0f, ready - now), hop ? 0f : lift, hop ? CounterHopSeconds : FlightSeconds,
                    hop ? null : landing);
            }

            foreach (var id in _items.Keys.Where(id => !wanted.ContainsKey(id)).ToList())
            {
                var view = _items[id];
                _items.Remove(id);
                if (instant)
                {
                    _itemPool.Release(view);
                    continue;
                }

                if (change.ToStock.TryGetValue(id, out var column))
                {
                    FlyAway(view, _stockLabels[column].worldBound.center);
                }
                else if (IsHammerUse(change, id))
                {
                    var target = change.Events.First(e => e.Type == LevelEventType.ShelfUnboarded);
                    FlyAway(view, _shelves.TryGetValue(target.Id, out var s) ? s.Root.worldBound.center : view.Root.worldBound.center);
                }
                else
                {
                    FlyAway(view, view.Root.worldBound.center + Vector2.down * 40f);
                }
            }
        }

        private void OnItemTapped(ItemView view) => _itemTapped.OnNext(view.JarId);

        private static bool IsHammerUse(BoardChange change, int jarId) =>
            change.Events.Any(e => e.Type == LevelEventType.ShelfUnboarded && e.Other == jarId);

        private void Detach(ItemView view, Vector2 panelCenter, Vector2 size)
        {
            view.Root.Kill();
            _flight.Add(view.Root);
            view.Root.PlaceCentered(_flight.WorldToLocal(panelCenter), size);
            view.Root.style.right = StyleKeyword.Null;
            view.Root.style.bottom = StyleKeyword.Null;
        }

        private void Fly(ItemView view, VisualElement cell, float delay, float lift, float seconds, SoundId? landing)
        {
            view.Destination = cell;
            var root = view.Root;
            var start = VisualElementTweens.GetPosition(root) + new Vector2(root.style.width.value.value, root.style.height.value.value) / 2f;
            var startSize = new Vector2(root.style.width.value.value, root.style.height.value.value);
            DOTween.To(() => 0f, t =>
                {
                    var end = _flight.WorldToLocal(cell.worldBound.center);
                    var endSize = cell.layout.size;
                    if (float.IsNaN(endSize.x))
                    {
                        endSize = startSize;
                    }
                    var p = Vector2.Lerp(start, end, t) + Vector2.down * (lift * 4f * t * (1f - t));
                    root.PlaceCentered(p, Vector2.Lerp(startSize, endSize, t));
                    VisualElementTweens.SetRotation(root, lift > 0f ? -8f * Mathf.Sin(t * Mathf.PI) : 0f);
                }, 1f, seconds)
                .SetEase(Ease.InOutQuad)
                .SetDelay(delay)
                .SetTarget(root)
                .SetId(this)
                .OnComplete(() =>
                {
                    if (view.Destination != cell)
                    {
                        return;
                    }

                    view.Snap(cell);
                    if (landing.HasValue)
                    {
                        _audio.Play(landing.Value);
                    }
                });
        }

        private void FlyAway(ItemView view, Vector2 panelTarget)
        {
            Detach(view, view.Root.worldBound.center, view.Root.layout.size);
            _leavingItems.Add(view);
            var size = new Vector2(view.Root.style.width.value.value, view.Root.style.height.value.value);
            var target = _flight.WorldToLocal(panelTarget) - size / 2f;
            DOTween.Sequence().SetId(this)
                .Append(view.Root.DOPosition(target, 0.48f).SetEase(Ease.InQuad))
                .Join(view.Root.DOScale(0.35f, 0.48f))
                .OnComplete(() =>
                {
                    if (_leavingItems.Remove(view))
                    {
                        _itemPool.Release(view);
                    }
                });
        }

        private Sprite SpriteOf(int type) => type == Jar.HammerType ? _art.Hammer : _catalog.Get(type).Sprite;

        private static Jar FindJar(LevelState state, int jarId)
        {
            foreach (var col in state.Columns)
            {
                foreach (var b in col.Blocks)
                {
                    foreach (var j in b.Slots)
                    {
                        if (j != null && j.Id == jarId)
                        {
                            return j;
                        }
                    }
                }
            }

            return null;
        }
    }
}
