using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Services.Content;
using Game.Services.Flow;
using Game.Services.Levels;
using Game.Services.Progress;
using Game.Services.UI;
using UniRx;

namespace Game.Features.Home
{
    /// <summary>
    /// Opens the home screen and handles its commands and milestones (album intro after level 9, naming the shop
    /// after level 10). Called by the Home flow state.
    /// </summary>
    public sealed class HomeScreen : IDisposable
    {
        private const int MilestoneDelayMs = 700;
        private const int AlbumFromLevel = 9;
        private const int NameShopAfterLevel = 10;

        private readonly IUIService _ui;
        private readonly IOverlayService _overlay;
        private readonly IProgressService _progress;
        private readonly ILevelCurveService _curves;
        private readonly IContentService _content;
        private readonly IGameFlow _flow;
        private readonly ISettingsScreen _settings;
        private readonly IShopNamePrompt _namePrompt;
        private readonly IAlbumScreen _album;
        private readonly IReviewersScreen _reviewers;
        private readonly CompositeDisposable _subscriptions = new CompositeDisposable();
        private HomeWindow _window;

        /// <summary>
        /// Creates the screen controller.
        /// </summary>
        /// <param name="ui">Windows.</param>
        /// <param name="overlay">Background and toasts.</param>
        /// <param name="progress">Progress.</param>
        /// <param name="curves">Next level.</param>
        /// <param name="content">Catalog.</param>
        /// <param name="flow">Starts levels.</param>
        /// <param name="settings">Settings dialog.</param>
        /// <param name="namePrompt">Shop naming.</param>
        /// <param name="album">Album screen.</param>
        /// <param name="reviewers">Reviewers panel.</param>
        public HomeScreen(IUIService ui, IOverlayService overlay, IProgressService progress, ILevelCurveService curves,
            IContentService content, IGameFlow flow, ISettingsScreen settings, IShopNamePrompt namePrompt,
            IAlbumScreen album, IReviewersScreen reviewers)
        {
            _ui = ui;
            _overlay = overlay;
            _progress = progress;
            _curves = curves;
            _content = content;
            _flow = flow;
            _settings = settings;
            _namePrompt = namePrompt;
            _album = album;
            _reviewers = reviewers;
        }

        /// <summary>
        /// Shows the home screen and schedules milestone popups.
        /// </summary>
        /// <param name="ct">Cancelled when the Home state exits.</param>
        /// <returns>Completes when the screen is shown.</returns>
        /// <exception cref="OperationCanceledException">Opening was cancelled.</exception>
        public async UniTask ShowAsync(CancellationToken ct)
        {
            _overlay.SetBackground(false, true);
            _subscriptions.Clear();
            var vm = new HomeViewModel(_progress, _curves, _content);
            vm.Commands.Subscribe(OnCommand).AddTo(_subscriptions);
            _window = await _ui.OpenAsync<HomeWindow, HomeViewModel>(vm, ct);
            ShowMilestonesAsync(ct).Forget();
        }

        /// <summary>
        /// Closes the home screen (entering a level).
        /// </summary>
        /// <returns>Completes when closed.</returns>
        public async UniTask HideAsync()
        {
            _subscriptions.Clear();
            if (_window != null) await _ui.CloseAsync(_window);
            _window = null;
        }

        /// <inheritdoc/>
        public void Dispose() => _subscriptions.Dispose();

        private void OnCommand(HomeCommand command)
        {
            if (_ui.IsPopupOpen) return;

            switch (command)
            {
                case HomeCommand.Play:
                    _flow.PlayLevelAsync(_curves.NextLevel()).Forget();
                    break;
                case HomeCommand.Album:
                    _album.OpenAsync(CancellationToken.None).Forget();
                    break;
                case HomeCommand.AlbumLocked:
                    _overlay.Toast($"The album opens after level {AlbumFromLevel}. Keep going!");
                    break;
                case HomeCommand.Settings:
                    _settings.OpenAsync(null).Forget();
                    break;
                case HomeCommand.Reviewers:
                    _reviewers.OpenAsync(CancellationToken.None).Forget();
                    break;
            }
        }

        private async UniTaskVoid ShowMilestonesAsync(CancellationToken ct)
        {
            if (_curves.Active.IsOriginal) return;

            var stars = _progress.Data.Stars;
            var album = stars.ContainsKey(AlbumFromLevel) && !_progress.HasFlag(Flags.AlbumIntro);
            var name = stars.ContainsKey(NameShopAfterLevel) && !_progress.HasFlag(Flags.NameAsked);
            if (!album && !name) return;

            await UniTask.Delay(MilestoneDelayMs, cancellationToken: ct);
            if (_window == null || _ui.IsPopupOpen) return;

            if (album) await _album.IntroAsync(ct);
            else await _namePrompt.AskAsync(ct);
        }
    }
}
