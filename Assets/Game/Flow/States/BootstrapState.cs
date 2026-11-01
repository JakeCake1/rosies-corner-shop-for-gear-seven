using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Game.Services.Assets;
using Game.Services.Content;
using Game.Services.Localization;

namespace Game.Flow.States
{
    /// <summary>
    /// First state: initializes DOTween, Addressables and Unity Localization (string tables) and loads the item
    /// catalog.
    /// </summary>
    public sealed class BootstrapState : IGameState
    {
        private const int TweenCapacity = 400;
        private const int SequenceCapacity = 250;

        private readonly IAssetProvider _assets;
        private readonly IContentService _content;
        private readonly ILocalizationService _localization;
        private readonly GameStateMachine _states;

        /// <summary>
        /// Creates the state.
        /// </summary>
        /// <param name="assets">Addressables.</param>
        /// <param name="content">Item catalog.</param>
        /// <param name="localization">Player-facing text; every screen after this state reads it synchronously.</param>
        /// <param name="states">Moves on to LoadProgress.</param>
        public BootstrapState(IAssetProvider assets, IContentService content, ILocalizationService localization,
            GameStateMachine states)
        {
            _assets = assets;
            _content = content;
            _localization = localization;
            _states = states;
        }

        /// <inheritdoc/>
        public async UniTask Enter(CancellationToken ct)
        {
            DOTween.Init(false, true).SetCapacity(TweenCapacity, SequenceCapacity);
            await _assets.InitializeAsync(ct);
            await _content.LoadAsync(ct);
            await _localization.InitializeAsync(ct);
            _states.Enter<LoadProgressState>(ct).Forget();
        }

        /// <inheritdoc/>
        public UniTask Exit(CancellationToken ct) => UniTask.CompletedTask;
    }
}
