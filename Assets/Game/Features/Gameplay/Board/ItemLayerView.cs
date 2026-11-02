using System;
using System.Collections.Generic;
using System.Linq;
using Game.Features.Gameplay.Model;
using Game.Features.Gameplay.Session;
using Game.Services.Audio;
using Game.Services.UI;
using Game.Services.UI.Elements;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Features.Gameplay.Board
{
    /// <summary>
    /// Every item on the board. Reconciles the item views with the level state: items are snapped into their shelf,
    /// counter or basket cell, fly when they move and fly away when they leave. Lives on the board's flight layer.
    /// </summary>
    public sealed class ItemLayerView : ElementView, IDisposable
    {
        private const float FlightSeconds = 0.43f;
        private const float CounterHopSeconds = 0.22f;
        private const float StockFlightDelay = 0.06f;
        private const float StockLift = 70f;
        private const float CounterLift = 46f;
        private const float ShelfLift = 72f;
        private const float ShuffleLift = 34f;

        private readonly Dictionary<int, ItemView> _items = new Dictionary<int, ItemView>();
        private readonly ItemPool _pool = new ItemPool();
        private readonly Subject<int> _tapped = new Subject<int>();
        private readonly BoardSprites _sprites;
        private readonly IAudioService _audio;
        private readonly IOverlayService _overlay;
        private readonly object _tweenId;

        /// <summary>Emits the jar id of a tapped item.</summary>
        public IObservable<int> Tapped => _tapped;

        /// <summary>Arc flights on this layer; also used for the key flight.</summary>
        public ItemFlights Flights { get; private set; }

        /// <summary>
        /// Creates the layer; attach it to the board's flight element.
        /// </summary>
        /// <param name="sprites">Item pictures.</param>
        /// <param name="audio">Landing sounds.</param>
        /// <param name="overlay">Key sparks.</param>
        /// <param name="tweenId">Id of every flight tween; killed by the board on rebuild.</param>
        public ItemLayerView(BoardSprites sprites, IAudioService audio, IOverlayService overlay, object tweenId)
        {
            _sprites = sprites;
            _audio = audio;
            _overlay = overlay;
            _tweenId = tweenId;
        }

        /// <summary>
        /// Puts every item of <paramref name="state"/> into its cell and animates the items that moved or left.
        /// </summary>
        /// <param name="state">Current level state.</param>
        /// <param name="change">What changed; carries stock arrivals and departures.</param>
        /// <param name="now">Unscaled time of the change, for waiting on shelves and baskets that still move.</param>
        /// <param name="isInstant">Snap everything (undo rebuild, resize).</param>
        /// <param name="cells">Finds the cells of shelves, counter and baskets.</param>
        public void Render(LevelState state, BoardChange change, float now, bool isInstant, BoardCells cells)
        {
            var wanted = cells.Collect(state);
            foreach (var kv in wanted)
            {
                if (!_items.TryGetValue(kv.Key, out var view))
                {
                    Add(kv.Value, change, now, cells);
                }
                else if (view.Destination != kv.Value.Cell)
                {
                    Move(view, kv.Value, change, now, isInstant, cells);
                }
            }

            foreach (var id in _items.Keys.Where(id => !wanted.ContainsKey(id)).ToList())
            {
                Remove(id, change, isInstant, cells);
            }
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
        /// Returns the view of an item on the board.
        /// </summary>
        /// <param name="jarId">Item id.</param>
        /// <param name="view">The view, or null.</param>
        /// <returns>False when the item isn't on the board.</returns>
        public bool TryGet(int jarId, out ItemView view) => _items.TryGetValue(jarId, out view);

        /// <summary>
        /// Takes the items inside a leaving basket off the board's books; they keep riding the basket until
        /// <see cref="Release"/>.
        /// </summary>
        /// <param name="container">The leaving basket's root.</param>
        /// <returns>The items to release when the basket is gone.</returns>
        public List<ItemView> ForgetItemsInside(VisualElement container)
        {
            var inside = _items.Values.Where(i => container.Contains(i.Root)).ToList();
            foreach (var item in inside)
            {
                _items.Remove(item.JarId);
                _pool.Retire(item);
            }

            return inside;
        }

        /// <summary>
        /// Returns items to the pool unless a board rebuild already did.
        /// </summary>
        /// <param name="items">Items from <see cref="ForgetItemsInside"/>.</param>
        public void Release(IEnumerable<ItemView> items)
        {
            foreach (var item in items)
            {
                _pool.ReleaseRetired(item);
            }
        }

        /// <summary>
        /// Returns every item to the pool and empties the layer. Called on rebuild; flight tweens are killed by the board.
        /// </summary>
        public void Clear()
        {
            _pool.ReleaseAll(_items.Values);
            _items.Clear();
            Root.Clear();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _pool.Dispose();
            _tapped.Dispose();
        }

        /// <inheritdoc/>
        protected override void OnAttached()
        {
            Flights = new ItemFlights(Root, _audio, _overlay, _tweenId);
        }

        private void Add(BoardCell target, BoardChange change, float now, BoardCells cells)
        {
            var jar = target.Jar;
            var view = _pool.Get();
            view.Setup(jar.Id, _sprites.OfJar(jar), jar.IsWrapped, OnItemTapped);
            _items[jar.Id] = view;
            if (!change.FromStock.TryGetValue(jar.Id, out var column))
            {
                view.Snap(target.Cell);
                return;
            }

            Flights.Detach(view, cells.StockCenter(column), cells.ItemSize);
            var delay = Mathf.Max(0f, target.ReadyAt - now) + StockFlightDelay;
            Flights.Fly(view, target.Cell, delay, StockLift, FlightSeconds, target.Landing);
        }

        private void Move(ItemView view, BoardCell target, BoardChange change, float now, bool isInstant,
            BoardCells cells)
        {
            if (isInstant)
            {
                view.Snap(target.Cell);
                return;
            }

            var isFromCounter = view.Destination != null && cells.IsCounterSlot(view.Destination);
            var isHop = target.IsCounter && isFromCounter;
            var lift = change.Kind == BoardChangeKind.Shuffle ? ShuffleLift : target.IsCounter ? CounterLift : ShelfLift;
            Flights.Detach(view, view.Root.worldBound.center, view.Root.layout.size);
            // Items sliding along the counter after one leaves are not new arrivals: no landing sound.
            Flights.Fly(view, target.Cell, Mathf.Max(0f, target.ReadyAt - now), isHop ? 0f : lift,
                isHop ? CounterHopSeconds : FlightSeconds, isHop ? null : target.Landing);
        }

        private void Remove(int jarId, BoardChange change, bool isInstant, BoardCells cells)
        {
            var view = _items[jarId];
            _items.Remove(jarId);
            if (isInstant)
            {
                _pool.Release(view);
                return;
            }

            _pool.Retire(view);
            var target = cells.LeaveTarget(view.Root.worldBound.center, jarId, change);
            Flights.FlyAway(view, target, () => _pool.ReleaseRetired(view));
        }

        private void OnItemTapped(ItemView view)
        {
            _tapped.OnNext(view.JarId);
        }
    }
}
