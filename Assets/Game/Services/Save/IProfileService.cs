using System;
using UniRx;

namespace Game.Services.Save
{
    /// <summary>
    /// The player profile as a set of chunks (<see cref="IProfileChunk"/>), each owned by its own service: loads
    /// them at startup, saves a chunk after its owner reports a change, resets progress.
    /// </summary>
    public interface IProfileService
    {
        /// <summary>
        /// Emits after any chunk changed or after a reset; views that combine several chunks refresh on it.
        /// </summary>
        IObservable<Unit> Changed { get; }

        /// <summary>
        /// Loads every chunk; called once by the LoadProgress state. Chunks without stored data start with a new
        /// player's defaults, and the single-file profile of older builds is split into chunks.
        /// </summary>
        void Load();

        /// <summary>
        /// Requests a save of every chunk (app paused or losing focus).
        /// </summary>
        void SaveAll();

        /// <summary>
        /// Resets every chunk to a new player's defaults except those kept on reset (settings, reviewer choices),
        /// and saves them.
        /// </summary>
        void ResetProgress();
    }
}
