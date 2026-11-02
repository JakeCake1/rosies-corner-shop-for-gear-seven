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
        /// <param name="ct">The home screen's showing; cancelling it closes the panel.</param>
        /// <returns>Completes when the panel closes.</returns>
        /// <exception cref="System.OperationCanceledException"><paramref name="ct"/> was cancelled.</exception>
        UniTask OpenAsync(CancellationToken ct);
    }
}
