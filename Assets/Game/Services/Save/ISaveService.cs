using System.Threading;
using Cysharp.Threading.Tasks;

namespace Game.Services.Save
{
    /// <summary>
    /// Reads and writes the player profile. Used by the progress service only; gameplay never touches files.
    /// </summary>
    public interface ISaveService
    {
        /// <summary>
        /// Loads the profile at startup (LoadProgress state): main file, then backup, then a fresh profile.
        /// </summary>
        /// <returns>The loaded or new profile; never null.</returns>
        PlayerProfileData Load();

        /// <summary>
        /// Serializes on the calling (main) thread and writes on a background thread; overlapping calls are
        /// merged so only the latest data is written.
        /// </summary>
        /// <param name="data">Profile to save.</param>
        /// <param name="ct">Cancelled on app shutdown.</param>
        /// <returns>Completes after the write (or after it was skipped because a newer one superseded it).</returns>
        /// <exception cref="System.OperationCanceledException">The token was cancelled while waiting.</exception>
        UniTask SaveAsync(PlayerProfileData data, CancellationToken ct);

        /// <summary>
        /// Deletes the profile, its backup and temp file (reset progress).
        /// </summary>
        void Delete();
    }
}
