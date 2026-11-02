using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Services.Balance;
using Game.Services.Content;
using Game.Services.Flow;
using Game.Services.Levels;
using Game.Services.Localization;
using Game.Services.Onboarding;
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

        private readonly IUIService _ui;
        private readonly IOverlayService _overlay;
        private readonly HomeProgress _progress;
        private readonly ILevelCurveService _curves;
        private readonly IContentService _content;
        private readonly IGameFlow _flow;
        private readonly ISettingsScreen _settings;
        private readonly IShopNamePrompt _namePrompt;
        private readonly IAlbumScreen _album;
        private readonly IReviewersScreen _reviewers;
        private readonly ILocalizationService _texts;
        private readonly GameBalanceConfig _balance;
        private readonly CompositeDisposable _subscriptions = new CompositeDisposable();
        private HomeWindow _window;
        private CancellationTokenSource _shown;

        /// <summary>
        /// Creates the screen controller.
        /// </summary>
        /// <param name="ui">Windows.</param>
        /// <param name="overlay">Background and toasts.</param>
        /// <param name="progress">Profile chunks the home screen shows.</param>
        /// <param name="curves">Next level.</param>
        /// <param name="content">Catalog.</param>
        /// <param name="flow">Starts levels.</param>
        /// <param name="settings">Settings dialog.</param>
        /// <param name="namePrompt">Shop naming.</param>
        /// <param name="album">Album screen.</param>
        /// <param name="reviewers">Reviewers panel.</param>
        /// <param name="texts">Home texts.</param>
        /// <param name="balance">Album and shop naming milestones.</param>
        public HomeScreen(IUIService ui, IOverlayService overlay, HomeProgress progress, ILevelCurveService curves,
            IContentService content, IGameFlow flow, ISettingsScreen settings, IShopNamePrompt namePrompt,
            IAlbumScreen album, IReviewersScreen reviewers, ILocalizationService texts, GameBalanceConfig balance)
        {
            _texts = texts;
            _balance = balance;
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
        /// <param name="ct">Cancelled with the game flow (app shutdown).</param>
        /// <returns>Completes when the screen is shown.</returns>
        /// <exception cref="OperationCanceledException">Opening was cancelled.</exception>
        /// <remarks>
        /// Everything started from the screen (milestone popups, album, settings, reviewers) is bound to this
        /// showing and is cancelled and closed by <see cref="HideAsync"/>.
        /// </remarks>
        public async UniTask ShowAsync(CancellationToken ct)
        {
            CancelShown();
            _shown = CancellationTokenSource.CreateLinkedTokenSource(ct);
            var shown = _shown.Token;
            _overlay.SetBackground(false, true);
            _subscriptions.Clear();
            // The showcase may show any item, so home loads every item sprite (cached after the first time).
            await _content.LoadAllItemSpritesAsync(shown);
            var vm = new HomeViewModel(_progress, _curves, _content, _texts, _balance);
            vm.Commands.Subscribe(command => OnCommand(command, shown)).AddTo(_subscriptions);
            _window = await _ui.OpenAsync<HomeWindow, HomeViewModel>(vm, shown);
            ShowMilestonesAsync(shown).Forget();
        }

        /// <summary>
        /// Closes the home screen (entering a level).
        /// </summary>
        /// <returns>Completes when closed.</returns>
        public async UniTask HideAsync()
        {
            _subscriptions.Clear();
            var window = _window;
            _window = null;
            // The close starts before the cancel so the window plays its fade; the cancel then stops pending
            // milestones and closes popups opened from the home screen.
            var closing = window != null ? _ui.CloseAsync(window) : UniTask.CompletedTask;
            CancelShown();
            await closing;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            CancelShown();
            _subscriptions.Dispose();
        }

        private void CancelShown()
        {
            if (_shown == null)
            {
                return;
            }

            _shown.Cancel();
            _shown.Dispose();
            _shown = null;
        }

        private void OnCommand(HomeCommand command, CancellationToken shown)
        {
            if (_ui.IsPopupOpen)
            {
                return;
            }

            switch (command)
            {
                case HomeCommand.Play:
                    _flow.PlayLevelAsync(_curves.NextLevel()).Forget();
                    break;
                case HomeCommand.Album:
                    _album.OpenAsync(shown).Forget();
                    break;
                case HomeCommand.AlbumLocked:
                    _overlay.ToastAbove(_texts.Format(HomeTexts.AlbumLocked, _balance.AlbumFromLevel), _window?.AlbumButton);
                    break;
                case HomeCommand.Settings:
                    _settings.OpenAsync(null, shown).Forget();
                    break;
                case HomeCommand.Reviewers:
                    _reviewers.OpenAsync(shown).Forget();
                    break;
            }
        }

        private async UniTaskVoid ShowMilestonesAsync(CancellationToken ct)
        {
            if (_curves.Active.IsOriginal)
            {
                return;
            }

            var levels = _progress.Levels;
            var onboarding = _progress.Onboarding;
            var album = levels.IsCompleted(_balance.AlbumFromLevel) && !onboarding.HasFlag(Flags.AlbumIntro);
            var name = levels.IsCompleted(_balance.NameShopAfterLevel) && !onboarding.HasFlag(Flags.NameAsked);
            if (!album && !name)
            {
                return;
            }

            await UniTask.Delay(MilestoneDelayMs, cancellationToken: ct);
            if (_window == null || _ui.IsPopupOpen)
            {
                return;
            }

            if (album)
            {
                await _album.IntroAsync(ct);
            }
            else
            {
                await _namePrompt.AskAsync(ct);
            }
        }
    }
}
