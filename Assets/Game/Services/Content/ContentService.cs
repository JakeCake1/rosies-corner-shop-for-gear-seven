using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Services.Assets;

namespace Game.Services.Content
{
    /// <summary>
    /// Default <see cref="IContentService"/> backed by <see cref="IAssetProvider"/>.
    /// </summary>
    public sealed class ContentService : IContentService
    {
        private readonly IAssetProvider _assets;

        /// <summary>
        /// Creates the service.
        /// </summary>
        /// <param name="assets">Addressables access; owns the catalog handle.</param>
        public ContentService(IAssetProvider assets)
        {
            _assets = assets;
        }

        /// <inheritdoc/>
        public ItemCatalog Catalog { get; private set; }

        /// <inheritdoc/>
        public async UniTask LoadAsync(CancellationToken ct)
        {
            Catalog = await _assets.LoadAsync<ItemCatalog>(ItemCatalog.AddressKey, ct);
        }
    }
}
