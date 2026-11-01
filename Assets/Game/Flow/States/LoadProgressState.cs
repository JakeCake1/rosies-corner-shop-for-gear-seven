using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Services.Audio;
using Game.Services.Progress;

namespace Game.Flow.States
{
    /// <summary>
    /// Loads the player profile before any screen needs it, applies audio settings and goes home.
    /// </summary>
    public sealed class LoadProgressState : IGameState
    {
        private readonly IProgressService _progress;
        private readonly IAudioService _audio;
        private readonly GameStateMachine _states;

        /// <summary>
        /// Creates the state.
        /// </summary>
        /// <param name="progress">Profile.</param>
        /// <param name="audio">Starts the music if enabled.</param>
        /// <param name="states">Moves on to Home.</param>
        public LoadProgressState(IProgressService progress, IAudioService audio, GameStateMachine states)
        {
            _progress = progress;
            _audio = audio;
            _states = states;
        }

        /// <inheritdoc/>
        public UniTask Enter(CancellationToken ct)
        {
            _progress.Load();
            _audio.ApplySettings();
            _states.Enter<HomeState>(ct).Forget();
            return UniTask.CompletedTask;
        }

        /// <inheritdoc/>
        public UniTask Exit(CancellationToken ct) => UniTask.CompletedTask;
    }
}
