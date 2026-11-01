using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Services.UI;
using Game.Services.UI.Dialogs;

namespace Game.Flow.States
{
    /// <summary>
    /// A level in play: loads the gameplay scene, whose scope starts the requested level.
    /// </summary>
    public sealed class GameplayState : IGameState
    {
        private readonly SceneLoader _scenes;
        private readonly IDialogService _dialogs;
        private readonly IUIService _ui;
        private readonly IOverlayService _overlay;

        /// <summary>
        /// Creates the state.
        /// </summary>
        /// <param name="scenes">Scene loading.</param>
        /// <param name="dialogs">Closes leftover dialogs.</param>
        /// <param name="ui">Closes the album or other screens.</param>
        /// <param name="overlay">Clears coach bubbles.</param>
        public GameplayState(SceneLoader scenes, IDialogService dialogs, IUIService ui, IOverlayService overlay)
        {
            _scenes = scenes;
            _dialogs = dialogs;
            _ui = ui;
            _overlay = overlay;
        }

        /// <inheritdoc/>
        public async UniTask Enter(CancellationToken ct)
        {
            _overlay.ClearCoach();
            await _dialogs.CloseAllAsync();
            await _ui.CloseAllAsync(UILayer.Screens);
            await _scenes.LoadAsync(SceneLoader.GameplayScene, ct);
        }

        /// <inheritdoc/>
        public UniTask Exit(CancellationToken ct) => UniTask.CompletedTask;
    }
}
