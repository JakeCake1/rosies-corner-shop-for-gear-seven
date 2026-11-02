using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Services.Assets;
using UnityEngine;

namespace Game.Services.Content
{
    /// <summary>
    /// Default <see cref="IContentService"/> backed by <see cref="IAssetProvider"/>, which owns every handle.
    /// </summary>
    public sealed class ContentService : IContentService
    {
        private readonly IAssetProvider _assets;
        private readonly Dictionary<int, Sprite> _sprites = new Dictionary<int, Sprite>();

        /// <inheritdoc/>
        public ItemCatalog Catalog { get; private set; }

        /// <summary>
        /// Creates the service.
        /// </summary>
        /// <param name="assets">Addressables access; owns the catalog and sprite handles.</param>
        public ContentService(IAssetProvider assets)
        {
            _assets = assets;
        }

        /// <inheritdoc/>
        public async UniTask LoadAsync(CancellationToken ct)
        {
            Catalog = await _assets.LoadAsync<ItemCatalog>(ItemCatalog.AddressKey, ct);
        }

        /// <inheritdoc/>
        public async UniTask LoadItemSpritesAsync(IEnumerable<int> itemIds, CancellationToken ct)
        {
            var missing = itemIds.Distinct().Where(id => !_sprites.ContainsKey(id)).ToList();
            var sprites = await UniTask.WhenAll(missing.Select(id => LoadSpriteAsync(id, ct)));
            for (var i = 0; i < missing.Count; i++)
            {
                _sprites[missing[i]] = sprites[i];
            }
        }

        /// <inheritdoc/>
        public UniTask LoadAllItemSpritesAsync(CancellationToken ct) =>
            LoadItemSpritesAsync(Catalog.Items.Select(i => i.Id), ct);

        /// <inheritdoc/>
        public Sprite GetItemSprite(int itemId)
        {
            if (!_sprites.TryGetValue(itemId, out var sprite))
            {
                throw new InvalidOperationException(
                    $"Item {itemId} sprite is not loaded; call {nameof(LoadItemSpritesAsync)} first.");
            }

            return sprite;
        }

        private UniTask<Sprite> LoadSpriteAsync(int itemId, CancellationToken ct) =>
            _assets.LoadAsync<Sprite>(Catalog.Get(itemId).Sprite, ct);
    }
}
