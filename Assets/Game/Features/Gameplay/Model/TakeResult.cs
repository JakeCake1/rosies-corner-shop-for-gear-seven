using System.Collections.Generic;

namespace Game.Features.Gameplay.Model
{
    /// <summary>
    /// Outcome of tapping an item (see <see cref="LevelRules.Take"/>).
    /// </summary>
    public sealed class TakeResult
    {
        private static readonly IReadOnlyList<LevelEvent> NoEvents = new LevelEvent[0];

        /// <summary>Events to animate: unwraps, folds, unlocks, arrivals, unboarding.</summary>
        public IReadOnlyList<LevelEvent> Events { get; }

        /// <summary>The moved item (a fresh object without bag or second row) or the hammer.</summary>
        public Jar Jar { get; }

        /// <summary>The basket that received the item; null when it went to the counter or for the hammer.</summary>
        public Basket Basket { get; }

        /// <summary>Whether the move happened; a refused move changes nothing.</summary>
        public bool IsOk { get; }

        /// <summary>Refused because the counter is full and no basket wants the item; the counter shakes.</summary>
        public bool IsCounterFull { get; }

        /// <summary>The hammer was used on the nailed shelf.</summary>
        public bool IsTool { get; }

        /// <summary>
        /// Creates a result.
        /// </summary>
        /// <param name="isOk">Whether the move happened.</param>
        /// <param name="isCounterFull">Refused because no basket wants the item and the counter is full.</param>
        /// <param name="jar">The item as it now sits in a basket or on the counter (or the hammer).</param>
        /// <param name="basket">The basket that received the item; null when it went to the counter.</param>
        /// <param name="isTool">The hammer was used.</param>
        /// <param name="events">What happened, for animation.</param>
        public TakeResult(bool isOk, bool isCounterFull, Jar jar, Basket basket, bool isTool,
            IReadOnlyList<LevelEvent> events)
        {
            IsOk = isOk;
            IsCounterFull = isCounterFull;
            Jar = jar;
            Basket = basket;
            IsTool = isTool;
            Events = events ?? NoEvents;
        }

        /// <summary>
        /// A refused move.
        /// </summary>
        /// <param name="isCounterFull">True when refused because of a full counter.</param>
        /// <returns>The result.</returns>
        public static TakeResult Rejected(bool isCounterFull) =>
            new TakeResult(false, isCounterFull, null, null, false, null);
    }
}
