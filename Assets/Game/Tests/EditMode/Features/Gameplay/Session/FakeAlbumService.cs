using System.Collections.Generic;
using Game.Services.Album;

namespace Game.Tests.EditMode.Features.Gameplay.Session
{
    internal sealed class FakeAlbumService : IAlbumService
    {
        private readonly List<int> _found = new List<int>();

        public IReadOnlyList<int> Found => _found;

        /// <summary>Department reported as completed by the next <see cref="RegisterFound"/>; null for none.</summary>
        public string NextCompletedFamily { get; set; }

        public int NewCount => 0;

        public bool IsFamilyCompleted(string family) => false;

        public string RegisterFound(int itemId)
        {
            if (_found.Contains(itemId))
            {
                return null;
            }

            _found.Add(itemId);
            var family = NextCompletedFamily;
            NextCompletedFamily = null;
            return family;
        }

        public void MarkSeen()
        {
        }
    }
}
