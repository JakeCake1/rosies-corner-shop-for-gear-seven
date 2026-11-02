using System.Collections.Generic;
using System.Linq;
using Game.Services.Experiments;

namespace Game.Services.Levels
{
    /// <summary>
    /// Default <see cref="ILevelCurveService"/>; curves come from the game config, the choice from the save.
    /// </summary>
    public sealed class LevelCurveService : ILevelCurveService
    {
        private readonly IReadOnlyList<LevelCurveConfig> _curves;
        private readonly IExperimentsService _experiments;
        private readonly ILevelProgressService _levels;

        /// <inheritdoc/>
        public LevelCurveConfig Active => _curves.FirstOrDefault(c => c.Id == _experiments.Curve) ?? _curves[0];

        /// <inheritdoc/>
        public IReadOnlyList<LevelCurveConfig> Curves => _curves;

        /// <inheritdoc/>
        public int Count => Active.Levels.Count;

        /// <summary>
        /// Creates the service.
        /// </summary>
        /// <param name="curves">All curves; the first one is the default.</param>
        /// <param name="experiments">Holds the chosen curve id.</param>
        /// <param name="levels">Completed levels.</param>
        public LevelCurveService(IReadOnlyList<LevelCurveConfig> curves, IExperimentsService experiments,
            ILevelProgressService levels)
        {
            _curves = curves;
            _experiments = experiments;
            _levels = levels;
        }

        /// <inheritdoc/>
        public LevelDefinition Get(int level) => Active.Levels[level - 1];

        /// <inheritdoc/>
        public int NextLevel()
        {
            var next = 1;
            for (var i = 1; i <= Count; i++)
            {
                if (!_levels.IsCompleted(i))
                {
                    break;
                }

                next = i + 1;
            }

            return System.Math.Min(next, Count);
        }

        /// <inheritdoc/>
        public void SwitchCurve(string id)
        {
            _experiments.SetCurve(id);
            _levels.ClearStars();
        }
    }
}
