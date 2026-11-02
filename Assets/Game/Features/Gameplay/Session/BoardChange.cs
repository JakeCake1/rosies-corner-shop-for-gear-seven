using System.Collections.Generic;
using Game.Features.Gameplay.Model;

namespace Game.Features.Gameplay.Session
{
    /// <summary>
    /// Tells the board view that the level state changed and how to animate it.
    /// </summary>
    public sealed class BoardChange
    {
        private static readonly IReadOnlyList<LevelEvent> NoEvents = new LevelEvent[0];
        private static readonly IReadOnlyDictionary<int, int> NoMap = new Dictionary<int, int>();

        /// <summary>What caused the change.</summary>
        public BoardChangeKind Kind { get; }

        /// <summary>Rule events (folds, unlocks, arrivals...).</summary>
        public IReadOnlyList<LevelEvent> Events { get; }

        /// <summary>Items pulled out of a stockroom by Quick Pick, by jar id.</summary>
        public IReadOnlyDictionary<int, int> FromStock { get; }

        /// <summary>Items returned to a stockroom by Tidy Up, by jar id.</summary>
        public IReadOnlyDictionary<int, int> ToStock { get; }

        /// <summary>
        /// Creates a change notice.
        /// </summary>
        /// <param name="kind">What caused the change; picks animation timings.</param>
        /// <param name="events">Rule events to animate.</param>
        /// <param name="fromStock">Jar id → column for items pulled out of a stockroom (fly out of its label).</param>
        /// <param name="toStock">Jar id → column for items returned to a stockroom (fly into its label).</param>
        public BoardChange(BoardChangeKind kind, IReadOnlyList<LevelEvent> events = null,
            IReadOnlyDictionary<int, int> fromStock = null, IReadOnlyDictionary<int, int> toStock = null)
        {
            Kind = kind;
            Events = events ?? NoEvents;
            FromStock = fromStock ?? NoMap;
            ToStock = toStock ?? NoMap;
        }
    }
}
