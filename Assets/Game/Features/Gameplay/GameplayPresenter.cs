using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Features.Gameplay.Board;
using Game.Features.Gameplay.Boosters;
using Game.Features.Gameplay.Hud;
using Game.Features.Gameplay.Model;
using Game.Features.Gameplay.Session;
using Game.Services.Ads;
using Game.Services.Album;
using Game.Services.Audio;
using Game.Services.Content;
using Game.Services.Experiments;
using Game.Services.Flow;
using Game.Services.Levels;
using Game.Services.Localization;
using Game.Services.Onboarding;
using Game.Services.UI;
using Game.Services.UI.Dialogs;
using Game.Services.Wallet;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer.Unity;

namespace Game.Features.Gameplay
{
    /// <summary>
    /// Runs the gameplay scene: starts the requested level, connects the session to the board, HUD and booster
    /// bar, and shows every gameplay dialog (level complete, loss, purchase offers), coach tips, gifts, the
    /// level 1 tutorial and the idle hint. A port of the prototype's UI glue around the rules.
    /// </summary>
    public sealed class GameplayPresenter : IStartable, ITickable, IDisposable
    {
        private const float TipSeconds = 4.5f;
        private const int CompleteDelayMs = 900;

        private static readonly string[] HammerStockTips =
        {
            GameplayTexts.HammerStockLeft, GameplayTexts.HammerStockMiddle, GameplayTexts.HammerStockRight,
        };
        private static readonly string[] HammerTiedTips =
        {
            GameplayTexts.HammerTiedLeft, GameplayTexts.HammerTiedMiddle, GameplayTexts.HammerTiedRight,
        };
        private static readonly Dictionary<string, string> Gifts = new Dictionary<string, string>
        {
            [BoosterIds.Undo] = GameplayTexts.GiftUndo,
            [BoosterIds.Tidy] = GameplayTexts.GiftTidy,
            [BoosterIds.Pick] = GameplayTexts.GiftPick,
        };
        private static readonly Dictionary<string, string> BoosterNames = new Dictionary<string, string>
        {
            [BoosterIds.Undo] = GameplayTexts.BoosterUndo,
            [BoosterIds.Pick] = GameplayTexts.BoosterPick,
            [BoosterIds.Tidy] = GameplayTexts.BoosterTidy,
            [BoosterIds.Shuffle] = GameplayTexts.BoosterShuffle,
        };

        private readonly LevelSession _session;
        private readonly BoardView _board;
        private readonly GameplayScreen _screen;
        private readonly HudView _hud;
        private readonly List<BoosterButtonView> _boosterViews = new List<BoosterButtonView>();
        private readonly GameplayArt _art;
        private readonly GameplayRequest _request;
        private readonly IDialogService _dialogs;
        private readonly IUIService _ui;
        private readonly IOverlayService _overlay;
        private readonly IWalletService _wallet;
        private readonly IBoosterInventory _inventory;
        private readonly IOnboardingService _onboarding;
        private readonly ILevelProgressService _levels;
        private readonly IExperimentsService _experiments;
        private readonly IContentService _content;
        private readonly ILevelCurveService _curves;
        private readonly IGameFlow _flow;
        private readonly IRewardedAdService _ads;
        private readonly ISettingsScreen _settings;
        private readonly IAudioService _audio;
        private readonly ILocalizationService _texts;
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly List<BoosterViewModel> _boosters = new List<BoosterViewModel>();
        private readonly Queue<(string Text, VisualElement Anchor, bool IsAbove)> _tipQueue =
            new Queue<(string, VisualElement, bool)>();
        private readonly CancellationTokenSource _lifetime = new CancellationTokenSource();
        private HudViewModel _hudModel;
        private CancellationTokenSource _level;
        // Cached: reading Token from the disposed source throws instead of reporting cancellation.
        private readonly CancellationToken _lifetimeToken;
        private CancellationToken _levelToken;
        private string _pendingGift;
        private float _lastActionTime;
        private float _tipBusyUntil;
        private bool _isTutorialShown;

