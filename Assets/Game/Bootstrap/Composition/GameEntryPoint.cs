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
    public sealed class GameEntryPoint : IStartable
    {
        private readonly GameStateMachine _states;

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
        public void Start() => _states.Enter<BootstrapState>(CancellationToken.None).Forget();
    }
}
