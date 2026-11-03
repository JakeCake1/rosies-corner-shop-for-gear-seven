using System;

namespace Game.Features.Gameplay.Session
{
    /// <summary>
    /// Production seed source: derives the seed from the clock, like the prototype did.
    /// </summary>
    public sealed class ClockShuffleSeedSource : IShuffleSeedSource
    {
        private const long SeedRange = 100000;

        /// <inheritdoc/>
        public uint NextSeed() => (uint)(DateTime.UtcNow.Ticks % SeedRange);
    }
}
