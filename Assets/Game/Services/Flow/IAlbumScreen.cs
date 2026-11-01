using System.Threading;
using Cysharp.Threading.Tasks;

namespace Game.Services.Flow
{
    /// <summary>
    /// Opens the album screen and its one-time introduction.
    /// </summary>
    public interface IAlbumScreen
    {
        /// <summary>
        /// Opens the album and clears the "new items" badge.
        /// </summary>
        /// <param name="ct">Cancels opening.</param>
        /// <returns>Completes when the album is shown.</returns>
        /// <exception cref="System.OperationCanceledException">Opening was cancelled.</exception>
        UniTask OpenAsync(CancellationToken ct);

        /// <summary>
        /// Shows "New: the Album!" once after level 9 is cleared.
        /// </summary>
        /// <param name="ct">Cancels waiting.</param>
        /// <returns>Completes when the intro (and the album, if opened from it) is shown.</returns>
        /// <exception cref="System.OperationCanceledException">Waiting was cancelled.</exception>
        UniTask IntroAsync(CancellationToken ct);
    }
}
