using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Game.Services.Assets;
using Game.Services.Audio;
using Game.Services.Content;
using Game.Services.Localization;
using UnityEngine;

namespace Game.Flow.States
{
    /// <summary>
    /// First state: sets the target frame rate, initializes DOTween and Addressables, Localization and loads the item catalog and the audio clips.
    /// </summary>
    public sealed class BootstrapState : IGameState
    {
        private const int TweenCapacity = 400;
        private const int SequenceCapacity = 250;
        // Android caps an unset frame rate (-1) at 30 FPS and ignores vSyncCount, so the rate is set explicitly.
        private const int TargetFrameRate = 60;

        private readonly IAssetProvider _assets;
        private readonly IContentService _content;
        private readonly IAudioService _audio;
        private readonly ILocalizationService _localization;
        private readonly GameStateMachine _states;

        /// <summary>
        /// Creates the state.
        /// </summary>
        /// <param name="assets">Addressables.</param>
        /// <param name="content">Item catalog.</param>
        /// <param name="audio">Loads the audio library.</param>
        /// <param name="localization">Player-facing text; every screen after this state reads it synchronously.</param>
        /// <param name="states">Moves on to LoadProgress.</param>
        public BootstrapState(IAssetProvider assets, IContentService content, IAudioService audio, ILocalizationService localization,
            GameStateMachine states)
        {
            _assets = assets;
            _content = content;
            _audio = audio;
            _localization = localization;
            _states = states;
        }

        /// <inheritdoc/>
        public async UniTask Enter(CancellationToken ct)
        {
            Application.targetFrameRate = TargetFrameRate;
            DOTween.Init(false, true).SetCapacity(TweenCapacity, SequenceCapacity);
            await _assets.InitializeAsync(ct);
            await UniTask.WhenAll(_content.LoadAsync(ct), _audio.LoadAsync(ct));
            await _localization.InitializeAsync(ct);
            _states.Enter<LoadProgressState>(ct).Forget();
        }

        /// <inheritdoc/>
        public UniTask Exit(CancellationToken ct) => UniTask.CompletedTask;
    }
}
