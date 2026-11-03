using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Services.Save;

namespace Game.Tests.EditMode.Services.Save
{
    /// <summary>
    /// <see cref="ISaveService"/> that loads through a real <see cref="SaveService"/> over in-memory storage and
    /// records saved chunk keys instead of writing, so save routing is checked synchronously.
    /// </summary>
    internal sealed class RecordingSaveService : ISaveService
    {
        public readonly InMemorySaveStorage Storage = new InMemorySaveStorage();
        public readonly List<string> Saved = new List<string>();
        public readonly List<CancellationToken> Tokens = new List<CancellationToken>();
        private readonly SaveService _inner;

        public RecordingSaveService()
        {
            _inner = new SaveService(Storage);
        }

        public bool TryLoad(IProfileChunk chunk) => _inner.TryLoad(chunk);

        public bool TryMigrateLegacy(IReadOnlyList<IProfileChunk> chunks) => _inner.TryMigrateLegacy(chunks);

        public UniTask SaveAsync(IProfileChunk chunk, CancellationToken ct)
        {
            Saved.Add(chunk.Key);
            Tokens.Add(ct);
            return UniTask.CompletedTask;
        }

        public void Save(IProfileChunk chunk) => Saved.Add(chunk.Key);
    }
}
