using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Game.Features.Gameplay.Model;
using Game.Features.Gameplay.Session;
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
    /// The basket ledge: customer baskets that arrive, fill up and fly into the HUD progress bar when shipped, and the
    /// "+" buttons of locked basket slots.
    /// </summary>
    public sealed class BasketLedgeView : ElementView, IDisposable
    {
        private const string BasketsName = "baskets";
        private const int PoolCapacity = 4;
        private const int PoolMaxSize = 8;
        private const float EnterSeconds = 0.52f;
        private const int ShipSparksBase = 20;
        private const int ShipSparksPerCombo = 6;
        private const float PraiseAbove = 30f;
        private const int ProgressSparks = 10;
        private const float ProgressPunch = 0.08f;
        private const float ProgressPunchSeconds = 0.3f;
        private const int ProgressPunchVibrato = 6;

        private readonly BasketLocks _locks;
        private readonly Dictionary<int, BasketView> _baskets = new Dictionary<int, BasketView>();
        private readonly HashSet<BasketView> _leaving = new HashSet<BasketView>();
        private readonly VisualTreeAsset _basketLayout;
        private readonly BoardSprites _sprites;
        private readonly ItemLayerView _items;
        private readonly IOverlayService _overlay;
        private readonly ILocalizationService _texts;
        private readonly VisualElement _progressTarget;
        private readonly int _extraSlotLevel;
        private VisualElement _layer;
        private ElementPool<BasketView> _pool;

        /// <summary>Emits when a locked basket slot's "+" is tapped.</summary>
        public IObservable<Unit> ExtraBasketClicked => _locks.Clicked;

        /// <summary>
        /// Creates the ledge; attach it to the board's basket area.
        /// </summary>
        /// <param name="basketLayout">Basket template.</param>
        /// <param name="sprites">Picture of the item a basket asks for.</param>
        /// <param name="items">Items riding a shipped basket are released through it.</param>
        /// <param name="overlay">Sparks and praise.</param>
        /// <param name="texts">Praise words.</param>
        /// <param name="progressTarget">HUD progress bar that shipped baskets fly into.</param>
        /// <param name="extraSlotLevel">Level from which the locked third slot shows its "+".</param>
        public BasketLedgeView(VisualTreeAsset basketLayout, BoardSprites sprites, ItemLayerView items,
            IOverlayService overlay, ILocalizationService texts, VisualElement progressTarget,
            int extraSlotLevel)
        {
            _basketLayout = basketLayout;
            _sprites = sprites;
            _items = items;
            _overlay = overlay;
            _texts = texts;
            _progressTarget = progressTarget;
            _extraSlotLevel = extraSlotLevel;
            _locks = new BasketLocks();
        }

        /// <summary>
        /// Reconciles the baskets and slot locks with <paramref name="state"/>.
        /// </summary>
        /// <param name="state">Current level state.</param>
        /// <param name="layout">The board layout.</param>
        /// <param name="now">Unscaled time of the change.</param>
        /// <param name="isInstant">Snap everything (undo rebuild, resize).</param>
        public void Render(LevelState state, BoardLayout layout, float now, bool isInstant)
        {
            var live = new HashSet<int>();
            for (var i = 0; i < LevelState.MaxBaskets; i++)
            {
                var isLockedSlot = i >= state.UnlockedBaskets;
                var isLockShown = state.LevelNumber >= _extraSlotLevel || state.LevelNumber == 0;
                _locks.Set(Root, i, isLockedSlot && isLockShown, layout.BasketSlotLeft(i));
                var basket = state.Baskets[i];
                if (basket == null)
                {
                    continue;
                }

                live.Add(basket.Id);
                var view = GetOrAdd(basket, i == 0, now, isInstant);
                view.Place(new Vector2(layout.BasketSlotLeft(i), 0f),
                    new Vector2(BoardLayout.BasketWidth, BoardLayout.BasketHeight));
                view.SetCount(basket.Jars.Count);
            }

            foreach (var id in _baskets.Keys.Where(id => !live.Contains(id)).ToList())
            {
                Ship(id, isInstant);
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
            _overlay.Burst(at, EffectKind.Spark, ShipSparksBase + notice.Combo * ShipSparksPerCombo);
            _overlay.Praise(at + Vector2.down * PraiseAbove, _texts.Get(notice.WordKey), notice.Combo > 0);
        }

        /// <summary>
        /// Returns the view of a basket on the ledge.
        /// </summary>
        /// <param name="basketId">Basket id.</param>
        /// <param name="view">The basket, or null.</param>
        /// <returns>False when the basket isn't on the ledge.</returns>
        public bool TryGet(int basketId, out BasketView view) => _baskets.TryGetValue(basketId, out view);

        /// <summary>
        /// Returns the "+" button of the first locked basket slot, for the extra-basket tip.
        /// </summary>
        /// <returns>The button, or null when all baskets are open.</returns>
        public VisualElement FirstLock() => _locks.First();

        /// <summary>
        /// Returns every basket to the pool. Called on rebuild; basket tweens are killed with their views.
        /// </summary>
        public void Clear()
        {
            foreach (var view in _baskets.Values.Concat(_leaving))
            {
                _pool.Release(view);
            }

            _baskets.Clear();
            _leaving.Clear();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _pool?.Dispose();
            _locks.Dispose();
        }

        /// <inheritdoc/>
        protected override void OnAttached()
        {
            _layer = Q(BasketsName);
            _pool = new ElementPool<BasketView>(() => LayoutViews.Create<BasketView>(_basketLayout), PoolCapacity,
                PoolMaxSize);
        }

        private BasketView GetOrAdd(Basket basket, bool isFromLeft, float now, bool isInstant)
        {
            if (_baskets.TryGetValue(basket.Id, out var view))
            {
                return view;
            }

            view = _pool.Get();
            _layer.Add(view.Root);
            view.Setup(basket.Id, _sprites.OfType(basket.Type));
            _baskets[basket.Id] = view;
            view.SettledAt = now;
            if (!isInstant)
            {
                view.Enter(isFromLeft);
                view.SettledAt = now + EnterSeconds;
            }

            return view;
        }

        private void Ship(int basketId, bool isInstant)
        {
            var view = _baskets[basketId];
            _baskets.Remove(basketId);
            var inside = _items.ForgetItemsInside(view.Root);
            if (isInstant)
            {
                _items.Release(inside);
                _pool.Release(view);
                return;
            }

            _leaving.Add(view);
            view.Leave(_progressTarget.worldBound.center).OnComplete(() =>
            {
                PunchProgress();
                _items.Release(inside);
                _leaving.Remove(view);
                _pool.Release(view);
            });
        }

        private void PunchProgress()
        {
            _overlay.Burst(_progressTarget.worldBound.center, EffectKind.Spark, ProgressSparks);
            _progressTarget.Kill(true);
            _progressTarget.DOPunchScale(ProgressPunch, ProgressPunchSeconds, ProgressPunchVibrato);
        }
    }
}
