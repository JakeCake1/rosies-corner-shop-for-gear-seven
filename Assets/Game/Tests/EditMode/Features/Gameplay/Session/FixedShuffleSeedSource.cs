using Game.Features.Gameplay.Session;

namespace Game.Tests.EditMode.Features.Gameplay.Session
{
    /// <summary>
    /// Returns a fixed seed and counts how often the session asked for one.
    /// </summary>
    internal sealed class FixedShuffleSeedSource : IShuffleSeedSource
    {
        private readonly uint _seed;

        public FixedShuffleSeedSource(uint seed)
        {
            _seed = seed;
        }

        public int Requests { get; private set; }

        public uint NextSeed()
        {
            Requests++;
            return _seed;
        }
    }
}
