using System.Collections.Generic;

namespace Game.Services.Levels
{
    /// <summary>
    /// Completed levels with their best stars and levels lost at least once; owns the <c>levels</c> profile chunk.
    /// </summary>
    public interface ILevelProgressService
    {
        /// <summary>
        /// Best stars (1..3) per completed level number of the active curve.
        /// </summary>
        IReadOnlyDictionary<int, int> Stars { get; }

        /// <summary>
        /// Returns whether a level was completed at least once.
        /// </summary>
        /// <param name="level">Level number in the active curve.</param>
        /// <returns>True when the level has stars.</returns>
        bool IsCompleted(int level);

        /// <summary>
        /// Records a completed level, keeping the best stars.
        /// </summary>
        /// <param name="level">Level number in the active curve.</param>
        /// <param name="stars">Stars earned (1..3).</param>
        void Complete(int level, int stars);

        /// <summary>
        /// Returns whether the player has lost a level at least once (enables its hidden hint).
        /// </summary>
        /// <param name="level">Level number.</param>
        /// <returns>True after <see cref="MarkFailed"/> was called for it.</returns>
        bool HasFailed(int level);

        /// <summary>
        /// Remembers that a level was lost.
        /// </summary>
        /// <param name="level">Level number.</param>
        void MarkFailed(int level);

        /// <summary>
        /// Forgets all stars (switching the level curve, whose level numbers mean different levels).
        /// </summary>
        void ClearStars();
    }
}
