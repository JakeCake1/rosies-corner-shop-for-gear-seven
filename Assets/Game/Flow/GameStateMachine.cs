using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using VContainer;

namespace Game.Flow
{
    /// <summary>
    /// App flow: Bootstrap → LoadProgress → Home ⇄ Gameplay. Transitions are serialized so a second request waits
    /// for the first to finish.
    /// </summary>
    public sealed class GameStateMachine
    {
        private readonly IObjectResolver _resolver;
        private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
        private IGameState _current;

        /// <summary>
        /// Creates the state machine.
        /// </summary>
        /// <param name="resolver">Resolves state instances; allowed here as flow infrastructure.</param>
        public GameStateMachine(IObjectResolver resolver)
        {
            _resolver = resolver;
        }

        /// <summary>
        /// Exits the current state and enters <typeparamref name="TState"/> (re-entering the same state reloads it).
        /// </summary>
        /// <typeparam name="TState">Target state, registered in the project scope.</typeparam>
        /// <param name="ct">Cancelled on app shutdown.</param>
        /// <param name="prepare">Runs under the transition lock right before the state enters (e.g. sets its request).</param>
        /// <returns>Completes when the new state has entered.</returns>
        /// <exception cref="System.OperationCanceledException">The transition was cancelled.</exception>
        public async UniTask Enter<TState>(CancellationToken ct, Action prepare = null) where TState : IGameState
        {
            await _gate.WaitAsync(ct);
            try
            {
                if (_current != null)
                {
                    var previous = _current;
                    _current = null;
                    await previous.Exit(ct);
                }

                prepare?.Invoke();
                // Published only after Enter succeeded: a failed state is never exited and the next Enter can retry.
                var next = _resolver.Resolve<TState>();
                await next.Enter(ct);
                _current = next;
            }
            finally
            {
                _gate.Release();
            }
        }
    }
}