        /// <summary>
        /// Creates the presenter; VContainer calls <see cref="Start"/> once the gameplay scope is built.
        /// </summary>
        /// <param name="session">Level rules and state.</param>
        /// <param name="screen">The gameplay screen: board, HUD, booster bar and banner.</param>
        /// <param name="art">Booster and basket sprites.</param>
        /// <param name="request">Which level to start.</param>
        /// <param name="services">Shared services used by the gameplay UI.</param>
        public GameplayPresenter(LevelSession session, GameplayScreen screen, GameplayArt art,
            GameplayRequest request, GameplayServices services)
        {
            _session = session;
            _screen = screen;
            _board = screen.Board;
            _hud = screen.Hud;
            _art = art;
            _request = request;
            _dialogs = services.Dialogs;
            _ui = services.UI;
            _overlay = services.Overlay;
            _wallet = services.Wallet;
            _inventory = services.Boosters;
            _onboarding = services.Onboarding;
            _levels = services.Levels;
            _experiments = services.Experiments;
            _content = services.Content;
            _curves = services.Curves;
            _flow = services.Flow;
            _ads = services.Ads;
            _settings = services.Settings;
            _audio = services.Audio;
            _texts = services.Texts;
            _lifetimeToken = _lifetime.Token;
            ResetLevelLifetime();
        }

        /// <summary>
        /// Lifetime of the level in play: cancelled on restart, replay, the next level and scene unload, so delayed
        /// tips, gifts, result dialogs and ads of an old level never show over a new one.
        /// </summary>
        private CancellationToken Ct => _levelToken;

        private int Level => _session.LevelNumber;

        private bool HasAds => Level >= GameplayBalance.AdsFromLevel;

        /// <inheritdoc/>
        public void Start()
        {
            _session.Changed.Subscribe(OnChanged).AddTo(_disposables);
            _session.Refused.Subscribe(OnRefused).AddTo(_disposables);
            _session.Shipping.Subscribe(n => _board.PlayShipping(n)).AddTo(_disposables);
            _session.Notices.Subscribe(OnNotice).AddTo(_disposables);
            _session.Finished.Subscribe(o => OnFinishedAsync(o).Forget()).AddTo(_disposables);
            _session.UndoLeft.Subscribe(_ => RefreshBoosters()).AddTo(_disposables);
            _session.CanUndo.Subscribe(_ => RefreshBoosters()).AddTo(_disposables);
            _session.BonusPick.Subscribe(_ => RefreshBoosters()).AddTo(_disposables);
            _inventory.Changed.Subscribe(_ => RefreshBoosters()).AddTo(_disposables);
            _board.ItemTapped.Subscribe(OnItemTapped).AddTo(_disposables);
            _board.ExtraBasketClicked.Subscribe(_ => OfferExtraBasketAsync().Forget()).AddTo(_disposables);
            StartWhenLaidOutAsync().Forget();
        }

        private async UniTaskVoid StartWhenLaidOutAsync()
        {
            // The board sizes itself from the screen, so the first level waits for the first layout pass.
            await _screen.WhenLaidOutAsync(_lifetimeToken);
            StartLevel(_request.Level, _request.StartBoosters);
        }

        /// <summary>
        /// Drives the hidden hint: after a few idle seconds the best move glows (levels 1–5 and lost levels).
        /// </summary>
        public void Tick()
        {
            if (_session.State == null || _session.IsOver || _ui.IsPopupOpen) return;
            if (Level > GameplayBalance.HintUntilLevel && !_levels.HasFailed(Level)) return;
            if (Time.unscaledTime - _lastActionTime < GameplayBalance.HintIdleSeconds) return;

            _lastActionTime = Time.unscaledTime;
            var hint = _session.BestHint();
            if (hint != null) _board.SetHint(hint.Id);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _level.Cancel();
            _level.Dispose();
            _lifetime.Cancel();
            _lifetime.Dispose();
            _disposables.Dispose();
            DisposeHud();
            ClearBoosterViews();
        }

