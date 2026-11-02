using System;
using System.Collections.Generic;
using Game.Services.Pooling;

namespace Game.Features.Gameplay.Board
{
    /// <summary>
    /// Pool of board items that also tracks retired items: items that already left the board's books but are still
    /// flying away or riding a shipped basket. A retired item returns to the pool exactly once, either when its
    /// animation ends or when the board is rebuilt first.
    /// </summary>
    public sealed class ItemPool : IDisposable
    {
        private const int Capacity = 80;
        private const int MaxSize = 200;

        private readonly ElementPool<ItemView> _pool = new ElementPool<ItemView>(ItemView.Create, Capacity, MaxSize);
        private readonly HashSet<ItemView> _retired = new HashSet<ItemView>();

        /// <summary>
        /// Takes an item from the pool.
        /// </summary>
        /// <returns>An item ready for <see cref="ItemView.Setup"/>.</returns>
        public ItemView Get() => _pool.Get();

        /// <summary>
        /// Returns an item that is not retired, e.g. one removed without animation.
        /// </summary>
        /// <param name="view">The item.</param>
        public void Release(ItemView view)
        {
            _pool.Release(view);
        }

        /// <summary>
        /// Marks an item as leaving; release it later with <see cref="ReleaseRetired"/>.
        /// </summary>
        /// <param name="view">The item that left the board's books.</param>
        public void Retire(ItemView view)
        {
            _retired.Add(view);
        }

        /// <summary>
        /// Returns a retired item to the pool unless a rebuild already did.
        /// </summary>
        /// <param name="view">The retired item.</param>
        public void ReleaseRetired(ItemView view)
        {
            if (_retired.Remove(view))
            {
                _pool.Release(view);
            }
        }

        /// <summary>
        /// Returns the board's active items and every retired item to the pool. Called on rebuild.
        /// </summary>
        /// <param name="active">Items still on the board's books.</param>
        public void ReleaseAll(IEnumerable<ItemView> active)
        {
            foreach (var view in active)
            {
                _pool.Release(view);
            }

            foreach (var view in _retired)
            {
                _pool.Release(view);
            }

            _retired.Clear();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _pool.Dispose();
        }
    }
}
