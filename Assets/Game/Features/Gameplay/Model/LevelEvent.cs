namespace Game.Features.Gameplay.Model
{
    /// <summary>
    /// Something visible that happened while the rules ran; the board view turns events into animations and sounds.
    /// </summary>
    public readonly struct LevelEvent
    {
        /// <summary>
        /// Creates an event.
        /// </summary>
        /// <param name="type">What happened.</param>
        /// <param name="id">Main subject id: shelf, jar or basket id depending on <paramref name="type"/>.</param>
        /// <param name="other">Secondary value: column index, source shelf id or basket slot.</param>
        public LevelEvent(LevelEventType type, int id, int other)
        {
            Type = type;
            Id = id;
            Other = other;
        }

        /// <summary>What happened.</summary>
        public LevelEventType Type { get; }

        /// <summary>
        /// Main subject: the shelf (arrived, folded, unlocked, unboarded, thawed), the jar (unwrapped) or the basket.
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// Column for arrivals and folds, the key/hammer source for unlocks, the slot for new baskets.
        /// </summary>
        public int Other { get; }

        /// <summary>
        /// Shorthand constructor.
        /// </summary>
        /// <param name="type">What happened.</param>
        /// <param name="id">Main subject id.</param>
        /// <param name="other">Secondary value; see <see cref="Other"/>.</param>
        /// <returns>The event.</returns>
        public static LevelEvent Of(LevelEventType type, int id, int other = 0) => new LevelEvent(type, id, other);
    }
}
