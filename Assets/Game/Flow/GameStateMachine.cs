using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;
using VContainer;

namespace Game.Flow
{
    /// <summary>
    /// App flow: Bootstrap → LoadProgress → Home ⇄ Gameplay. Transitions are serialized so a second request waits
    /// for the first to finish.
    /// </summary>
    public sealed class GameStateMachine : IDisposable
    {
        private readonly IObjectResolver _resolver;
        private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
        private readonly Subject<Func<UniTask>> _failed = new Subject<Func<UniTask>>();
        private IGameState _current;

        /// <summary>
        /// Emits when entering a state failed (not when it was cancelled), with a function that repeats the
        /// transition. The player has nothing on screen then, so a listener offers a retry.
        /// </summary>
        public IObservable<Func<UniTask>> Failed => _failed;

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
                try
                {
                    await next.Enter(ct);
                }
                catch (Exception e) when (!(e is OperationCanceledException))
                {
                    Debug.LogException(e);
                    await TryExitAsync(next);
                    _failed.OnNext(() => Enter<TState>(ct, prepare));
                    throw;
                }

                _current = next;
            }
            finally
            {
                _gate.Release();
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _failed.Dispose();
            _gate.Dispose();
        }

        // A half-entered state (e.g. the home screen shown, then the scene load failed) must hide what it showed,
        // otherwise the next state starts on top of it.
        private static async UniTask TryExitAsync(IGameState state)
        {
            try
            {
                await state.Exit(CancellationToken.None);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