        private void StartLevel(int level, StartBoosters startBoosters)
        {
            ResetLevelLifetime();
            _dialogs.CloseAllAsync().Forget();
            _overlay.ClearCoach();
            _tipQueue.Clear();
            _tipBusyUntil = 0f;
            _pendingGift = null;
            _isTutorialShown = false;
            _lastActionTime = Time.unscaledTime;
            _session.Start(level, startBoosters);
            _overlay.SetBackground(_session.IsTricky, false);
            BindHud();
            BuildBoosters();
            ShowBanner();
            GiftBooster();
            if (level == 1) ShowTutorialAsync().Forget();
            if (level == GameplayBalance.ExtraBasketSlotLevel && !_onboarding.HasFlag(Flags.FreeBasket)) ShowBasketTipAsync().Forget();
        }

        private void ResetLevelLifetime()
        {
            if (_level != null)
            {
                _level.Cancel();
                _level.Dispose();
            }

            _level = CancellationTokenSource.CreateLinkedTokenSource(_lifetimeToken);
            _levelToken = _level.Token;
        }

        private void BindHud()
        {
            DisposeHud();
            _hudModel = new HudViewModel(_session, _wallet, _texts);
            _hudModel.SettingsRequested.Subscribe(_ => OpenSettings()).AddTo(_disposables);
            _hud.Bind(_hudModel);
        }

        private void DisposeHud()
        {
            if (_hudModel == null) return;

            _hud.Unbind();
            _hudModel.Dispose();
            _hudModel = null;
        }

        private void OpenSettings()
        {
            _settings.OpenAsync(() => StartLevel(Level, null), Ct).Forget();
        }

        private void ShowBanner()
        {
            var title = _session.IsTricky
                ? _texts.Get(GameplayTexts.BannerTricky)
                : _texts.Format(GameplayTexts.BannerLevel, Level);
            var column = _session.State.HammerColumn;
            string tipKey;
            switch (_session.State.HammerPlace)
            {
                case HammerPlace.Stock:
                    tipKey = HammerStockTips[column];
                    break;
                case HammerPlace.Tied:
                    tipKey = HammerTiedTips[column];
                    break;
                case HammerPlace.Shelf:
                    tipKey = GameplayTexts.HammerShelf;
                    break;
                default:
                    tipKey = _session.Definition.TipKey;
                    break;
            }

            var tip = string.IsNullOrEmpty(tipKey) ? string.Empty : _texts.Get(tipKey);

            _screen.Banner.Show(title, tip, _session.IsTricky, _board.Counter);
        }

        private void OnChanged(BoardChange change)
        {
            if (change.Kind == BoardChangeKind.LevelStart || change.Kind == BoardChangeKind.Rebuild)
            {
                _board.Rebuild(_session.State, change.Kind == BoardChangeKind.LevelStart);
            }
            else
            {
                _board.Render(_session.State, change);
            }

            RefreshBoosters();
        }

        private void OnItemTapped(int jarId)
        {
            if (_ui.IsPopupOpen) return;

            _lastActionTime = Time.unscaledTime;
            _board.SetHint(0);
            _overlay.ClearCoach();
            if (!_session.Tap(jarId)) return;

            if (_pendingGift != null)
            {
                var gift = _pendingGift;
                _pendingGift = null;
                ShowGiftAsync(gift, 650).Forget();
            }
        }

        private void OnRefused(int jarId)
        {
            _board.ShakeCounter(jarId);
            _overlay.Toast(_texts.Get(GameplayTexts.CounterFull), true);
        }

        private void OnNotice(string notice)
        {
            switch (notice)
            {
                case "oneLeft":
                    _overlay.Toast(_texts.Get(GameplayTexts.OneSpotLeft), true);
                    break;
                case "counter":
                    QueueTip("counter", GameplayTexts.TipCounter, _board.Counter, false);
                    break;
                case "stock":
                    QueueTip("stock", GameplayTexts.TipStock, _board.StockLabels, false);
                    break;
                case "ship":
                    QueueTip("ship", GameplayTexts.TipShip, _board.BasketArea, false);
                    break;
                default:
                    if (notice.StartsWith("family:"))
                    {
                        var family = _texts.GetFamilyName(notice.Substring("family:".Length));
                        _overlay.Toast(_texts.Format(GameplayTexts.FamilyComplete, family, AlbumService.FamilyBonus));
                    }

                    break;
            }
        }

