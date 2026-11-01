using System.Threading;
using Cysharp.Threading.Tasks;

namespace Game.Flow
{
    /// <summary>
    /// A state of the app flow with explicit enter and exit.
    /// </summary>
    public interface IGameState
    {
        /// <summary>
        /// Runs when the state machine enters this state.
        /// </summary>
        /// <param name="ct">Cancelled on app shutdown.</param>
        /// <returns>Completes when the state is set up.</returns>
        /// <exception cref="System.OperationCanceledException">Entering was cancelled.</exception>
        UniTask Enter(CancellationToken ct);

        /// <summary>
        /// Runs before the state machine leaves this state.
        /// </summary>
        /// <param name="ct">Cancelled on app shutdown.</param>
        /// <returns>Completes when the state is torn down.</returns>
        /// <exception cref="System.OperationCanceledException">Exiting was cancelled.</exception>
        UniTask Exit(CancellationToken ct);
    }
}
