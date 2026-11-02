using System;
using System.Collections.Generic;
using Game.Services.Save;

namespace Game.Services.Levels
{
    /// <summary>
    /// Default <see cref="ILevelProgressService"/>.
    /// </summary>
    public sealed class LevelProgressService : ProfileChunk<LevelProgressData>, ILevelProgressService
    {
        /// <inheritdoc/>
        public override string Key => "levels";

        /// <inheritdoc/>
        public IReadOnlyDictionary<int, int> Stars => Data.Stars;

        /// <inheritdoc/>
        public bool IsCompleted(int level) => Data.Stars.ContainsKey(level);

        /// <inheritdoc/>
        public void Complete(int level, int stars)
        {
            Data.Stars[level] = Math.Max(stars, Data.Stars.TryGetValue(level, out var previous) ? previous : 0);
            MarkChanged();
        }

        /// <inheritdoc/>
        public bool HasFailed(int level) => Data.FailedLevels.Contains(level);

        /// <inheritdoc/>
        public void MarkFailed(int level)
        {
            if (Data.FailedLevels.Add(level))
            {
                MarkChanged();
            }
        }

        /// <inheritdoc/>
        public void ClearStars()
        {
            Data.Stars.Clear();
            MarkChanged();
        }
    }
}
