using System.Threading;
using Cysharp.Threading.Tasks;

namespace Game.Services.Content
{
    /// <summary>
    /// Gives access to the always-loaded game content (the item catalog). Loaded once by the bootstrap state.
    /// </summary>
    public interface IContentService
    {
        /// <summary>
        /// The item catalog; valid after <see cref="LoadAsync"/> completed.
        /// </summary>
        ItemCatalog Catalog { get; }

        /// <summary>
        /// Loads the catalog through Addressables; the handle is kept for the app's lifetime.
        /// </summary>
        /// <param name="ct">Cancelled on app shutdown.</param>
        /// <returns>Completes when the catalog and its sprites are loaded.</returns>
        /// <exception cref="System.OperationCanceledException">Loading was cancelled.</exception>
        UniTask LoadAsync(CancellationToken ct);
    }
}
