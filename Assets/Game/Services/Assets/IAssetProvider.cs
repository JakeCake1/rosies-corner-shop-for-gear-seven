using System.Threading;
using Cysharp.Threading.Tasks;

namespace Game.Services.Assets
{
    /// <summary>
    /// The only gateway to Addressables: loads assets by key and owns their handles.
    /// </summary>
    public interface IAssetProvider
    {
        /// <summary>
        /// Initializes Addressables; called once by the bootstrap state before any load.
        /// </summary>
        /// <param name="ct">Cancelled on app shutdown.</param>
        /// <returns>Completes when the catalog is ready.</returns>
        /// <exception cref="System.OperationCanceledException">Initialization was cancelled.</exception>
        UniTask InitializeAsync(CancellationToken ct);

        /// <summary>
        /// Loads (or returns the cached) asset; repeated loads of one key share a handle.
        /// </summary>
        /// <typeparam name="T">Asset type.</typeparam>
        /// <param name="key">Address string or <c>AssetReference</c>.</param>
        /// <param name="ct">Cancels the wait; the load itself continues and stays cached.</param>
        /// <returns>The loaded asset.</returns>
        /// <exception cref="System.OperationCanceledException">The wait was cancelled.</exception>
        UniTask<T> LoadAsync<T>(object key, CancellationToken ct) where T : class;

        /// <summary>
        /// Releases one cached handle.
        /// </summary>
        /// <param name="key">The key used to load it.</param>
        void Release(object key);

        /// <summary>
        /// Releases every handle this provider holds.
        /// </summary>
        void ReleaseAll();
    }
}
