using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Services.Content;

namespace Game.Tests.EditMode.Services.Content
{
    /// <summary>
    /// Content service over a catalog built in the test (see <c>TestContent</c>); loading does nothing.
    /// </summary>
    internal sealed class FakeContentService : IContentService
    {
        public ItemCatalog Catalog { get; }

        public FakeContentService(ItemCatalog catalog)
        {
            Catalog = catalog;
        }

        public UniTask LoadAsync(CancellationToken ct) => UniTask.CompletedTask;
    }
}
