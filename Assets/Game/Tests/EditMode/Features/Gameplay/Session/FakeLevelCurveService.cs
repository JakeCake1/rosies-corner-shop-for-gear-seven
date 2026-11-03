using System.Collections.Generic;
using Game.Services.Levels;

namespace Game.Tests.EditMode.Features.Gameplay.Session
{
    /// <summary>
    /// Serves one level definition for every level number and a curve config built in the test.
    /// </summary>
    internal sealed class FakeLevelCurveService : ILevelCurveService
    {
        private readonly LevelDefinition _definition;

        public FakeLevelCurveService(LevelCurveConfig active, LevelDefinition definition)
        {
            Active = active;
            _definition = definition;
        }

        public LevelCurveConfig Active { get; }

        public IReadOnlyList<LevelCurveConfig> Curves => new[] { Active };

        public int Count => 1;

        public LevelDefinition Get(int level) => _definition;

        public int NextLevel() => 1;

        public void SwitchCurve(string id)
        {
        }
    }
}
