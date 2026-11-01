using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Game.Services.Assets
{
    /// <summary>
    /// Addressables-backed <see cref="IAssetProvider"/> that caches handles by key and releases them on dispose.
    /// </summary>
    /// <remarks>Registered in the project scope; scene-scoped providers would release on scene unload.</remarks>
    public sealed class AssetProvider : IAssetProvider, IDisposable
    {
        private readonly Dictionary<object, AsyncOperationHandle> _handles = new Dictionary<object, AsyncOperationHandle>();

        /// <inheritdoc/>
        public async UniTask InitializeAsync(CancellationToken ct)
        {
            await Addressables.InitializeAsync().ToUniTask(cancellationToken: ct);
        }

        /// <inheritdoc/>
        public async UniTask<T> LoadAsync<T>(object key, CancellationToken ct) where T : class
        {
            if (key is AssetReference reference) key = reference.RuntimeKey;

            if (_handles.TryGetValue(key, out var cached))
            {
                if (!cached.IsDone) await cached.ToUniTask(cancellationToken: ct);
                return cached.Result as T;
            }

            var handle = Addressables.LoadAssetAsync<T>(key);
            _handles[key] = handle;
            try
            {
                return await handle.ToUniTask(cancellationToken: ct);
            }
            catch
            {
                _handles.Remove(key);
                if (handle.IsValid()) Addressables.Release(handle);
                throw;
            }
        }

        /// <inheritdoc/>
        public void Release(object key)
        {
            if (key is AssetReference reference) key = reference.RuntimeKey;
            if (!_handles.TryGetValue(key, out var handle)) return;

            _handles.Remove(key);
            if (handle.IsValid()) Addressables.Release(handle);
        }

        /// <inheritdoc/>
        public void ReleaseAll()
        {
            foreach (var handle in _handles.Values)
            {
                if (handle.IsValid()) Addressables.Release(handle);
            }

            _handles.Clear();
        }

        /// <inheritdoc/>
        public void Dispose() => ReleaseAll();
    }
}
