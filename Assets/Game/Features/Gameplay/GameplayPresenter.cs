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
        private const string TipCounter = "No basket wants this one yet, so it waits on the counter. If the counter fills up, you're stuck!";
        private const string TipShip = "Basket full! It's on its way, and a new order arrives.";
        private const string TipStock = "The labels show how many items are still in the stock behind each column.";
        private const float TipSeconds = 4.5f;
        private const int CompleteDelayMs = 900;

        private static readonly string[] ColumnNames = { "left", "middle", "right" };
        private static readonly Dictionary<string, string> Gifts = new Dictionary<string, string>
        {
            [BoosterIds.Undo] = "New: <b>Undo</b>. Took the wrong item? Take it back. Two free per level.",
            [BoosterIds.Tidy] = "A gift from Rosie: <b>two Tidy Ups</b>. Each clears the counter when it gets crowded.",
            [BoosterIds.Pick] = "A gift from Rosie: <b>two Quick Picks</b>. Each fills a basket for you.",
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
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly List<BoosterViewModel> _boosters = new List<BoosterViewModel>();
        private readonly Queue<(string Text, VisualElement Anchor, bool IsAbove)> _tipQueue =
            new Queue<(string, VisualElement, bool)>();
        private readonly CancellationTokenSource _lifetime = new CancellationTokenSource();
        private HudViewModel _hudModel;
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
        }

        private CancellationToken Ct => _lifetime.Token;

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
            await _screen.WhenLaidOutAsync(Ct);
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
            _lifetime.Cancel();
            _lifetime.Dispose();
            _disposables.Dispose();
            DisposeHud();
            ClearBoosterViews();
        }

        private void StartLevel(int level, StartBoosters startBoosters)
        {
            _dialogs.CloseAllAsync().Forget();
            _overlay.ClearCoach();
            _tipQueue.Clear();
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

        private void BindHud()
        {
            DisposeHud();
            _hudModel = new HudViewModel(_session, _wallet);
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
            _settings.OpenAsync(() => StartLevel(Level, null)).Forget();
        }

        private void ShowBanner()
        {
            var title = _session.IsTricky ? "Tricky Level" : "Level " + Level;
            var hammer = _session.State.HammerPlace;
            var column = _session.State.HammerColumn >= 0 ? ColumnNames[_session.State.HammerColumn] : string.Empty;
            string tip;
            switch (hammer)
            {
                case HammerPlace.Stock:
                    tip = $"The hammer is in the {column} column's stock. Dig there first!";
                    break;
                case HammerPlace.Tied:
                    tip = $"The hammer is tied up in the {column} column. Find the key first!";
                    break;
                case HammerPlace.Shelf:
                    tip = "Tap the hammer to open the nailed shelf.";
                    break;
                default:
                    tip = _session.Definition.Tip;
                    break;
            }

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
            _overlay.Toast("The counter is full", true);
        }

        private void OnNotice(string notice)
        {
            switch (notice)
            {
                case "oneLeft":
                    _overlay.Toast("Only one spot left!", true);
                    break;
                case "counter":
                    QueueTip("counter", TipCounter, _board.Counter, false);
                    break;
                case "stock":
                    QueueTip("stock", TipStock, _board.StockLabels, false);
                    break;
                case "ship":
                    QueueTip("ship", TipShip, _board.BasketArea, false);
                    break;
                default:
                    if (notice.StartsWith("family:"))
                    {
                        var family = _content.Catalog.FamilyName(notice.Substring("family:".Length));
                        _overlay.Toast($"{family} complete! +{AlbumService.FamilyBonus} coins");
                    }

                    break;
            }
        }

        private void QueueTip(string key, string text, VisualElement anchor, bool isAbove)
        {
            if (Level > GameplayBalance.CoachTipsUntilLevel || _onboarding.HasSeenTip(key)) return;

            _onboarding.MarkTipSeen(key);
            _tipQueue.Enqueue((text, anchor, isAbove));
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
            var name = _content.Catalog.Get(basket.Type).Name;
            _board.SetHint(target.Jar.Id);
            _overlay.PointAt(rect);
            _overlay.Coach($"Tap the <b>{name}</b>. It goes straight into the basket that asks for it.", rect, true, 0f);
        }

        private async UniTaskVoid ShowBasketTipAsync()
        {
            await UniTask.Delay(5200, cancellationToken: Ct);
            var plus = _board.FirstBasketLock();
            if (plus == null || _ui.IsPopupOpen || Level != GameplayBalance.ExtraBasketSlotLevel) return;

            _overlay.Coach("Tap <b>+</b> to call one more customer. Free this time!", plus, false, 6f);
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
            _overlay.Coach(Gifts[id], _boosterViews[index].Root, true, 4f);
        }

        private void BuildBoosters()
        {
            foreach (var b in _boosters)
            {
                b.Dispose();
            }

            _boosters.Clear();
            ClearBoosterViews();
            var all = new[]
            {
                (BoosterIds.Undo, "Undo"), (BoosterIds.Pick, "Quick Pick"), (BoosterIds.Tidy, "Tidy Up"),
                (BoosterIds.Shuffle, "Shuffle"),
            };
            foreach (var (id, title) in all)
            {
                if (!_session.IsUnlocked(id)) continue;

                var vm = new BoosterViewModel(id, title, UseBooster);
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
                    _overlay.Toast(_session.UseQuickPick() ? "Quick Pick filled a basket" : "No basket to fill right now");
                    break;
                case BoosterIds.Tidy:
                    _overlay.Toast(_session.UseTidy() ? "Items went back to the stockroom" : "The counter is already clear");
                    break;
                case BoosterIds.Shuffle:
                    if (!_session.UseShuffle()) _overlay.Toast("Nothing to shuffle");
                    break;
            }
        }

        private void Undo()
        {
            switch (_session.Undo())
            {
                case UndoResult.Done:
                    _dialogs.CloseAllAsync().Forget();
                    _overlay.Toast("Move undone");
                    break;
                case UndoResult.NothingToUndo:
                    _overlay.Toast("Nothing to undo yet");
                    break;
                case UndoResult.NoUndosLeft:
                    OfferUndoAsync().Forget();
                    break;
            }
        }

        private async UniTaskVoid OfferUndoAsync()
        {
            var wasOver = _session.IsOver;
            var dialog = new DialogViewModel("Undo", RibbonStyle.Berry)
            {
                Icon = _art.Undo,
                Text = "Out of free undos on this level. Take back your last move.",
            };
            var buy = new DialogButton("Get 2", ButtonStyle.Gold, () =>
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
                dialog.Buttons.Add(new DialogButton("Get 1 free", ButtonStyle.Green, () => RewardAsync(() =>
                {
                    _session.AddUndos(1);
                    Undo();
                }).Forget()) { Badge = ButtonBadge.Ad });
            }

            dialog.Buttons.Add(new DialogButton("Not now", ButtonStyle.WoodSmall, () =>
            {
                if (wasOver && _session.IsOver) ShowLossAsync(_session.State.Total - _session.State.Collected).Forget();
            }));
            await _dialogs.ShowAsync(dialog, Ct);
        }

        private async UniTaskVoid OfferBoosterAsync(string id)
        {
            var title = id == BoosterIds.Pick ? "Quick Pick" : id == BoosterIds.Tidy ? "Tidy Up" : "Shuffle";
            var text = id == BoosterIds.Pick
                ? "Fills the basket that's closest to done, pulling items from the counter, the shelves, even the stockroom."
                : id == BoosterIds.Tidy
                    ? "Puts up to three items from the counter back on a shelf, so the counter has room again."
                    : "Mixes up all the items on the open shelves.";
            var dialog = new DialogViewModel(title, RibbonStyle.Berry) { Icon = _art.IconOf(id), Text = text };
            var buy = new DialogButton("Get 1", ButtonStyle.Gold, () =>
            {
                if (!_wallet.TrySpend(GameplayBalance.BoosterPrice)) return;

                _audio.Play(SoundId.Coin);
                _inventory.Add(id, 1);
            }) { Badge = ButtonBadge.Coins, Price = GameplayBalance.BoosterPrice };
            buy.IsEnabled.Value = _wallet.Coins.Value >= GameplayBalance.BoosterPrice;
            dialog.Buttons.Add(buy);
            if (HasAds)
            {
                dialog.Buttons.Add(new DialogButton("Get 1 free", ButtonStyle.Green, () => RewardAsync(() =>
                {
                    _inventory.Add(id, 1);
                    _overlay.Toast(title + " +1");
                }).Forget()) { Badge = ButtonBadge.Ad });
            }

            dialog.Buttons.Add(new DialogButton("Not now", ButtonStyle.WoodSmall, null));
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
                _overlay.Toast("Rosie opened a third basket. On the house!");
                return;
            }

            var dialog = new DialogViewModel("Extra basket", RibbonStyle.Green)
            {
                Icon = _art.Basket,
                Text = "Open a third basket for this level. More baskets, fewer items waiting on the counter.",
            };
            var buy = new DialogButton("Open", ButtonStyle.Gold, () =>
            {
                if (_wallet.TrySpend(GameplayBalance.ExtraBasketPrice)) _session.OpenExtraBasket();
            }) { Badge = ButtonBadge.Coins, Price = GameplayBalance.ExtraBasketPrice };
            buy.IsEnabled.Value = _wallet.Coins.Value >= GameplayBalance.ExtraBasketPrice;
            dialog.Buttons.Add(buy);
            if (HasAds)
            {
                dialog.Buttons.Add(new DialogButton("Open free", ButtonStyle.Green,
                    () => RewardAsync(_session.OpenExtraBasket).Forget()) { Badge = ButtonBadge.Ad });
            }

            dialog.Buttons.Add(new DialogButton("Not now", ButtonStyle.WoodSmall, null));
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
            await ShowCompleteAsync(outcome);
        }

        private async UniTask ShowCompleteAsync(LevelOutcome outcome)
        {
            var st = _session.State;
            var threshold = GameplayBalance.StarThreshold(Level);
            var dialog = new DialogViewModel(_session.IsTricky ? "Tricky level complete" : $"Level {Level} complete",
                RibbonStyle.Green)
            {
                Stars = outcome.Stars,
                Headline = outcome.Stars == 3 ? "Beautifully kept!" : outcome.Stars == 2 ? "Lovely work!" : "Shelf cleared!",
                Text = outcome.Stars == 3
                    ? "The counter never got crowded. That's the neat way to do it."
                    : $"Keep the counter at {threshold} items or fewer for three stars.",
                Reward = "+" + outcome.Reward,
            };
            dialog.Stats.Add(("Items packed", st.Total.ToString()));
            dialog.Stats.Add(("Most items on the counter", $"{st.Peak} / {st.Capacity}"));
            dialog.Stats.Add(("Moves", st.Moves.ToString()));
            if (HasAds)
            {
                dialog.Buttons.Add(new DialogButton("Double coins", ButtonStyle.Green, () => RewardAsync(() =>
                {
                    _wallet.Add(outcome.Reward);
                    _audio.Play(SoundId.Coin);
                    _overlay.Toast($"+{outcome.Reward} coins");
                    AfterWin();
                }).Forget()) { Badge = ButtonBadge.Ad });
            }

            dialog.Buttons.Add(new DialogButton("Replay", ButtonStyle.Wood, () => StartLevel(Level, null)) { IsInRow = true });
            var isDirect = Level < GameplayBalance.DirectNextLevelBelow;
            var next = new DialogButton(isDirect ? "Next level" : "Continue", ButtonStyle.Berry, AfterWin) { IsInRow = true };
            if (isDirect && Level < _curves.Count && _curves.Get(Level + 1).IsHard) next.SubLabel = "Tricky · Rewards ×2";
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
            var dialog = new DialogViewModel("Oops, the counter is full!", RibbonStyle.Berry)
            {
                Progress = st.Collected / (float)st.Total,
                ProgressCaption = left / (float)st.Total <= 0.35f
                    ? $"Only {left} item{(left == 1 ? string.Empty : "s")} left!"
                    : $"{left} items left",
            };
            dialog.Items.AddRange(st.Counter.Select(j => _content.Catalog.Get(j.Type).Sprite));
            if (offer == "basket")
            {
                dialog.Buttons.Add(new DialogButton("Call one more customer", ButtonStyle.Green, () => RewardAsync(() =>
                {
                    _session.OpenExtraBasket();
                    _overlay.Toast("One more customer is here!");
                }).Forget()) { Badge = ButtonBadge.Ad });
                dialog.Note = "A third basket opens and takes items straight from the counter.";
            }
            else if (offer == "slot")
            {
                dialog.Buttons.Add(new DialogButton("+1 counter spot", ButtonStyle.Green, () => RewardAsync(() =>
                {
                    _session.ExtendCounter();
                    _overlay.Toast("One more spot on the counter");
                }).Forget()) { Badge = ButtonBadge.Ad });
            }

            if (canUndo) dialog.Buttons.Add(new DialogButton("Undo last move · free", ButtonStyle.Wood, Undo));
            if (_session.IsUnlocked(BoosterIds.Tidy))
            {
                var tidyButton = new DialogButton(tidy > 0 ? $"Tidy Up ({tidy} left)" : "Tidy Up", ButtonStyle.Gold, () =>
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

            dialog.Buttons.Add(new DialogButton("Start the level over", ButtonStyle.WoodSmall, () => StartLevel(Level, null)));
            await _dialogs.ShowAsync(dialog, Ct);
        }
    }
}
