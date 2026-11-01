using System;
using Game.Services.UI.Elements;
using UnityEngine.Pool;

namespace Game.Services.Pooling
{
    /// <summary>
    /// Pool of UI Toolkit views (board items, shelves, baskets, particles). Released views are detached from the
    /// panel, so only elements that are on screen exist in the visual tree.
    /// </summary>
    /// <typeparam name="T">Pooled view type.</typeparam>
    public sealed class ElementPool<T> : IDisposable where T : ElementView, IPoolable
    {
        private readonly ObjectPool<T> _pool;

        /// <summary>
        /// Creates the pool.
        /// </summary>
        /// <param name="create">Creates and attaches a new view (not yet added to any parent).</param>
        /// <param name="defaultCapacity">Initial capacity.</param>
        /// <param name="maxSize">Maximum number of kept inactive views.</param>
        public ElementPool(Func<T> create, int defaultCapacity, int maxSize)
        {
            _pool = new ObjectPool<T>(
                create,
                view => view.OnSpawned(),
                view =>
                {
                    view.OnDespawned();
                    view.Root.Kill();
                    view.Root.RemoveFromHierarchy();
                },
                view => view.Root.RemoveFromHierarchy(),
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                true,
#else
                false,
#endif
                defaultCapacity,
                maxSize);
        }

        /// <summary>
        /// Takes a view from the pool (the caller adds it to a parent).
        /// </summary>
        /// <returns>A ready view.</returns>
        public T Get() => _pool.Get();

        /// <summary>
        /// Returns a view; it is removed from the panel.
        /// </summary>
        /// <param name="view">View to return.</param>
        public void Release(T view) => _pool.Release(view);

        /// <inheritdoc/>
        public void Dispose() => _pool.Clear();
    }
}