        private void QueueTip(string key, string textKey, VisualElement anchor, bool isAbove)
        {
            if (Level > GameplayBalance.CoachTipsUntilLevel || _onboarding.HasSeenTip(key)) return;

            _onboarding.MarkTipSeen(key);
            _tipQueue.Enqueue((_texts.Get(textKey), anchor, isAbove));
            if (Time.unscaledTime >= _tipBusyUntil) ShowNextTipAsync().Forget();
        }

        private async UniTaskVoid ShowNextTipAsync()
        {
            while (_tipQueue.Count > 0)
            {
                await UniTask.WaitUntil(() => !_ui.IsPopupOpen, cancellationToken: Ct);
                var (text, anchor, isAbove) = _tipQueue.Dequeue();
                _overlay.Coach(text, anchor, isAbove, TipSeconds);
                _tipBusyUntil = Time.unscaledTime + TipSeconds + 0.5f;
                await UniTask.Delay(TimeSpan.FromSeconds(TipSeconds + 0.5f), cancellationToken: Ct);
            }
        }

        private async UniTaskVoid ShowTutorialAsync()
        {
            await UniTask.Delay(2300, cancellationToken: Ct);
            if (_isTutorialShown || _session.State.Moves > 0 || _session.LevelNumber != 1) return;

            var basket = _session.State.Baskets.FirstOrDefault(b => b != null);
            if (basket == null) return;

            var target = LevelRules.VisibleJars(_session.State, false).FirstOrDefault(v => v.Jar.Type == basket.Type);
            var rect = target.Jar != null ? _board.FindItem(target.Jar.Id) : null;
            if (rect == null) return;

            _isTutorialShown = true;
            var name = _texts.GetItemName(basket.Type);
            _board.SetHint(target.Jar.Id);
            _overlay.PointAt(rect);
            _overlay.Coach(_texts.Format(GameplayTexts.Tutorial, name), rect, true, 0f);
        }

        private async UniTaskVoid ShowBasketTipAsync()
        {
            await UniTask.Delay(5200, cancellationToken: Ct);
            var plus = _board.FirstBasketLock();
            if (plus == null || _ui.IsPopupOpen || Level != GameplayBalance.ExtraBasketSlotLevel) return;

            _overlay.Coach(_texts.Get(GameplayTexts.BasketTip), plus, false, 6f);
        }

        private void GiftBooster()
        {
            var unlocks = new[]
            {
                (BoosterIds.Undo, GameplayBalance.UndoUnlockLevel), (BoosterIds.Tidy, GameplayBalance.TidyUnlockLevel),
                (BoosterIds.Pick, GameplayBalance.PickUnlockLevel),
            };
            foreach (var (id, level) in unlocks)
            {
                if (level != Level || _inventory.IsGifted(id)) continue;

                _inventory.MarkGifted(id);
                if (id != BoosterIds.Undo) _inventory.Add(id, GameplayBalance.BoosterGift);
                // Undo means nothing before the first move, so its tip waits for that move.
                if (id == BoosterIds.Undo) _pendingGift = id;
                else ShowGiftAsync(id, 2400).Forget();
            }
        }

        private async UniTaskVoid ShowGiftAsync(string id, int delayMs)
        {
            await UniTask.Delay(delayMs, cancellationToken: Ct);
            var vm = _boosters.FirstOrDefault(b => b.Id == id);
            if (vm == null || _ui.IsPopupOpen) return;

            vm.SetGift(true);
            var index = _boosters.IndexOf(vm);
            _overlay.Coach(_texts.Get(Gifts[id]), _boosterViews[index].Root, true, 4f);
        }

