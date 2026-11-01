using System;
using UnityEngine;
using UnityEngine.Pool;
using VContainer;
using VContainer.Unity;
using Object = UnityEngine.Object;

namespace Game.Services.Pooling
{
    /// <summary>
    /// Typed pool of prefab instances built on <see cref="ObjectPool{T}"/>. Instances are created through the
    /// container (so <c>[Inject]</c> methods run once) and parked under a container transform while inactive.
    /// </summary>
    /// <typeparam name="T">Component on the prefab root.</typeparam>
    /// <remarks>Owned by the scope that created it; dispose it with that scope so no instance outlives it.</remarks>
    public sealed class ComponentPool<T> : IDisposable where T : Component
    {
        private readonly ObjectPool<T> _pool;
        private readonly Transform _container;

        /// <summary>
        /// Creates the pool.
        /// </summary>
        /// <param name="resolver">Instantiates with injection.</param>
        /// <param name="prefab">Prefab to clone.</param>
        /// <param name="container">Parent of inactive instances ("[Pool] ItemView").</param>
        /// <param name="defaultCapacity">Typical number of live instances.</param>
        /// <param name="maxSize">Hard cap of kept inactive instances; extra releases are destroyed.</param>
        public ComponentPool(IObjectResolver resolver, T prefab, Transform container, int defaultCapacity, int maxSize)
        {
            _container = container;
            _pool = new ObjectPool<T>(
                () => resolver.Instantiate(prefab, _container),
                item =>
                {
                    item.gameObject.SetActive(true);
                    if (item is IPoolable p) p.OnSpawned();
                },
                item =>
                {
                    if (item is IPoolable p) p.OnDespawned();
                    item.gameObject.SetActive(false);
                    item.transform.SetParent(_container, false);
                },
                item =>
                {
                    if (item != null) Object.Destroy(item.gameObject);
                },
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                true,
#else
                false,
#endif
                defaultCapacity,
                maxSize);
        }

        /// <summary>
        /// Takes an active instance; pass per-spawn data with an explicit setup method afterwards.
        /// </summary>
        /// <returns>The instance.</returns>
        public T Get() => _pool.Get();

        /// <summary>
        /// Returns an instance; treat any reference to it as invalid afterwards.
        /// </summary>
        /// <param name="item">Instance taken from this pool.</param>
        public void Release(T item) => _pool.Release(item);

        /// <inheritdoc/>
        public void Dispose() => _pool.Clear();
    }
}
