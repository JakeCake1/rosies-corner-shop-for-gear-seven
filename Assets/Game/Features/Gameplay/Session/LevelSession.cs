using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Features.Gameplay.Model;
using Game.Services.Audio;
using Game.Services.Content;
using Game.Services.Flow;
using Game.Services.Haptics;
using Game.Services.Levels;
using Game.Services.Progress;
using UniRx;

namespace Game.Features.Gameplay.Session
{
    /// <summary>
    /// One level in play: owns the <see cref="LevelState"/>, applies taps and boosters through
    /// <see cref="LevelRules"/>, schedules basket shipments, keeps the undo snapshot and decides win and loss.
    /// The board view renders <see cref="State"/> whenever <see cref="Changed"/> fires.
    /// </summary>
    /// <remarks>
    /// Scene-scoped (gameplay scope). Delays run on UniTask with a token that is cancelled on restart, undo and
    /// dispose, so a stale shipment never touches a new state (the prototype's <c>epoch</c>).
    /// </remarks>
    public sealed class LevelSession : IDisposable
    {
        private static readonly string[] Praises = { "Lovely!", "Sweet!", "Delicious!", "Well done!", "Perfect!", "Yummy!" };
        private static readonly string[] ComboWords =
        {
            "Neat!", "Tidy shop!", "Rosie is proud!", "On a roll!", "Busy hands!", "What a streak!",
            "The shop is buzzing!", "Look at you go!",
        };

        private readonly ILevelCurveService _curves;
        private readonly IContentService _content;
        private readonly IProgressService _progress;
        private readonly IAudioService _audio;
        private readonly IHapticsService _haptics;
        private readonly Subject<BoardChange> _changed = new Subject<BoardChange>();
        private readonly Subject<int> _refused = new Subject<int>();
        private readonly Subject<ShipNotice> _shipping = new Subject<ShipNotice>();
        private readonly Subject<LevelOutcome> _finished = new Subject<LevelOutcome>();
        private readonly Subject<string> _notice = new Subject<string>();
        private readonly ReactiveProperty<int> _undoLeft = new ReactiveProperty<int>();
        private readonly ReactiveProperty<bool> _canUndo = new ReactiveProperty<bool>();
        private readonly ReactiveProperty<int> _bonusPick = new ReactiveProperty<int>();
        private readonly HashSet<int> _scheduled = new HashSet<int>();
        private CancellationTokenSource _epoch = new CancellationTokenSource();
        private LevelState _snapshot;
        private int _pendingShips;
        private int _combo = -1;
        private int _praiseIndex;
        private int _comboIndex;
        private bool _isWarned;

        /// <summary>
        /// Creates the session; <see cref="Start"/> loads a level.
        /// </summary>
        /// <param name="curves">Active level curve.</param>
        /// <param name="content">Item catalog.</param>
        /// <param name="progress">Coins, album and flags.</param>
        /// <param name="audio">Sound effects.</param>
        /// <param name="haptics">Vibration.</param>
        public LevelSession(ILevelCurveService curves, IContentService content, IProgressService progress,
            IAudioService audio, IHapticsService haptics)
        {
            _curves = curves;
            _content = content;
            _progress = progress;
            _audio = audio;
            _haptics = haptics;
        }

        /// <summary>The level in play; replaced on undo, so don't cache it across changes.</summary>
        public LevelState State { get; private set; }

        /// <summary>Definition of the level in play.</summary>
        public LevelDefinition Definition { get; private set; }

        /// <summary>Emits after every state change with the events to animate.</summary>
        public IObservable<BoardChange> Changed => _changed;

        /// <summary>Emits the tapped jar id when a move is refused because the counter is full.</summary>
        public IObservable<int> Refused => _refused;

        /// <summary>Emits when a full basket starts shipping (napkin, sparks, praise).</summary>
        public IObservable<ShipNotice> Shipping => _shipping;

        /// <summary>Emits once when the level is won or lost (and again after a loss is undone and lost again).</summary>
        public IObservable<LevelOutcome> Finished => _finished;

        /// <summary>
        /// Emits one-time moments the presenter turns into coach tips: "counter", "ship", "stock", "oneLeft".
        /// </summary>
        public IObservable<string> Notices => _notice;

        /// <summary>Free and bought undos left in this level.</summary>
        public IReadOnlyReactiveProperty<int> UndoLeft => _undoLeft;

        /// <summary>True when there is a move to undo.</summary>
        public IReadOnlyReactiveProperty<bool> CanUndo => _canUndo;

