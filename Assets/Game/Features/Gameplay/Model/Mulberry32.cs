using System.Collections.Generic;

namespace Game.Features.Gameplay.Model
{
    /// <summary>
    /// Deterministic 32-bit PRNG identical to the prototype's <c>mulberry32</c>, so a level seed produces the same
    /// shelves, orders and mechanics as in the HTML version.
    /// </summary>
    /// <remarks>
    /// All arithmetic is unsigned 32-bit and mirrors the JavaScript bit operations exactly; do not "simplify" it.
    /// </remarks>
    public sealed class Mulberry32
    {
        private const uint Increment = 0x6D2B79F5;
        private const double TwoPow32 = 4294967296.0;

        private uint _state;

        /// <summary>
        /// Creates a generator; the same seed always yields the same sequence.
        /// </summary>
        /// <param name="seed">Seed, reduced to 32 bits like JavaScript's <c>&gt;&gt;&gt; 0</c>.</param>
        public Mulberry32(uint seed)
        {
            _state = seed;
        }

        /// <summary>
        /// Returns the next value in [0, 1).
        /// </summary>
        /// <returns>A uniformly distributed double, never 1.</returns>
        public double Next()
        {
            unchecked
            {
                _state += Increment;
                var t = _state;
                t = (t ^ (t >> 15)) * (1u | t);
                t = (t + (t ^ (t >> 7)) * (61u | t)) ^ t;
                return (t ^ (t >> 14)) / TwoPow32;
            }
        }

        /// <summary>
        /// Shuffles <paramref name="list"/> in place with the prototype's Fisher-Yates order (<c>shuffleIn</c>).
        /// </summary>
        /// <typeparam name="T">Element type.</typeparam>
        /// <param name="list">List to shuffle; consumes <c>Count - 1</c> random values.</param>
        /// <returns>The same list, for chaining.</returns>
        public List<T> Shuffle<T>(List<T> list)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = (int)(Next() * (i + 1));
                (list[i], list[j]) = (list[j], list[i]);
            }

            return list;
        }

        /// <summary>
        /// Returns a value in [0, <paramref name="count"/>) like <c>Math.floor(r() * count)</c>.
        /// </summary>
        /// <param name="count">Exclusive upper bound; must be positive.</param>
        /// <returns>The chosen index.</returns>
        public int NextIndex(int count) => (int)(Next() * count);

        /// <summary>
        /// Draws a single value from a fresh generator seeded with <paramref name="seed"/> (the prototype's
        /// one-shot <c>mulberry32(seed)()</c> pattern).
        /// </summary>
        /// <param name="seed">32-bit seed.</param>
        /// <returns>The first value of that generator.</returns>
        public static double First(uint seed) => new Mulberry32(seed).Next();
    }
}
