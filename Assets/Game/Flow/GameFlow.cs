using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Features.Gameplay;
using Game.Flow.States;
using Game.Services.Flow;

namespace Game.Flow
{
    /// <summary>
    /// <see cref="IGameFlow"/> on top of the state machine; asks for start boosters before levels that need them.
    /// </summary>
    public sealed class GameFlow : IGameFlow, IDisposable
    {
        private readonly GameStateMachine _states;
        private readonly GameplayRequest _request;
        private readonly StartBoostersPrompt _startBoosters;
        private readonly CancellationTokenSource _lifetime = new CancellationTokenSource();
        // Cached: a button handler running during shutdown must see cancellation, not ObjectDisposedException.
        private readonly CancellationToken _lifetimeToken;

        /// <summary>
        /// Creates the flow.
        /// </summary>
        /// <param name="states">App state machine.</param>
        /// <param name="request">Level to start, read by the gameplay scene.</param>
        /// <param name="startBoosters">Start-boosters popup.</param>
        public GameFlow(GameStateMachine states, GameplayRequest request, StartBoostersPrompt startBoosters)
        {
            _states = states;
            _request = request;
            _startBoosters = startBoosters;
            _lifetimeToken = _lifetime.Token;
        }

        /// <inheritdoc/>
        public UniTask GoHomeAsync() => _states.Enter<HomeState>(_lifetimeToken);

        /// <inheritdoc/>
        public async UniTask PlayLevelAsync(int level, StartBoosters startBoosters = null)
        {
            if (startBoosters == null && _startBoosters.IsNeeded(level))
            {
                startBoosters = await _startBoosters.AskAsync(level, _lifetimeToken);
                if (startBoosters == null)
                {
                    return;
                }
            }

            await _states.Enter<GameplayState>(_lifetimeToken, () =>
            {
                _request.Level = level;
                _request.StartBoosters = startBoosters ?? StartBoosters.None;
            });
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _lifetime.Cancel();
            _lifetime.Dispose();
        }
    }
}