        private void BuildBoosters()
        {
            foreach (var b in _boosters)
            {
                b.Dispose();
            }

            _boosters.Clear();
            ClearBoosterViews();
            var all = new[] { BoosterIds.Undo, BoosterIds.Pick, BoosterIds.Tidy, BoosterIds.Shuffle };
            foreach (var id in all)
            {
                if (!_session.IsUnlocked(id)) continue;

                var vm = new BoosterViewModel(id, _texts.Get(BoosterNames[id]), UseBooster);
                _boosters.Add(vm);
                var view = _screen.AddBooster();
                view.SetIcon(_art.IconOf(id));
                view.Bind(vm);
                _boosterViews.Add(view);
            }

            RefreshBoosters();
        }

        private void ClearBoosterViews()
        {
            foreach (var view in _boosterViews)
            {
                view.Unbind();
                _screen.RemoveBooster(view);
            }

            _boosterViews.Clear();
        }

        private void RefreshBoosters()
        {
            if (_session.State == null) return;

            foreach (var vm in _boosters)
            {
                switch (vm.Id)
                {
                    case BoosterIds.Undo:
                        var almostFull = _session.State.Counter.Count >= _session.State.Capacity - 1;
                        vm.Set(_session.UndoLeft.Value, !_session.CanUndo.Value,
                            almostFull && _session.CanUndo.Value && _session.UndoLeft.Value > 0);
                        break;
                    case BoosterIds.Pick:
                        vm.Set(_inventory.Get(BoosterIds.Pick) + _session.BonusPick.Value, false, false);
                        break;
                    default:
                        vm.Set(_inventory.Get(vm.Id), false, false);
                        break;
                }
            }
        }

        private void UseBooster(string id)
        {
            _overlay.ClearCoach();
            _lastActionTime = Time.unscaledTime;
            if (id == BoosterIds.Undo)
            {
                Undo();
                return;
            }

            if (_session.IsOver && id != BoosterIds.Tidy) return;

            var owned = _inventory.Get(id) + (id == BoosterIds.Pick ? _session.BonusPick.Value : 0);
            if (owned <= 0)
            {
                OfferBoosterAsync(id).Forget();
                return;
            }

            switch (id)
            {
                case BoosterIds.Pick:
                    _overlay.Toast(_texts.Get(_session.UseQuickPick() ? GameplayTexts.PickDone : GameplayTexts.PickNone));
                    break;
                case BoosterIds.Tidy:
                    _overlay.Toast(_texts.Get(_session.UseTidy() ? GameplayTexts.TidyDone : GameplayTexts.TidyNone));
                    break;
                case BoosterIds.Shuffle:
                    if (!_session.UseShuffle()) _overlay.Toast(_texts.Get(GameplayTexts.ShuffleNone));
                    break;
            }
        }

        private void Undo()
        {
            switch (_session.Undo())
            {
                case UndoResult.Done:
                    _dialogs.CloseAllAsync().Forget();
                    _overlay.Toast(_texts.Get(GameplayTexts.UndoDone));
                    break;
                case UndoResult.NothingToUndo:
                    _overlay.Toast(_texts.Get(GameplayTexts.UndoNone));
                    break;
                case UndoResult.NoUndosLeft:
                    OfferUndoAsync().Forget();
                    break;
            }
        }

