using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Game.Features.Gameplay.Model;
using Game.Features.Gameplay.Session;
using Game.Services.Content;
using Game.Services.Pooling;
using Game.Services.UI;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Game.Features.Gameplay.Board
{
    /// <summary>
    /// The shop board of the gameplay scene: basket ledge, counter, three-column cabinet with stock labels, and
    /// the layer items fly through. Mirrors <see cref="LevelState"/> by reconciling views against it on every
    /// change (a port of the prototype's <c>render()</c>), so animations follow the rules instead of driving them.
    /// </summary>
    /// <remarks>
    /// Contains no game rules: taps and button clicks are forwarded through observables to the presenter.
    /// Item, shelf and basket views are pooled.
    /// </remarks>
    public sealed class BoardView : MonoBehaviour
    {
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
        private const float CabinetMinGap = 38f;
        private const float CabinetOverhead = 72f;
        private const float BoosterBarHeight = 100f;
        private const float MinRowHeight = 40f;
        private const float ShelfDropSeconds = 0.52f;
        private const float FlightSeconds = 0.43f;
        private const float CounterHopSeconds = 0.22f;

        [Header("Layout")]
        [SerializeField] private RectTransform _content;
        [SerializeField] private RectTransform _basketArea;
        [SerializeField] private RectTransform _basketLayer;
        [SerializeField] private Button[] _basketLocks = new Button[3];
        [SerializeField] private RectTransform _counter;
        [SerializeField] private RectTransform[] _counterSlots = new RectTransform[6];
        [SerializeField] private Image[] _counterDoilies = new Image[6];
        [SerializeField] private RectTransform _cabinet;
        [SerializeField] private RectTransform[] _columns = new RectTransform[3];
        [SerializeField] private TMP_Text[] _stockLabels = new TMP_Text[3];
        [SerializeField] private GameObject[] _stockHammers = new GameObject[3];
        [SerializeField] private RectTransform _boosterBar;
        [SerializeField] private RectTransform _flightLayer;
        [SerializeField] private RectTransform _progressTarget;
        [Header("Art")]
        [SerializeField] private Sprite _hammerSprite;
        [SerializeField] private Sprite _bagSprite;
        [SerializeField] private Sprite _doily;
        [SerializeField] private Sprite _doilyHot;
        [SerializeField] private Image _keyFlyer;
        [Header("Pools")]
        [SerializeField] private ItemView _itemPrefab;
        [SerializeField] private ShelfView _shelfPrefab;
        [SerializeField] private BasketView _basketPrefab;
        [SerializeField] private RectTransform _poolContainer;

        private readonly Dictionary<int, ItemView> _items = new Dictionary<int, ItemView>();
        private readonly Dictionary<int, ShelfView> _shelves = new Dictionary<int, ShelfView>();
        private readonly Dictionary<int, BasketView> _baskets = new Dictionary<int, BasketView>();
        private readonly Subject<int> _itemTapped = new Subject<int>();
        private readonly Subject<Unit> _extraBasketClicked = new Subject<Unit>();
        private IOverlayService _overlay;
        private ItemCatalog _catalog;
        private ComponentPool<ItemView> _itemPool;
        private ComponentPool<ShelfView> _shelfPool;
        private ComponentPool<BasketView> _basketPool;
        private Vector2 _itemSize;
        private float _rowHeight;
        private int _visibleRows;
        private int _jarsPerRow;
        private LevelState _state;
        private Vector2 _laidOutSize;
        private bool _isResizePending;

        /// <summary>Emits the jar id of a tapped shelf item.</summary>
        public IObservable<int> ItemTapped => _itemTapped;

        /// <summary>Emits when a locked basket slot's "+" is tapped.</summary>
        public IObservable<Unit> ExtraBasketClicked => _extraBasketClicked;

        /// <summary>The counter panel (anchor for coach tips and the shake).</summary>
        public RectTransform Counter => _counter;

        /// <summary>The basket ledge (anchor for the "ship" tip).</summary>
        public RectTransform BasketArea => _basketArea;

        /// <summary>The stock labels row (anchor for the "stock" tip).</summary>
        public RectTransform StockLabels => (RectTransform)_stockLabels[1].transform.parent;

        /// <summary>The booster bar (anchor for booster gift bubbles).</summary>
        public RectTransform BoosterBar => _boosterBar;

        /// <summary>
        /// Receives dependencies and creates the pools; called once when the scene scope builds.
        /// </summary>
        /// <param name="resolver">Creates pooled views with injection (infrastructure use only).</param>
        /// <param name="overlay">Bursts and praise.</param>
        /// <param name="content">Item catalog for sprites.</param>
        [Inject]
        public void Construct(IObjectResolver resolver, IOverlayService overlay, IContentService content)
        {
            _overlay = overlay;
            _catalog = content.Catalog;
            _itemPool = new ComponentPool<ItemView>(resolver, _itemPrefab, _poolContainer, 80, 200);
            _shelfPool = new ComponentPool<ShelfView>(resolver, _shelfPrefab, _poolContainer, 24, 60);
            _basketPool = new ComponentPool<BasketView>(resolver, _basketPrefab, _poolContainer, 4, 8);
            for (var i = 0; i < _basketLocks.Length; i++)
            {
                _basketLocks[i].onClick.AddListener(OnLockClicked);
            }
        }

        /// <summary>
        /// Clears the board and draws a new level; the opening shelves drop in column by column.
        /// </summary>
        /// <param name="state">The new level state.</param>
        /// <param name="isAnimated">Drop shelves in (level start) or snap (undo rebuild).</param>
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
            if (change.Kind == BoardChangeKind.Layout) Layout(state);

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
            _counter.DOKill(true);
            _counter.DOShakeAnchorPos(0.4f, new Vector2(10f, 0f), 18, 0f).SetLink(gameObject);
            if (_items.TryGetValue(jarId, out var item)) item.Nope();
        }

        /// <summary>
        /// Plays the start of a shipment: napkin, sparks and praise over the basket.
        /// </summary>
        /// <param name="notice">The shipment.</param>
        public void PlayShipping(ShipNotice notice)
        {
            if (!_baskets.TryGetValue(notice.BasketId, out var basket)) return;

            basket.CoverWithNapkin();
            var at = basket.Rect.position;
            _overlay.Burst(at, EffectKind.Spark, 20 + notice.Combo * 6);
            _overlay.Praise(at + Vector3.up * 30f * basket.Rect.lossyScale.y, notice.Word, notice.Combo > 0);
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
        /// Returns the view of an item on the board, for coach anchors and the tutorial hand.
        /// </summary>
        /// <param name="jarId">Item id.</param>
        /// <returns>The item's rect, or null.</returns>
        public RectTransform FindItem(int jarId) => _items.TryGetValue(jarId, out var v) ? v.Rect : null;

        /// <summary>
        /// Returns the "+" button of the first locked basket slot, for the level 3 tip.
        /// </summary>
        /// <returns>The button rect, or null when all baskets are open.</returns>
        public RectTransform FirstBasketLock()
        {
            foreach (var b in _basketLocks)
            {
                if (b != null && b.gameObject.activeSelf) return (RectTransform)b.transform;
            }

            return null;
        }

        private void OnRectTransformDimensionsChange() => _isResizePending = true;

        private void LateUpdate()
        {
            if (!_isResizePending) return;

            _isResizePending = false;
            // Screen rotated or the window was resized: lay the level out again for the new size.
            if (_state != null && _content.rect.size != _laidOutSize) Rebuild(_state, false);
        }

        private void OnDestroy()
        {
            for (var i = 0; i < _basketLocks.Length; i++)
            {
                if (_basketLocks[i] != null) _basketLocks[i].onClick.RemoveListener(OnLockClicked);
            }

            ClearAll();
            _itemPool?.Dispose();
            _shelfPool?.Dispose();
            _basketPool?.Dispose();
            _itemTapped.Dispose();
            _extraBasketClicked.Dispose();
        }

        private void OnLockClicked() => _extraBasketClicked.OnNext(Unit.Default);

        private void OnItemTapped(ItemView view) => _itemTapped.OnNext(view.JarId);

        private void ClearAll()
        {
            DOTween.Kill(this);
            foreach (var v in _items.Values)
            {
                _itemPool?.Release(v);
            }

            foreach (var v in _shelves.Values)
            {
                _shelfPool?.Release(v);
            }

            foreach (var v in _baskets.Values)
            {
                _basketPool?.Release(v);
            }

            _items.Clear();
            _shelves.Clear();
            _baskets.Clear();
        }

        /// <summary>
        /// Sizes everything for the level and the screen (prototype <c>layout()</c> + <c>fitCabinet()</c>).
        /// </summary>
        private void Layout(LevelState state)
        {
            var size = _content.rect.size;
            _laidOutSize = size;
            var inner = Mathf.Min(MaxContentWidth, size.x) - SidePadding * 2f;
            _visibleRows = state.VisibleRows;
            _jarsPerRow = state.JarsPerRow;

            var slot = Mathf.Floor(Mathf.Min(58f, (inner - 60f) / state.Capacity));
            var counterHeight = slot * 1.3f + CounterPadding;
            var y = -TopPadding - HudHeight;
            Place(_basketArea, inner, BasketAreaHeight, y);
            y -= BasketAreaHeight + CounterTopGap;
            Place(_counter, inner, counterHeight, y);
            LayoutCounter(slot, inner);
            y -= counterHeight;

            var columnWidth = (inner - 24f) / 3f;
            var jwMax = (columnWidth - 10f) / state.JarsPerRow;
            var available = size.y + y - BoosterBarHeight - CabinetMinGap - 8f - CabinetOverhead;
            _rowHeight = Mathf.Max(MinRowHeight, Mathf.Min(jwMax / ItemAspect + 10f, available / state.VisibleRows));
            var itemHeight = _rowHeight - 10f;
            _itemSize = new Vector2(Mathf.Min(itemHeight * ItemAspect, jwMax), itemHeight);

            var cabinetHeight = CabinetOverhead + state.VisibleRows * _rowHeight;
            var spare = Mathf.Max(0f, available - state.VisibleRows * _rowHeight);
            Place(_cabinet, inner, cabinetHeight, y - CabinetMinGap - spare / 2f);
            for (var c = 0; c < LevelState.ColumnCount; c++)
            {
                _columns[c].sizeDelta = new Vector2(_columns[c].sizeDelta.x, state.VisibleRows * _rowHeight);
            }

            for (var i = 0; i < LevelState.MaxBaskets; i++)
            {
                var lockRect = (RectTransform)_basketLocks[i].transform;
                lockRect.anchoredPosition = new Vector2(SlotCenterX(i), lockRect.anchoredPosition.y);
            }
        }

        private void LayoutCounter(float slot, float inner)
        {
            var gap = 8f;
            var visible = _counterSlots.Count(s => s.gameObject.activeSelf);
            var count = Mathf.Max(visible, 5);
            var total = count * slot + (count - 1) * gap;
            for (var i = 0; i < _counterSlots.Length; i++)
            {
                var rt = _counterSlots[i];
                rt.sizeDelta = new Vector2(slot, slot / ItemAspect * 0.62f);
                rt.anchoredPosition = new Vector2(-total / 2f + slot / 2f + i * (slot + gap), rt.anchoredPosition.y);
            }
        }

        private static void Place(RectTransform rt, float width, float height, float top)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(width, height);
            rt.anchoredPosition = new Vector2(0f, top);
        }

        private float SlotCenterX(int slot) => _basketArea.rect.width * (slot * 2 + 1) / 6f - _basketArea.rect.width / 2f;

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
                            if (jar != null) item.Unwrap(SpriteOf(jar.Type));
                        }

                        break;
                    case LevelEventType.ShelfUnlocked:
                        FlyKey(e.Other, e.Id);
                        break;
                    case LevelEventType.ShelfUnboarded:
                        if (_shelves.TryGetValue(e.Id, out var nailed))
                        {
                            _overlay.Burst(nailed.Rect.position, EffectKind.Dust, 18);
                            nailed.KnockBoardsOff();
                        }

                        break;
                    case LevelEventType.ShelfThawed:
                        if (_shelves.TryGetValue(e.Id, out var thawed))
                        {
                            _overlay.Burst(thawed.Rect.position, EffectKind.Spark, 22,
                                new[] { Color.white, new Color(0.75f, 0.89f, 1f), new Color(0.5f, 0.75f, 0.94f) });
                        }

                        break;
                }
            }
        }

        private void FlyKey(int fromBlock, int toBlock)
        {
            if (!_shelves.TryGetValue(toBlock, out var target)) return;

            var start = _shelves.TryGetValue(fromBlock, out var source) ? source.KeyPosition() : target.Rect.position;
            _keyFlyer.gameObject.SetActive(true);
            _keyFlyer.rectTransform.position = start;
            _keyFlyer.rectTransform.DOKill();
            _keyFlyer.rectTransform.DOJump(target.Rect.position, 80f * _content.lossyScale.y, 1, 0.55f)
                .SetLink(gameObject)
                .OnComplete(() =>
                {
                    _keyFlyer.gameObject.SetActive(false);
                    _overlay.Burst(target.Rect.position, EffectKind.Spark, 16);
                });
        }

        private void RenderBaskets(LevelState state, float now, bool instant)
        {
            var live = new HashSet<int>();
            for (var i = 0; i < LevelState.MaxBaskets; i++)
            {
                var b = state.Baskets[i];
                var isLockedSlot = i >= state.UnlockedBaskets;
                _basketLocks[i].gameObject.SetActive(isLockedSlot &&
                                                     (state.LevelNumber >= GameplayBalance.ExtraBasketSlotLevel || state.LevelNumber == 0));
                if (b == null) continue;

                live.Add(b.Id);
                if (!_baskets.TryGetValue(b.Id, out var view))
                {
                    view = _basketPool.Get();
                    view.Rect.SetParent(_basketLayer, false);
                    view.Setup(b.Id, SpriteOf(b.Type));
                    view.Rect.anchoredPosition = new Vector2(SlotCenterX(i), 0f);
                    view.Rect.sizeDelta = new Vector2(BasketWidth, BasketHeight);
                    _baskets[b.Id] = view;
                    view.SettledAt = now;
                    if (!instant)
                    {
                        view.Enter(i == 0).SetId(this);
                        view.SettledAt = now + 0.52f;
                    }
                }

                view.Rect.anchoredPosition = new Vector2(SlotCenterX(i), view.Rect.anchoredPosition.y);
                view.SetCount(b.Jars.Count);
            }

            foreach (var id in _baskets.Keys.Where(id => !live.Contains(id)).ToList())
            {
                var view = _baskets[id];
                _baskets.Remove(id);
                ForgetItemsInside(view.Rect);
                if (instant)
                {
                    ReleaseItemsInside(view.Rect);
                    _basketPool.Release(view);
                    continue;
                }

                view.Leave(_progressTarget.position).SetId(this).OnComplete(() =>
                {
                    _overlay.Burst(_progressTarget.position, EffectKind.Spark, 10);
                    _progressTarget.DOKill(true);
                    _progressTarget.DOPunchScale(Vector3.one * 0.08f, 0.3f, 6).SetLink(gameObject);
                    ReleaseItemsInside(view.Rect);
                    _basketPool.Release(view);
                });
            }
        }

        private void ForgetItemsInside(RectTransform container)
        {
            foreach (var item in container.GetComponentsInChildren<ItemView>(true))
            {
                if (_items.TryGetValue(item.JarId, out var registered) && registered == item) _items.Remove(item.JarId);
            }
        }

        private void ReleaseItemsInside(RectTransform container)
        {
            foreach (var item in container.GetComponentsInChildren<ItemView>(true))
            {
                _itemPool.Release(item);
            }
        }

        private void RenderCounter(LevelState state)
        {
            var isHot = state.Counter.Count >= state.Capacity - 1;
            for (var i = 0; i < _counterSlots.Length; i++)
            {
                _counterSlots[i].gameObject.SetActive(i < state.Capacity);
                _counterDoilies[i].sprite = isHot && i < state.Counter.Count ? _doilyHot : _doily;
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
                if (hasHammer) stock--;
                if (stock > 0) _stockLabels[c].SetText("<b>{0}</b> in stock", stock);
                else _stockLabels[c].SetText("all out");
                _stockHammers[c].SetActive(hasHammer || tiedHammer);

                var offset = 0;
                var fresh = 0;
                foreach (var b in col.Blocks)
                {
                    live.Add(b.Id);
                    var bottom = offset * _rowHeight;
                    if (!_shelves.TryGetValue(b.Id, out var view))
                    {
                        view = _shelfPool.Get();
                        view.Rect.SetParent(_columns[c], false);
                        view.Rect.anchorMin = new Vector2(0f, 0f);
                        view.Rect.anchorMax = new Vector2(1f, 0f);
                        view.Rect.pivot = new Vector2(0.5f, 0f);
                        view.Rect.sizeDelta = new Vector2(0f, b.Height * _rowHeight - 3f);
                        view.Setup(b.Id, b.Height, _jarsPerRow, _rowHeight, _itemSize);
                        _shelves[b.Id] = view;
                        if (instant)
                        {
                            view.Rect.anchoredPosition = new Vector2(0f, bottom);
                            view.SettledAt = now;
                        }
                        else
                        {
                            var delay = dropBase + (isStart ? c * 0.12f : 0f) + fresh * 0.1f;
                            view.Rect.anchoredPosition = new Vector2(0f, _visibleRows * _rowHeight + 20f + fresh * _rowHeight * 0.5f);
                            view.Rect.DOAnchorPosY(bottom, ShelfDropSeconds).SetDelay(delay).SetEase(Ease.OutBack, 1.3f)
                                .SetLink(view.gameObject).SetId(this);
                            view.SettledAt = now + delay + ShelfDropSeconds;
                            fresh++;
                        }
                    }
                    else if (!Mathf.Approximately(view.Rect.anchoredPosition.y, bottom))
                    {
                        view.Rect.DOKill();
                        if (instant)
                        {
                            view.Rect.anchoredPosition = new Vector2(0f, bottom);
                        }
                        else
                        {
                            view.Rect.DOAnchorPosY(bottom, 0.4f).SetDelay(0.3f).SetEase(Ease.OutBack, 1.1f)
                                .SetLink(view.gameObject).SetId(this);
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

                _overlay.Burst(view.Rect.position, EffectKind.Dust, 16);
                view.Rect.DOKill();
                view.Rect.DOScaleY(0f, 0.3f).SetEase(Ease.InBack).SetLink(view.gameObject).SetId(this)
                    .OnComplete(() => _shelfPool.Release(view));
            }
        }

        private void RenderItems(LevelState state, BoardChange change, float now, bool instant)
        {
            var wanted = new Dictionary<int, (RectTransform Cell, Jar Jar, float Ready, bool IsCounter)>();
            foreach (var col in state.Columns)
            {
                foreach (var b in col.Blocks)
                {
                    var shelf = _shelves[b.Id];
                    for (var i = 0; i < b.Slots.Length; i++)
                    {
                        var j = b.Slots[i];
                        if (j != null) wanted[j.Id] = (shelf.Cell(i), j, shelf.SettledAt, false);
                    }
                }
            }

            for (var i = 0; i < state.Counter.Count; i++)
            {
                wanted[state.Counter[i].Id] = (_counterSlots[i], state.Counter[i], 0f, true);
            }

            foreach (var b in state.Baskets)
            {
                if (b == null || !_baskets.TryGetValue(b.Id, out var basket)) continue;

                for (var i = 0; i < b.Jars.Count; i++)
                {
                    wanted[b.Jars[i].Id] = (basket.Slot(i), b.Jars[i], basket.SettledAt, false);
                }
            }

            foreach (var kv in wanted)
            {
                var (cell, jar, ready, isCounter) = kv.Value;
                if (!_items.TryGetValue(kv.Key, out var view))
                {
                    view = _itemPool.Get();
                    view.Setup(jar.Id, jar.IsTool ? _hammerSprite : jar.IsWrapped ? _bagSprite : SpriteOf(jar.Type),
                        jar.IsWrapped, OnItemTapped);
                    _items[kv.Key] = view;
                    if (change.FromStock.TryGetValue(jar.Id, out var column))
                    {
                        Detach(view, _stockLabels[column].rectTransform.position, _itemSize);
                        Fly(view, cell, Mathf.Max(0f, ready - now) + 0.06f, 70f, FlightSeconds);
                    }
                    else
                    {
                        view.Snap(cell);
                    }

                    continue;
                }

                if (view.Destination == cell) continue;

                if (instant)
                {
                    view.Snap(cell);
                    continue;
                }

                var fromCounter = view.Destination != null && _counterSlots.Contains(view.Destination);
                var hop = isCounter && fromCounter;
                var lift = isCounter ? 46f : 72f;
                if (change.Kind == BoardChangeKind.Shuffle) lift = 34f;
                Detach(view, view.Rect.position, view.Rect.rect.size);
                Fly(view, cell, Mathf.Max(0f, ready - now), hop ? 0f : lift, hop ? CounterHopSeconds : FlightSeconds);
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
                    FlyAway(view, _stockLabels[column].rectTransform.position);
                }
                else if (view.JarId != 0 && _hammerSprite != null && IsHammerUse(change, id))
                {
                    var target = change.Events.First(e => e.Type == LevelEventType.ShelfUnboarded);
                    FlyAway(view, _shelves.TryGetValue(target.Id, out var s) ? s.Rect.position : view.Rect.position);
                }
                else
                {
                    FlyAway(view, view.Rect.position + Vector3.up * 40f * _content.lossyScale.y);
                }
            }
        }

        private static bool IsHammerUse(BoardChange change, int jarId) =>
            change.Events.Any(e => e.Type == LevelEventType.ShelfUnboarded && e.Other == jarId);

        private void Detach(ItemView view, Vector3 worldPosition, Vector2 size)
        {
            view.Rect.DOKill();
            view.Rect.SetParent(_flightLayer, true);
            view.Rect.anchorMin = view.Rect.anchorMax = new Vector2(0.5f, 0.5f);
            view.Rect.pivot = new Vector2(0.5f, 0.5f);
            view.Rect.sizeDelta = size;
            view.Rect.position = worldPosition;
            view.Rect.SetAsLastSibling();
        }

        private void Fly(ItemView view, RectTransform cell, float delay, float lift, float seconds)
        {
            view.Destination = cell;
            var targetSize = cell.rect.size;
            var corners = new Vector3[4];
            cell.GetWorldCorners(corners);
            var center = (corners[0] + corners[2]) / 2f;
            var power = lift * _content.lossyScale.y;
            DOTween.Sequence().SetLink(view.gameObject).SetId(this).SetDelay(delay)
                .Append(view.Rect.DOJump(center, power, 1, seconds).SetEase(Ease.InOutQuad))
                .Join(view.Rect.DOSizeDelta(targetSize, seconds))
                .Join(view.Rect.DOLocalRotate(new Vector3(0, 0, lift > 0 ? -8f : 0f), seconds * 0.5f).SetLoops(2, LoopType.Yoyo))
                .OnComplete(() =>
                {
                    if (view.Destination == cell) view.Snap(cell);
                });
        }

        private void FlyAway(ItemView view, Vector3 target)
        {
            Detach(view, view.Rect.position, view.Rect.rect.size);
            DOTween.Sequence().SetLink(view.gameObject).SetId(this)
                .Append(view.Rect.DOMove(target, 0.48f).SetEase(Ease.InQuad))
                .Join(view.Rect.DOScale(0.35f, 0.48f))
                .OnComplete(() => _itemPool.Release(view));
        }

        private Sprite SpriteOf(int type) => type == Jar.HammerType ? _hammerSprite : _catalog.Get(type).Sprite;

        private static Jar FindJar(LevelState state, int jarId)
        {
            foreach (var col in state.Columns)
            {
                foreach (var b in col.Blocks)
                {
                    foreach (var j in b.Slots)
                    {
                        if (j != null && j.Id == jarId) return j;
                    }
                }
            }

            return null;
        }
    }
}
