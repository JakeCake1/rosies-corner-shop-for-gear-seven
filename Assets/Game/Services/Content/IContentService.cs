using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Services.Content
{
    /// <summary>
    /// Gives access to the game content: the always-loaded item catalog and the item sprites, which load per level
    /// from their own Addressables groups.
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
        /// <returns>Completes when the catalog data is loaded (item sprites are not).</returns>
        /// <exception cref="System.OperationCanceledException">Loading was cancelled.</exception>
        UniTask LoadAsync(CancellationToken ct);

        /// <summary>
        /// Loads the sprites of some items (and so the bundles holding them); loaded sprites stay cached.
        /// </summary>
        /// <param name="itemIds">Catalog ids, e.g. the items of the level about to start.</param>
        /// <param name="ct">Cancels the wait; loads already started stay cached.</param>
        /// <returns>Completes when every sprite is available through <see cref="GetItemSprite"/>.</returns>
        /// <exception cref="System.OperationCanceledException">The wait was cancelled.</exception>
        UniTask LoadItemSpritesAsync(IEnumerable<int> itemIds, CancellationToken ct);

        /// <summary>
        /// Loads the sprites of the whole catalog; used by screens that may show any item (home, album).
        /// </summary>
        /// <param name="ct">Cancels the wait; loads already started stay cached.</param>
        /// <returns>Completes when every item sprite is loaded.</returns>
        /// <exception cref="System.OperationCanceledException">The wait was cancelled.</exception>
        UniTask LoadAllItemSpritesAsync(CancellationToken ct);

        /// <summary>
        /// Returns a loaded item sprite.
        /// </summary>
        /// <param name="itemId">Catalog id.</param>
        /// <returns>The sprite.</returns>
        /// <exception cref="System.InvalidOperationException">The sprite was not loaded first.</exception>
        Sprite GetItemSprite(int itemId);
    }
}
