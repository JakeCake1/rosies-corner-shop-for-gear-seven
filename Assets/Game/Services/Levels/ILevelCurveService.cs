using System.Collections.Generic;

namespace Game.Services.Levels
{
    /// <summary>
    /// The active level curve and progression rules that depend on it (next level, milestones).
    /// </summary>
    public interface ILevelCurveService
    {
        /// <summary>
        /// The curve chosen in the save (the reviewers panel switches it).
        /// </summary>
        LevelCurveConfig Active { get; }

        /// <summary>
        /// All available curves, for the reviewers panel.
        /// </summary>
        IReadOnlyList<LevelCurveConfig> Curves { get; }

        /// <summary>
        /// Number of levels in the active curve.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Returns a level of the active curve.
        /// </summary>
        /// <param name="level">1-based level number.</param>
        /// <returns>The level definition.</returns>
        LevelDefinition Get(int level);

        /// <summary>
        /// The level "Open the shop" starts: the first level without stars, capped at the last level.
        /// </summary>
        /// <returns>1-based level number.</returns>
        int NextLevel();

        /// <summary>
        /// Switches the curve; stars are reset because level numbers mean different levels in each curve.
        /// </summary>
        /// <param name="id">Curve id ("new" or "orig").</param>
        void SwitchCurve(string id);
    }
}
