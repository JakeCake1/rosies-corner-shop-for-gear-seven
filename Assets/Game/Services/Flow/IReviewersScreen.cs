using System.Threading;
using Cysharp.Threading.Tasks;

namespace Game.Services.Flow
{
    /// <summary>
    /// Opens the "For reviewers" panel.
    /// </summary>
    public interface IReviewersScreen
    {
        /// <summary>
        /// Opens the panel.
        /// </summary>
        /// <param name="ct">Cancels waiting.</param>
        /// <returns>Completes when the panel closes.</returns>
        /// <exception cref="System.OperationCanceledException">Waiting was cancelled.</exception>
        UniTask OpenAsync(CancellationToken ct);
    }
}