        private async UniTaskVoid OfferUndoAsync()
        {
            var wasOver = _session.IsOver;
            var dialog = new DialogViewModel(_texts.Get(GameplayTexts.BoosterUndo), RibbonStyle.Berry)
            {
                Icon = _art.Undo,
                Text = _texts.Get(GameplayTexts.UndoOffer),
            };
            var buy = new DialogButton(_texts.Get(GameplayTexts.BuyTwo), ButtonStyle.Gold, () =>
            {
                if (!_wallet.TrySpend(GameplayBalance.UndoPackPrice)) return;

                _audio.Play(SoundId.Coin);
                _session.AddUndos(GameplayBalance.UndoPackSize);
                Undo();
            }) { Badge = ButtonBadge.Coins, Price = GameplayBalance.UndoPackPrice };
            buy.IsEnabled.Value = _wallet.Coins.Value >= GameplayBalance.UndoPackPrice;
            dialog.Buttons.Add(buy);
            if (HasAds)
            {
                var adLabel = _texts.Get(GameplayTexts.GetOneFree);
                dialog.Buttons.Add(new DialogButton(adLabel, ButtonStyle.Green, () => RewardAsync(() =>
                {
                    _session.AddUndos(1);
                    Undo();
                }).Forget()) { Badge = ButtonBadge.Ad });
            }

            dialog.Buttons.Add(new DialogButton(_texts.Get(GameplayTexts.NotNow), ButtonStyle.WoodSmall, () =>
            {
                if (wasOver && _session.IsOver) ShowLossAsync(_session.State.Total - _session.State.Collected).Forget();
            }));
            await _dialogs.ShowAsync(dialog, Ct);
        }

        private async UniTaskVoid OfferBoosterAsync(string id)
        {
            var title = _texts.Get(BoosterNames[id]);
            var text = _texts.Get(id == BoosterIds.Pick ? GameplayTexts.PickInfo
                : id == BoosterIds.Tidy ? GameplayTexts.TidyInfo
                : GameplayTexts.ShuffleInfo);
            var dialog = new DialogViewModel(title, RibbonStyle.Berry) { Icon = _art.IconOf(id), Text = text };
            var buy = new DialogButton(_texts.Get(GameplayTexts.BuyOne), ButtonStyle.Gold, () =>
            {
                if (!_wallet.TrySpend(GameplayBalance.BoosterPrice)) return;

                _audio.Play(SoundId.Coin);
                _inventory.Add(id, 1);
            }) { Badge = ButtonBadge.Coins, Price = GameplayBalance.BoosterPrice };
            buy.IsEnabled.Value = _wallet.Coins.Value >= GameplayBalance.BoosterPrice;
            dialog.Buttons.Add(buy);
            if (HasAds)
            {
                var adLabel = _texts.Get(GameplayTexts.GetOneFree);
                dialog.Buttons.Add(new DialogButton(adLabel, ButtonStyle.Green, () => RewardAsync(() =>
                {
                    _inventory.Add(id, 1);
                    _overlay.Toast(_texts.Format(GameplayTexts.BoosterAdded, title));
                }).Forget()) { Badge = ButtonBadge.Ad });
            }

            dialog.Buttons.Add(new DialogButton(_texts.Get(GameplayTexts.NotNow), ButtonStyle.WoodSmall, null));
            await _dialogs.ShowAsync(dialog, Ct);
        }

        private async UniTaskVoid OfferExtraBasketAsync()
        {
            if (_ui.IsPopupOpen || _session.IsOver) return;

            _overlay.ClearCoach();
            if (!_onboarding.HasFlag(Flags.FreeBasket))
            {
                _onboarding.SetFlag(Flags.FreeBasket);
                _session.OpenExtraBasket();
                _overlay.Toast(_texts.Get(GameplayTexts.BasketFree));
                return;
            }

            var dialog = new DialogViewModel(_texts.Get(GameplayTexts.BasketTitle), RibbonStyle.Green)
            {
                Icon = _art.Basket,
                Text = _texts.Get(GameplayTexts.BasketText),
            };
            var buy = new DialogButton(_texts.Get(GameplayTexts.BasketOpen), ButtonStyle.Gold, () =>
            {
                if (_wallet.TrySpend(GameplayBalance.ExtraBasketPrice)) _session.OpenExtraBasket();
            }) { Badge = ButtonBadge.Coins, Price = GameplayBalance.ExtraBasketPrice };
            buy.IsEnabled.Value = _wallet.Coins.Value >= GameplayBalance.ExtraBasketPrice;
            dialog.Buttons.Add(buy);
            if (HasAds)
            {
                dialog.Buttons.Add(new DialogButton(_texts.Get(GameplayTexts.BasketOpenFree), ButtonStyle.Green,
                    () => RewardAsync(_session.OpenExtraBasket).Forget()) { Badge = ButtonBadge.Ad });
            }

            dialog.Buttons.Add(new DialogButton(_texts.Get(GameplayTexts.NotNow), ButtonStyle.WoodSmall, null));
            await _dialogs.ShowAsync(dialog, Ct);
        }

