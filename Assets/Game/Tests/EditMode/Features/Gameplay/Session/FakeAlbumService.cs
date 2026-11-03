using System.Collections.Generic;
using Game.Services.Album;

namespace Game.Tests.EditMode.Features.Gameplay.Session
{
    internal sealed class FakeAlbumService : IAlbumService
    {
        public IReadOnlyList<int> Found { get; } = new List<int>();

        public int NewCount => 0;

        public bool IsFamilyCompleted(string family) => false;

        public string RegisterFound(int itemId) => null;

        public void MarkSeen()
        {
        }
    }
}
