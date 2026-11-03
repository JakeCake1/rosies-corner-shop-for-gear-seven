using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Flow;
using Game.Flow.States;
using VContainer.Unity;

namespace Game.Bootstrap.Composition
{
    /// <summary>
    /// Starts the app flow once the project scope is built.
    /// </summary>
    public sealed class GameEntryPoint : IStartable, IDisposable
    {
        private readonly GameStateMachine _states;
        private readonly CancellationTokenSource _cts = new();

        /// <summary>
        /// Creates the entry point.
        /// </summary>
        /// <param name="states">App state machine.</param>
        /// <param name="lifecycle">Resolved here so the app pause listener exists from the start.</param>
        public GameEntryPoint(GameStateMachine states, AppLifecycleListener lifecycle)
        {
            _states = states;
        }

        /// <inheritdoc/>
        /// <remarks>
        /// The token is cancelled when the project scope is disposed (Play mode exit, quit), so a transition that
        /// is still loading stops instead of continuing on a destroyed container.
        /// </remarks>
        public void Start() => _states.Enter<BootstrapState>(_cts.Token).Forget();

        /// <inheritdoc/>
        public void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();
        }
    }
}
