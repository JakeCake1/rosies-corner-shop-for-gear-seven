using System;
using UniRx;

namespace Game.Features.Gameplay.Boosters
{
    /// <summary>
    /// Boosters the player owns (in-level and start boosters) and which unlock gifts were given; owns the
    /// <c>boosters</c> profile chunk.
    /// </summary>
    public interface IBoosterInventory
    {
        /// <summary>
        /// Emits after any count or gift changed; booster buttons refresh on it.
        /// </summary>
        IObservable<Unit> Changed { get; }

        /// <summary>
        /// Returns how many of an in-level booster the player owns.
        /// </summary>
        /// <param name="id">"pick", "tidy" or "shuffle".</param>
        /// <returns>Owned count; zero when never owned.</returns>
        int Get(string id);

        /// <summary>
        /// Changes an owned in-level booster count.
        /// </summary>
        /// <param name="id">Booster id.</param>
        /// <param name="delta">Change; the count never goes below zero.</param>
        void Add(string id, int delta);

        /// <summary>
        /// Returns how many of a start booster the player owns.
        /// </summary>
        /// <param name="id">"basket" or "quick".</param>
        /// <returns>Owned count; zero when never owned.</returns>
        int GetStart(string id);

        /// <summary>
        /// Changes an owned start booster count.
        /// </summary>
        /// <param name="id">Start booster id.</param>
        /// <param name="delta">Change; the count never goes below zero.</param>
        void AddStart(string id, int delta);

        /// <summary>
        /// Returns whether the unlock gift of a booster was given.
        /// </summary>
        /// <param name="id">Booster id.</param>
        /// <returns>True once <see cref="MarkGifted"/> was called for it.</returns>
        bool IsGifted(string id);

        /// <summary>
        /// Remembers that the unlock gift of a booster was given.
        /// </summary>
        /// <param name="id">Booster id.</param>
        void MarkGifted(string id);
    }
}
