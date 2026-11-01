using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Features.Home;
using Game.Services.UI;
using Game.Services.UI.Dialogs;

namespace Game.Flow.States
{
    /// <summary>
    /// The shop front: loads the (empty) home scene so the gameplay scope is unloaded, then shows the home screen.
    /// </summary>
    public sealed class HomeState : IGameState
    {
        private readonly SceneLoader _scenes;
        private readonly HomeScreen _home;
        private readonly IDialogService _dialogs;
        private readonly IUIService _ui;
        private readonly IOverlayService _overlay;

        /// <summary>
        /// Creates the state.
        /// </summary>
        /// <param name="scenes">Scene loading.</param>
        /// <param name="home">Home screen.</param>
        /// <param name="dialogs">Closes leftover dialogs.</param>
        /// <param name="ui">Closes leftover screens.</param>
        /// <param name="overlay">Clears coach bubbles.</param>
        public HomeState(SceneLoader scenes, HomeScreen home, IDialogService dialogs, IUIService ui, IOverlayService overlay)
        {
            _scenes = scenes;
            _home = home;
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
            await _scenes.LoadAsync(SceneLoader.HomeScene, ct);
            await _home.ShowAsync(ct);
        }

        /// <inheritdoc/>
        public async UniTask Exit(CancellationToken ct)
        {
            await _home.HideAsync();
        }
    }
}
