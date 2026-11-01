using System.Collections.Generic;
using System.Linq;
using Game.Services.Progress;

namespace Game.Services.Levels
{
    /// <summary>
    /// Default <see cref="ILevelCurveService"/>; curves come from the game config, the choice from the save.
    /// </summary>
    public sealed class LevelCurveService : ILevelCurveService
    {
        private readonly IReadOnlyList<LevelCurveConfig> _curves;
        private readonly IProgressService _progress;

        /// <summary>
        /// Creates the service.
        /// </summary>
        /// <param name="curves">All curves; the first one is the default.</param>
        /// <param name="progress">Holds the chosen curve id and the stars.</param>
        public LevelCurveService(IReadOnlyList<LevelCurveConfig> curves, IProgressService progress)
        {
            _curves = curves;
            _progress = progress;
        }

        /// <inheritdoc/>
        public LevelCurveConfig Active => _curves.FirstOrDefault(c => c.Id == _progress.Data.Curve) ?? _curves[0];

        /// <inheritdoc/>
        public IReadOnlyList<LevelCurveConfig> Curves => _curves;

        /// <inheritdoc/>
        public int Count => Active.Levels.Count;

        /// <inheritdoc/>
        public LevelDefinition Get(int level) => Active.Levels[level - 1];

        /// <inheritdoc/>
        public int NextLevel()
        {
            var next = 1;
            for (var i = 1; i <= Count; i++)
            {
                if (!_progress.Data.Stars.ContainsKey(i)) break;

                next = i + 1;
            }

            return System.Math.Min(next, Count);
        }

        /// <inheritdoc/>
        public void SwitchCurve(string id)
        {
            _progress.Data.Curve = id;
            _progress.Data.Stars.Clear();
            _progress.NotifyChanged();
        }
    }
}
