using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Services.Content;
using UnityEngine;

namespace Game.Tests.EditMode.Services.Content
{
    /// <summary>
    /// Content service over a catalog built in the test (see <c>TestContent</c>); loading does nothing and items have
    /// no sprites.
    /// </summary>
    internal sealed class FakeContentService : IContentService
    {
        public ItemCatalog Catalog { get; }

        public FakeContentService(ItemCatalog catalog)
        {
            Catalog = catalog;
        }

        public UniTask LoadAsync(CancellationToken ct) => UniTask.CompletedTask;

        public UniTask LoadItemSpritesAsync(IEnumerable<int> itemIds, CancellationToken ct) => UniTask.CompletedTask;

        public UniTask LoadAllItemSpritesAsync(CancellationToken ct) => UniTask.CompletedTask;

        public Sprite GetItemSprite(int itemId) => null;
    }
}
