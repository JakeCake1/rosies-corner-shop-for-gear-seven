using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Game.Services.Save
{
    /// <summary>
    /// Reads and writes profile chunks: JSON with per-chunk versions and migrations, atomic writes with a backup,
    /// recovery from broken files. Used by <see cref="IProfileService"/> only; gameplay never touches files.
    /// </summary>
    public interface ISaveService
    {
        /// <summary>
        /// Loads a chunk from its main file, or from the backup when the main file is broken (the broken file is
        /// moved aside).
        /// </summary>
        /// <param name="chunk">Chunk to restore; left untouched when nothing usable is stored.</param>
        /// <returns>True when stored data was restored.</returns>
        /// <remarks>When a file exists but can't be read (I/O or permission error, unlike a missing file) the chunk
        /// stays read-only for the session: later saves are skipped so the stored data isn't replaced by defaults.</remarks>
        bool TryLoad(IProfileChunk chunk);

        /// <summary>
        /// Splits the single-file profile of older builds (<c>profile.json</c>) into chunks, writes them and
        /// deletes the old file once every chunk was written.
        /// </summary>
        /// <param name="chunks">All chunks; each is restored from the old profile.</param>
        /// <returns>True when an old profile was found and restored.</returns>
        bool TryMigrateLegacy(IReadOnlyList<IProfileChunk> chunks);

        /// <summary>
        /// Serializes on the calling (main) thread and writes on a background thread; overlapping saves of the
        /// same chunk are merged so only the latest data is written.
        /// </summary>
        /// <param name="chunk">Chunk to save.</param>
        /// <param name="ct">Cancelled on app shutdown.</param>
        /// <returns>Completes after the write (or after it was skipped because a newer one superseded it).</returns>
        /// <exception cref="System.OperationCanceledException">The token was cancelled before the write started.</exception>
        /// <remarks>Does nothing for a chunk whose stored data could not be read at startup (see <see cref="TryLoad"/>).</remarks>
        UniTask SaveAsync(IProfileChunk chunk, CancellationToken ct);

        /// <summary>
        /// Serializes and writes on the calling thread, and returns after the data is on disk; replaces any queued
        /// write of the chunk. For app pause and exit, where the OS may stop the process before a background write
        /// finishes.
        /// </summary>
        /// <param name="chunk">Chunk to save.</param>
        /// <remarks>Blocks while another write is in progress. Write failures are logged, not thrown.</remarks>
        void Save(IProfileChunk chunk);
    }
}
