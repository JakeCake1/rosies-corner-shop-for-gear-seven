using System;
using Newtonsoft.Json.Linq;
using UniRx;

namespace Game.Services.Save
{
    /// <summary>
    /// One independently saved part of the player profile (wallet, levels, album, ...), owned by the service
    /// responsible for that data. <see cref="IProfileService"/> loads every registered chunk, saves a chunk after
    /// it reports a change and resets them all on "reset progress".
    /// </summary>
    /// <remarks>
    /// Derive from <see cref="ProfileChunk{TData}"/> instead of implementing this directly. Register the owner in
    /// the project scope with <c>AsImplementedInterfaces()</c> so the profile service receives it.
    /// </remarks>
    public interface IProfileChunk
    {
        /// <summary>
        /// Unique, stable storage key ("wallet"); the chunk is stored as <c>profile_&lt;key&gt;.json</c>. Never
        /// rename it: that orphans existing saves.
        /// </summary>
        string Key { get; }

        /// <summary>
        /// Format version of the chunk data; bump it and handle the old version in <see cref="Migrate"/> whenever
        /// the data structure changes.
        /// </summary>
        int Version { get; }

        /// <summary>
        /// Whether "reset progress" leaves the chunk as it is (settings, reviewer choices).
        /// </summary>
        bool IsKeptOnReset { get; }

        /// <summary>
        /// Type of the plain JSON data object returned by <see cref="Data"/>.
        /// </summary>
        Type DataType { get; }

        /// <summary>
        /// The live data object; serialized on the main thread right after a change.
        /// </summary>
        object Data { get; }

        /// <summary>
        /// Emits after the owner changed the data; the profile service then saves the chunk.
        /// </summary>
        IObservable<Unit> Changed { get; }

        /// <summary>
        /// Replaces the data with loaded data, or with a new player's defaults.
        /// </summary>
        /// <param name="data">An instance of <see cref="DataType"/>; null means a new player.</param>
        /// <remarks>Doesn't emit <see cref="Changed"/>: whoever restores decides whether to save.</remarks>
        void Restore(object data);

        /// <summary>
        /// Upgrades raw stored data from an older <see cref="Version"/> in place, before it is deserialized.
        /// </summary>
        /// <param name="data">Stored fields of the chunk; works even for fields the data type no longer has.</param>
        /// <param name="fromVersion">Version the data was written with; always lower than <see cref="Version"/>.</param>
        void Migrate(JObject data, int fromVersion);
    }
}