        /// <summary>Quick Picks granted by the start booster for this level only.</summary>
        public IReadOnlyReactiveProperty<int> BonusPick => _bonusPick;

        /// <summary>1-based level number.</summary>
        public int LevelNumber => State.LevelNumber;

        /// <summary>True on tricky levels (double rewards, red styling).</summary>
        public bool IsTricky => Definition.IsHard;

        /// <summary>True once won or lost; taps are ignored until a loss is undone or an offer is taken.</summary>
        public bool IsOver { get; private set; }

        /// <summary>True when a shipment is still pending; win and loss wait for it.</summary>
        public bool IsBusy => _pendingShips > 0;

        /// <summary>Whether this level uses the original curve (all boosters unlocked).</summary>
        public bool IsOriginalCurve => _curves.Active.IsOriginal;

        /// <summary>
        /// Starts (or restarts) a level.
        /// </summary>
        /// <param name="level">1-based level number in the active curve.</param>
        /// <param name="startBoosters">Paid start boosters; may be null.</param>
        public void Start(int level, StartBoosters startBoosters)
        {
            ResetEpoch();
            Definition = _curves.Get(level);
            var generator = new LevelGenerator(_content.Catalog.Items, _content.Catalog.Families);
            State = generator.Generate(level, Definition, new HashSet<int>(_progress.Data.Found));
            _snapshot = null;
            _combo = -1;
            _isWarned = false;
            IsOver = false;
            _undoLeft.Value = GameplayBalance.FreeUndosPerLevel;
            _bonusPick.Value = startBoosters != null && startBoosters.HasQuickPick ? 1 : 0;
            _canUndo.Value = false;
            if (startBoosters != null && startBoosters.HasExtraBasket)
            {
                State.UnlockedBaskets = LevelState.MaxBaskets;
                LevelRules.RefillBaskets(State, null);
            }

            _changed.OnNext(new BoardChange(BoardChangeKind.LevelStart));
            _audio.Play(SoundId.Roll);
            ScanDone();
        }

        /// <summary>
        /// Handles a tap on a shelf item (prototype <c>onJarTap</c>).
        /// </summary>
        /// <param name="jarId">Id of the tapped item.</param>
        /// <returns>False when the tap was ignored or refused.</returns>
        public bool Tap(int jarId)
        {
            if (State == null || IsOver) return false;

            var location = LevelRules.VisibleJars(State, false).FirstOrDefault(v => v.Jar.Id == jarId);
            if (location.Jar == null) return false;

            var tapped = location.Jar;
            var snapshot = State.Clone();
            var result = LevelRules.Take(State, location.Block.Id, location.Slot);
            if (!result.IsOk)
            {
                if (result.IsCounterFull)
                {
                    _refused.OnNext(jarId);
                    _audio.Play(SoundId.Fail);
                    _haptics.Pulse(HapticKind.Fail);
                }

                return false;
            }

            SetSnapshot(snapshot);
            if (tapped.IsTool || tapped.IsWrapped) _audio.Play(SoundId.TapSoft);
            else _audio.PlayTap(_content.Catalog.Get(tapped.Type).Silhouette);
            _haptics.Pulse(HapticKind.Tap);
            if (result.Basket == null && !result.IsTool) _combo = -1;

            _changed.OnNext(new BoardChange(BoardChangeKind.Tap, result.Events));
            if (result.Basket == null && !result.IsTool)
            {
                if (State.Counter.Count == State.Capacity - 1 && !_isWarned)
                {
                    _isWarned = true;
                    _notice.OnNext("oneLeft");
                }

                _notice.OnNext("counter");
            }

            if (result.Events.Any(e => e.Type == LevelEventType.ShelfFolded)) _notice.OnNext("stock");
            if (State.Counter.Count < State.Capacity - 1) _isWarned = false;
            ScanDone();
            CheckEnd();
            return true;
        }

        /// <summary>
        /// Takes back the last move if an undo is available.
        /// </summary>
        /// <returns><see cref="UndoResult.Done"/>, or why it could not be done.</returns>
        public UndoResult Undo()
        {
            if (_snapshot == null) return UndoResult.NothingToUndo;
            if (_undoLeft.Value <= 0) return UndoResult.NoUndosLeft;

            _undoLeft.Value--;
            ResetEpoch();
            State = _snapshot;
            SetSnapshot(null);
            IsOver = false;
            _changed.OnNext(new BoardChange(BoardChangeKind.Rebuild));
            _audio.Play(SoundId.Magic);
            ScanDone();
            return UndoResult.Done;
        }

