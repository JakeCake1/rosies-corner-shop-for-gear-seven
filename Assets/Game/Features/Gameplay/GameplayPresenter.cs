using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Features.Gameplay.Coach;
using Game.Features.Gameplay.Hud;
using Game.Features.Gameplay.Offers;
using Game.Features.Gameplay.Results;
using Game.Features.Gameplay.Session;
using Game.Services.Flow;
using Game.Services.UI.Dialogs;
using UniRx;
using UnityEngine;
using VContainer.Unity;

namespace Game.Features.Gameplay
{
    /// <summary>
    /// Runs the gameplay scene: starts the requested level and connects the session to the board, the HUD and the
    /// parts of the gameplay UI (booster bar and gifts, coach tips, purchase offers, win and loss dialogs).
    /// A port of the prototype's UI glue around the rules.
    /// </summary>
    public sealed class GameplayPresenter : IStartable, ITickable, IDisposable
    {
        private readonly GameplayContext _context;
        private readonly GameplayRequest _request;
        private readonly LevelLifetime _lifetime = new LevelLifetime();
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly HudPresenter _hud;
        private readonly LevelBannerPresenter _banner;
        private readonly IdleHint _idle;
        private readonly CoachPresenter _coach;
        private readonly PurchaseOffers _offers;
        private readonly BoosterBarPresenter _boosterBar;
        private readonly BoosterGifts _gifts;
        private readonly LevelWinPresenter _win;
        private readonly LevelLossPresenter _loss;

        /// <summary>
        /// Creates the presenter and its parts; VContainer calls <see cref="Start"/> once the gameplay scope is built.
        /// </summary>
        /// <param name="session">Level rules and state.</param>
        /// <param name="screen">The gameplay screen: board, HUD, booster bar and banner.</param>
        /// <param name="art">Booster and basket sprites.</param>
        /// <param name="request">Which level to start.</param>
        /// <param name="services">Shared services used by the gameplay UI.</param>
        public GameplayPresenter(LevelSession session, GameplayScreen screen, GameplayArt art,
            GameplayRequest request, GameplayServices services)
        {
            _context = new GameplayContext(session, screen, art, services, _lifetime);
            _request = request;
            _hud = new HudPresenter(_context, Restart);
            _banner = new LevelBannerPresenter(_context);
            _idle = new IdleHint(_context);
            _coach = new CoachPresenter(_context);
            _offers = new PurchaseOffers(_context);
            var boosters = new BoosterActions(_context, _offers, _idle, ShowLossAgain);
            _boosterBar = new BoosterBarPresenter(_context, boosters);
            _gifts = new BoosterGifts(_context, _boosterBar);
            _win = new LevelWinPresenter(_context, _offers, Restart);
            _loss = new LevelLossPresenter(_context, _offers, boosters, Restart);
        }

        /// <inheritdoc/>
        public void Start()
        {
            var session = _context.Session;
            session.Changed.Subscribe(OnChanged).AddTo(_disposables);
            session.Refused.Subscribe(OnRefused).AddTo(_disposables);
            session.Shipping.Subscribe(n => _context.Board.PlayShipping(n)).AddTo(_disposables);
            session.Notices.Subscribe(_coach.OnNotice).AddTo(_disposables);
            session.Finished.Subscribe(o => OnFinishedAsync(o).Forget()).AddTo(_disposables);
            session.UndoLeft.Subscribe(_ => _boosterBar.Refresh()).AddTo(_disposables);
            session.CanUndo.Subscribe(_ => _boosterBar.Refresh()).AddTo(_disposables);
            session.BonusPick.Subscribe(_ => _boosterBar.Refresh()).AddTo(_disposables);
            _context.Services.Boosters.Changed.Subscribe(_ => _boosterBar.Refresh()).AddTo(_disposables);
            _context.Board.ItemTapped.Subscribe(OnItemTapped).AddTo(_disposables);
            _context.Board.ExtraBasketClicked.Subscribe(_ => _offers.OfferExtraBasketAsync().Forget())
                .AddTo(_disposables);
            StartWhenLaidOutAsync().Forget();
        }