        private async UniTaskVoid RewardAsync(Action reward)
        {
            if (await _ads.ShowAsync(Ct)) reward();
        }

        private async UniTaskVoid OnFinishedAsync(LevelOutcome outcome)
        {
            _overlay.ClearCoach();
            _board.SetHint(0);
            if (!outcome.IsWon)
            {
                await ShowLossAsync(outcome.ItemsLeft);
                return;
            }

            _overlay.Burst(_board.Counter.worldBound.center + Vector2.left * 80f, EffectKind.Confetti, 60);
            await UniTask.Delay(220, cancellationToken: Ct);
            _overlay.Burst(_board.BasketArea.worldBound.center + Vector2.right * 80f, EffectKind.Confetti, 60);
            await UniTask.Delay(CompleteDelayMs - 220, cancellationToken: Ct);
            // Undo still works on a won board and reopens the level without restarting it.
            if (!_session.IsOver) return;

            await ShowCompleteAsync(outcome);
        }

        private async UniTask ShowCompleteAsync(LevelOutcome outcome)
        {
            var st = _session.State;
            var threshold = GameplayBalance.StarThreshold(Level);
            var title = _session.IsTricky
                ? _texts.Get(GameplayTexts.CompleteTrickyTitle)
                : _texts.Format(GameplayTexts.CompleteTitle, Level);
            var headline = outcome.Stars == 3 ? GameplayTexts.HeadlineThreeStars
                : outcome.Stars == 2 ? GameplayTexts.HeadlineTwoStars
                : GameplayTexts.HeadlineOneStar;
            var dialog = new DialogViewModel(title, RibbonStyle.Green)
            {
                Stars = outcome.Stars,
                Headline = _texts.Get(headline),
                Text = outcome.Stars == 3
                    ? _texts.Get(GameplayTexts.CompleteNeat)
                    : _texts.Format(GameplayTexts.CompleteHint, threshold),
                Reward = "+" + outcome.Reward,
            };
            dialog.Stats.Add((_texts.Get(GameplayTexts.StatItems), st.Total.ToString()));
            dialog.Stats.Add((_texts.Get(GameplayTexts.StatPeak), $"{st.Peak} / {st.Capacity}"));
            dialog.Stats.Add((_texts.Get(GameplayTexts.StatMoves), st.Moves.ToString()));
            if (HasAds)
            {
                var adLabel = _texts.Get(GameplayTexts.DoubleCoins);
                dialog.Buttons.Add(new DialogButton(adLabel, ButtonStyle.Green, () => RewardAsync(() =>
                {
                    _wallet.Add(outcome.Reward);
                    _audio.Play(SoundId.Coin);
                    _overlay.Toast(_texts.Format(GameplayTexts.CoinsAdded, outcome.Reward));
                    AfterWin();
                }).Forget()) { Badge = ButtonBadge.Ad });
            }

            var replay = new DialogButton(_texts.Get(GameplayTexts.Replay), ButtonStyle.Wood, () => StartLevel(Level, null));
            replay.IsInRow = true;
            dialog.Buttons.Add(replay);
            var isDirect = Level < GameplayBalance.DirectNextLevelBelow;
            var nextLabel = _texts.Get(isDirect ? GameplayTexts.NextLevel : GameplayTexts.Continue);
            var next = new DialogButton(nextLabel, ButtonStyle.Berry, AfterWin) { IsInRow = true };
            if (isDirect && Level < _curves.Count && _curves.Get(Level + 1).IsHard)
            {
                next.SubLabel = _texts.Get(GameplayTexts.NextTricky);
            }

            dialog.Buttons.Add(next);
            await _dialogs.ShowAsync(dialog, Ct);
        }