        /// <summary>
        /// Adds bought or rewarded undos to this level.
        /// </summary>
        /// <param name="count">Undos to add.</param>
        public void AddUndos(int count) => _undoLeft.Value += count;

        /// <summary>
        /// Quick Pick: fills the basket closest to done (start-booster picks are spent first).
        /// </summary>
        /// <returns>False when no basket is waiting or no Quick Pick is owned.</returns>
        public bool UseQuickPick()
        {
            if (State == null || IsOver) return false;
            if (_bonusPick.Value <= 0 && _progress.GetBooster(BoosterIds.Pick) <= 0) return false;

            var snapshot = State.Clone();
            var events = new List<LevelEvent>();
            var fromStock = new Dictionary<int, int>();
            if (LevelRules.QuickPick(State, events, fromStock) < 0) return false;

            SetSnapshot(snapshot);
            if (_bonusPick.Value > 0) _bonusPick.Value--;
            else _progress.AddBooster(BoosterIds.Pick, -1);
            _audio.Play(SoundId.Magic);
            _changed.OnNext(new BoardChange(BoardChangeKind.QuickPick, events, fromStock));
            ScanDone();
            return true;
        }

        /// <summary>
        /// Tidy Up: returns up to three counter items to a stockroom. Also usable from the loss popup.
        /// </summary>
        /// <returns>False when the counter is empty or no Tidy Up is owned.</returns>
        public bool UseTidy()
        {
            if (State == null || _progress.GetBooster(BoosterIds.Tidy) <= 0) return false;

            var snapshot = State.Clone();
            var events = new List<LevelEvent>();
            var back = new List<int>();
            if (LevelRules.Tidy(State, events, out var column, back) == 0) return false;

            SetSnapshot(snapshot);
            _progress.AddBooster(BoosterIds.Tidy, -1);
            IsOver = false;
            _audio.Play(SoundId.Magic);
            _changed.OnNext(new BoardChange(BoardChangeKind.Tidy, events, null, back.ToDictionary(id => id, _ => column)));
            CheckEnd();
            return true;
        }

        /// <summary>
        /// Shuffle: mixes the open shelves (original curve only).
        /// </summary>
        /// <returns>False when nothing can move or no Shuffle is owned.</returns>
        public bool UseShuffle()
        {
            if (State == null || IsOver || _progress.GetBooster(BoosterIds.Shuffle) <= 0) return false;

            var snapshot = State.Clone();
            var seed = (uint)(DateTime.UtcNow.Ticks % 100000);
            if (!LevelRules.Shuffle(State, new Mulberry32(seed))) return false;

            SetSnapshot(snapshot);
            _progress.AddBooster(BoosterIds.Shuffle, -1);
            _audio.Play(SoundId.Magic);
            _changed.OnNext(new BoardChange(BoardChangeKind.Shuffle));
            CheckEnd();
            return true;
        }

        /// <summary>
        /// Opens the third basket (already paid for, free, or rewarded); the new customer takes matching counter
        /// items at once. Also the "Call one more customer" loss offer.
        /// </summary>
        public void OpenExtraBasket()
        {
            SetSnapshot(State.Clone());
            State.UnlockedBaskets = LevelState.MaxBaskets;
            var events = new List<LevelEvent>();
            LevelRules.RefillBaskets(State, events);
            IsOver = false;
            _audio.Play(SoundId.Magic);
            _changed.OnNext(new BoardChange(BoardChangeKind.Layout, events));
            ScanDone();
            CheckEnd();
        }

        /// <summary>
        /// The "+1 counter spot" loss offer: the counter grows to six spots for this level.
        /// </summary>
        public void ExtendCounter()
        {
            State.Capacity = GameplayBalance.ExtendedCapacity;
            IsOver = false;
            _changed.OnNext(new BoardChange(BoardChangeKind.Layout));
            CheckEnd();
        }

        /// <summary>
        /// The move the hidden hint highlights (prototype <c>bestHint</c>).
        /// </summary>
        /// <returns>The jar to glow, or null.</returns>
        public Jar BestHint() => State == null || IsOver ? null : LevelRules.BestHint(State);