        /// <summary>
        /// Drives the hidden hint: after a few idle seconds the best move glows (levels 1–5 and lost levels).
        /// </summary>
        public void Tick()
        {
            _idle.Tick();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _lifetime.Dispose();
            _disposables.Dispose();
            _hud.Dispose();
            _boosterBar.Dispose();
        }

        private async UniTaskVoid StartWhenLaidOutAsync()
        {
            // The board sizes itself from the screen, so the first level waits for the first layout pass.
            await _context.Screen.WhenLaidOutAsync(_lifetime.SceneToken);
            StartLevelAsync(_request.Level, _request.StartBoosters).Forget();
        }

        private void Restart()
        {
            StartLevelAsync(_context.Level, null).Forget();
        }

        private async UniTaskVoid StartLevelAsync(int level, StartBoosters startBoosters)
        {
            _lifetime.Reset();
            // Together with the level token: a tip queued during the sprite load must not start a loop of its own.
            _coach.Reset();
            var token = _lifetime.LevelToken;
            _context.Services.Dialogs.CloseAllAsync().Forget();
            var prepared = _context.Session.Prepare(level);
            // Item sprites live in per-level Addressables groups: load this level's before it is drawn.
            try
            {
                await _context.Services.Content.LoadItemSpritesAsync(LevelItemTypes.Of(prepared), token);
            }
            catch (Exception e) when (!(e is OperationCanceledException))
            {
                // The old board is already retired by Prepare; without a way out the player would stare at it.
                Debug.LogException(e);
                OfferRetryAsync(level, startBoosters, token).Forget();
                return;
            }

            if (token.IsCancellationRequested)
            {
                // A newer start (restart, next level) replaced this one while its sprites loaded.
                return;
            }

            _idle.Touch();
            _context.Session.Start(startBoosters);
            _context.Services.Overlay.SetBackground(_context.Session.IsTricky, false);
            _hud.Bind();
            _boosterBar.Build();
            _banner.Show();
            _gifts.OnLevelStarted();
            _coach.OnLevelStarted();
        }

        private async UniTaskVoid OfferRetryAsync(int level, StartBoosters startBoosters, CancellationToken token)
        {
            var texts = _context.Services.Texts;
            var dialog = new DialogViewModel(texts.Get(GameplayTexts.LoadFailedTitle), RibbonStyle.Berry)
            {
                Text = texts.Get(GameplayTexts.LoadFailedText),
            };
            dialog.Buttons.Add(new DialogButton(texts.Get(GameplayTexts.LoadFailedRetry), ButtonStyle.Green,
                () => StartLevelAsync(level, startBoosters).Forget()));
            await _context.Services.Dialogs.ShowAsync(dialog, token).SuppressCancellationThrow();
        }

        private void OnChanged(BoardChange change)
        {
            var state = _context.Session.State;
            if (change.Kind == BoardChangeKind.LevelStart || change.Kind == BoardChangeKind.Rebuild)
            {
                _context.Board.Rebuild(state, change.Kind == BoardChangeKind.LevelStart);
            }
            else
            {
                _context.Board.Render(state, change);
            }

            _boosterBar.Refresh();
            _idle.Refresh();
        }

        private void OnItemTapped(int jarId)
        {
            if (_context.Services.UI.IsPopupOpen)
            {
                return;
            }

            _idle.Touch();
            _context.Board.SetHint(0);
            _context.Services.Overlay.ClearCoach();
            if (_context.Session.Tap(jarId))
            {
                _gifts.OnMoveMade();
            }
        }

        private void OnRefused(int jarId)
        {
            _context.Board.ShakeCounter(jarId);
            _context.Services.Overlay.Toast(_context.Services.Texts.Get(GameplayTexts.CounterFull), true);
        }

        private async UniTaskVoid OnFinishedAsync(LevelOutcome outcome)
        {
            _context.Services.Overlay.ClearCoach();
            _context.Board.SetHint(0);
            if (outcome.IsWon)
            {
                await _win.ShowAsync(outcome);
            }
            else
            {
                await _loss.ShowAsync(outcome.ItemsLeft);
            }
        }

        private void ShowLossAgain()
        {
            var state = _context.Session.State;
            _loss.ShowAsync(state.Total - state.Collected).Forget();
        }
    }
}
