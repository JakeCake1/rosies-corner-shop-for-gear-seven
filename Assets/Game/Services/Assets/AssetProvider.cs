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

        /// <summary>
        /// Replaces the Addressables load with a test double; null uses Addressables.
        /// </summary>
        internal Func<object, AsyncOperationHandle> StartLoad { get; set; }

        /// <inheritdoc/>
        public async UniTask InitializeAsync(CancellationToken ct)
        {
            await Addressables.InitializeAsync().ToUniTask(cancellationToken: ct);
        }

        /// <inheritdoc/>
        public async UniTask<T> LoadAsync<T>(object key, CancellationToken ct) where T : class
        {
            ct.ThrowIfCancellationRequested();
            if (key is AssetReference reference)
            {
                key = reference.RuntimeKey;
            }

            if (_handles.TryGetValue(key, out var cached))
            {
                if (!cached.IsDone)
                {
                    try
                    {
                        await cached.ToUniTask(cancellationToken: ct);
                    }
                    catch (OperationCanceledException) when (ct.IsCancellationRequested)
                    {
                        // Only this caller stopped waiting; the shared load goes on.
                        throw;
                    }
                    catch
                    {
                        Evict(key, cached);
                        throw;
                    }
                }

                if (cached.Status == AsyncOperationStatus.Succeeded)
                {
                    return cached.Result as T;
                }

                // The load failed after its first caller stopped waiting (nobody evicted it): load again instead of
                // handing out null for the rest of the session.
                Evict(key, cached);
            }

            AsyncOperationHandle handle = StartLoad != null ? StartLoad(key) : Addressables.LoadAssetAsync<T>(key);
            _handles[key] = handle;
            try
            {
                await handle.ToUniTask(cancellationToken: ct);
                return handle.Result as T;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // Only this caller stopped waiting. The provider still owns the shared operation.
                throw;
            }
            catch
            {
                Evict(key, handle);
                throw;
            }
        }

        private void Evict(object key, AsyncOperationHandle handle)
        {
            // Another caller may have replaced the failed handle already.
            if (_handles.TryGetValue(key, out var current) && current.Equals(handle))
            {
                _handles.Remove(key);
            }

            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
        }

        /// <inheritdoc/>
        public void Release(object key)
        {
            if (key is AssetReference reference)
            {
                key = reference.RuntimeKey;
            }
            if (!_handles.TryGetValue(key, out var handle))
            {
                return;
            }

            _handles.Remove(key);
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
        }

        /// <inheritdoc/>
        public void ReleaseAll()
        {
            foreach (var handle in _handles.Values)
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }

            _handles.Clear();
        }

        /// <inheritdoc/>
        public void Dispose() => ReleaseAll();
    }
}