        private void AfterWin()
        {
            // Prototype quirk kept on purpose: from level 9 on "Continue" returns to the shop, where the album and
            // shop-naming milestones are shown and "Open the shop" starts the next level.
            if (Level < GameplayBalance.DirectNextLevelBelow && Level < _curves.Count) _flow.PlayLevelAsync(Level + 1).Forget();
            else _flow.GoHomeAsync().Forget();
        }

        private async UniTask ShowLossAsync(int left)
        {
            var st = _session.State;
            var canUndo = _session.IsUnlocked(BoosterIds.Undo) && _session.CanUndo.Value && _session.UndoLeft.Value > 0;
            var tidy = _inventory.Get(BoosterIds.Tidy);
            var offer = !HasAds
                ? null
                : _experiments.LossOffer == "basket" && st.UnlockedBaskets < LevelState.MaxBaskets ? "basket"
                : st.Capacity < GameplayBalance.ExtendedCapacity ? "slot" : null;
            var caption = left / (float)st.Total > 0.35f ? GameplayTexts.LossLeft
                : left == 1 ? GameplayTexts.LossOneLeft
                : GameplayTexts.LossFewLeft;
            var dialog = new DialogViewModel(_texts.Get(GameplayTexts.LossTitle), RibbonStyle.Berry)
            {
                Progress = st.Collected / (float)st.Total,
                ProgressCaption = _texts.Format(caption, left),
            };
            dialog.Items.AddRange(st.Counter.Select(j => _content.Catalog.Get(j.Type).Sprite));
            if (offer == "basket")
            {
                var adLabel = _texts.Get(GameplayTexts.CallCustomer);
                dialog.Buttons.Add(new DialogButton(adLabel, ButtonStyle.Green, () => RewardAsync(() =>
                {
                    _session.OpenExtraBasket();
                    _overlay.Toast(_texts.Get(GameplayTexts.CustomerArrived));
                }).Forget()) { Badge = ButtonBadge.Ad });
                dialog.Note = _texts.Get(GameplayTexts.CustomerNote);
            }
            else if (offer == "slot")
            {
                var adLabel = _texts.Get(GameplayTexts.ExtraSpot);
                dialog.Buttons.Add(new DialogButton(adLabel, ButtonStyle.Green, () => RewardAsync(() =>
                {
                    _session.ExtendCounter();
                    _overlay.Toast(_texts.Get(GameplayTexts.SpotAdded));
                }).Forget()) { Badge = ButtonBadge.Ad });
            }

            if (canUndo) dialog.Buttons.Add(new DialogButton(_texts.Get(GameplayTexts.UndoFree), ButtonStyle.Wood, Undo));
            if (_session.IsUnlocked(BoosterIds.Tidy))
            {
                var tidyLabel = tidy > 0
                    ? _texts.Format(GameplayTexts.TidyLeft, tidy)
                    : _texts.Get(GameplayTexts.BoosterTidy);
                var tidyButton = new DialogButton(tidyLabel, ButtonStyle.Gold, () =>
                {
                    if (_inventory.Get(BoosterIds.Tidy) <= 0)
                    {
                        if (!_wallet.TrySpend(GameplayBalance.BoosterPrice)) return;

                        _inventory.Add(BoosterIds.Tidy, 1);
                    }

                    UseBooster(BoosterIds.Tidy);
                });
                if (tidy <= 0)
                {
                    tidyButton.Badge = ButtonBadge.Coins;
                    tidyButton.Price = GameplayBalance.BoosterPrice;
                    tidyButton.IsEnabled.Value = _wallet.Coins.Value >= GameplayBalance.BoosterPrice;
                }

                dialog.Buttons.Add(tidyButton);
            }

            dialog.Buttons.Add(new DialogButton(_texts.Get(GameplayTexts.StartOver), ButtonStyle.WoodSmall,
                () => StartLevel(Level, null)));
            await _dialogs.ShowAsync(dialog, Ct);
        }
    }
}
