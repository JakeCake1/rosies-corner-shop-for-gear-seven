using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Services.Audio;
using Game.Services.Save;

namespace Game.Flow.States
{
    /// <summary>
    /// Loads the player profile before any screen needs it, applies audio settings and goes home.
    /// </summary>
    public sealed class LoadProgressState : IGameState
    {
        private readonly IProfileService _profile;
        private readonly IAudioService _audio;
        private readonly GameStateMachine _states;

        /// <summary>
        /// Creates the state.
        /// </summary>
        /// <param name="profile">Loads every profile chunk.</param>
        /// <param name="audio">Starts the music if enabled.</param>
        /// <param name="states">Moves on to Home.</param>
        public LoadProgressState(IProfileService profile, IAudioService audio, GameStateMachine states)
        {
            _profile = profile;
            _audio = audio;
            _states = states;
        }

        /// <inheritdoc/>
        public UniTask Enter(CancellationToken ct)
        {
            _profile.Load();
            _audio.ApplySettings();
            _states.Enter<HomeState>(ct).Forget();
            return UniTask.CompletedTask;
        }

        /// <inheritdoc/>
        public UniTask Exit(CancellationToken ct) => UniTask.CompletedTask;
    }
}
