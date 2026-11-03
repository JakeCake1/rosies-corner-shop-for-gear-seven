using System.Collections.Generic;
using Game.Services.Levels;

namespace Game.Tests.EditMode.Features.Gameplay.Session
{
    internal sealed class FakeLevelProgressService : ILevelProgressService
    {
        private readonly Dictionary<int, int> _stars = new Dictionary<int, int>();
        private readonly HashSet<int> _failed = new HashSet<int>();

        public IReadOnlyDictionary<int, int> Stars => _stars;

        public bool IsCompleted(int level) => _stars.ContainsKey(level);

        public void Complete(int level, int stars) => _stars[level] = stars;

        public bool HasFailed(int level) => _failed.Contains(level);

        public void MarkFailed(int level) => _failed.Add(level);

        public void ClearStars() => _stars.Clear();
    }
}