        /// <summary>
        /// Whether a booster is available at this level (unlock levels, or everything on the original curve).
        /// </summary>
        /// <param name="boosterId">Booster id from <see cref="BoosterIds"/>.</param>
        /// <returns>True when its button is shown.</returns>
        public bool IsUnlocked(string boosterId)
        {
            if (IsOriginalCurve) return true;

            switch (boosterId)
            {
                case BoosterIds.Undo: return LevelNumber >= GameplayBalance.UndoUnlockLevel;
                case BoosterIds.Tidy: return LevelNumber >= GameplayBalance.TidyUnlockLevel;
                case BoosterIds.Pick: return LevelNumber >= GameplayBalance.PickUnlockLevel;
                default: return false;
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _epoch.Cancel();
            _epoch.Dispose();
            _changed.Dispose();
            _refused.Dispose();
            _shipping.Dispose();
            _finished.Dispose();
            _notice.Dispose();
            _undoLeft.Dispose();
            _canUndo.Dispose();
            _bonusPick.Dispose();
        }

        private void SetSnapshot(LevelState snapshot)
        {
            _snapshot = snapshot;
            _canUndo.Value = snapshot != null;
        }

        private void ResetEpoch()
        {
            _epoch.Cancel();
            _epoch.Dispose();
            _epoch = new CancellationTokenSource();
            _scheduled.Clear();
            _pendingShips = 0;
        }

        private void ScanDone()
        {
            for (var slot = 0; slot < LevelState.MaxBaskets; slot++)
            {
                var b = State.Baskets[slot];
                if (b == null || !b.IsDone || !_scheduled.Add(b.Id)) continue;

                ShipAsync(slot, b.Id, _epoch.Token).Forget();
            }
        }

        private async UniTaskVoid ShipAsync(int slot, int basketId, CancellationToken ct)
        {
            _pendingShips++;
            try
            {
                await UniTask.Delay(GameplayBalance.ShipDelayMs, cancellationToken: ct);
                _combo++;
                var word = _combo > 0 ? ComboWord(_combo) : Praises[_praiseIndex++ % Praises.Length];
                _shipping.OnNext(new ShipNotice(slot, basketId, _combo, word));
                _audio.Play(SoundId.Ship, 1f + Math.Min(_combo, 8) * 0.06f);
                _haptics.Pulse(HapticKind.Ship);
                _notice.OnNext("ship");
                await UniTask.Delay(GameplayBalance.ShipLeaveMs, cancellationToken: ct);

                var basket = State.Baskets[slot];
                if (basket == null || basket.Id != basketId)
                {
                    _pendingShips--;
                    return;
                }

                var events = new List<LevelEvent>();
                var type = LevelRules.CompleteBasket(State, slot, events);
                if (type.HasValue)
                {
                    var family = _progress.RegisterFound(type.Value);
                    if (family != null)
                    {
                        _audio.Play(SoundId.Coin);
                        _notice.OnNext("family:" + family);
                    }
                }

                _changed.OnNext(new BoardChange(BoardChangeKind.Ship, events));
                if (events.Any(e => e.Type == LevelEventType.ShelfThawed)) _audio.Play(SoundId.Magic);
                _pendingShips--;
                ScanDone();
                if (LevelRules.IsWon(State))
                {
                    await UniTask.Delay(GameplayBalance.WinDelayMs, cancellationToken: ct);
                    Win();
                }
                else
                {
                    CheckEnd();
                }
            }
            catch (OperationCanceledException)
            {
                // Restart or undo: the shipment belongs to an old state.
            }
        }

        private string ComboWord(int k) =>
            k <= 3 ? ComboWords[k - 1] : ComboWords[3 + _comboIndex++ % (ComboWords.Length - 3)];

        private void CheckEnd()
        {
            if (State == null || IsOver || _pendingShips > 0) return;

            if (LevelRules.IsWon(State))
            {
                Win();
                return;
            }

            if (LevelRules.IsLost(State))
            {
                IsOver = true;
                LoseAsync(_epoch.Token).Forget();
            }
        }

        private async UniTaskVoid LoseAsync(CancellationToken ct)
        {
            try
            {
                await UniTask.Delay(GameplayBalance.LossDelayMs, cancellationToken: ct);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (!IsOver) return;

            _progress.Data.FailedLevels.Add(LevelNumber);
            _progress.NotifyChanged();
            _audio.Play(SoundId.Fail);
            _haptics.Pulse(HapticKind.Fail);
            _finished.OnNext(LevelOutcome.Lost(State.Total - State.Collected));
        }

        private void Win()
        {
            if (IsOver) return;

            IsOver = true;
            var stars = GameplayBalance.StarsFor(State.Peak, LevelNumber);
            var reward = stars * GameplayBalance.CoinsPerStar * (IsTricky ? 2 : 1);
            _progress.CompleteLevel(LevelNumber, stars);
            _progress.AddCoins(reward);
            _audio.Play(SoundId.Win);
            _haptics.Pulse(HapticKind.Win);
            _finished.OnNext(LevelOutcome.Won(stars, reward));
        }
    }
}
