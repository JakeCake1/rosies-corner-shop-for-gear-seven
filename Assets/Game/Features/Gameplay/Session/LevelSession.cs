using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Features.Gameplay.Boosters;
using Game.Features.Gameplay.Model;
using Game.Services.Album;
using Game.Services.Audio;
using Game.Services.Balance;
using Game.Services.Content;
using Game.Services.Flow;
using Game.Services.Haptics;
using Game.Services.Levels;
using Game.Services.Wallet;
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
    /// dispose, and on <see cref="Prepare"/> and <see cref="Start"/> (the sprite load between them), so a stale
    /// shipment never touches a new state (the prototype's <c>epoch</c>).
    /// </remarks>
    public sealed class LevelSession : IDisposable
    {
        private static readonly string[] Praises =
        {
            GameplayTexts.Praise1, GameplayTexts.Praise2, GameplayTexts.Praise3, GameplayTexts.Praise4,
            GameplayTexts.Praise5, GameplayTexts.Praise6,
        };
        private static readonly string[] ComboWords =
        {
            GameplayTexts.Combo1, GameplayTexts.Combo2, GameplayTexts.Combo3, GameplayTexts.Combo4,
            GameplayTexts.Combo5, GameplayTexts.Combo6, GameplayTexts.Combo7, GameplayTexts.Combo8,
        };

        private readonly ILevelCurveService _curves;
        private readonly IContentService _content;
        private readonly IWalletService _wallet;
        private readonly IBoosterInventory _boosters;
        private readonly IAlbumService _album;
        private readonly ILevelProgressService _levels;
        private readonly IAudioService _audio;
        private readonly IHapticsService _haptics;
        private readonly GameBalanceConfig _balance;
        private readonly IShuffleSeedSource _shuffleSeeds;
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
        private LevelState _prepared;
        private LevelDefinition _preparedDefinition;
        private int _pendingShips;
        private int _lossId;
        private int _combo = -1;
        private int _praiseIndex;
        private int _comboIndex;
        private bool _isWarned;
        private bool _hasGrantedWinReward;

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
        /// Creates the session; <see cref="Start"/> loads a level.
        /// </summary>
        /// <param name="curves">Active level curve.</param>
        /// <param name="content">Item catalog.</param>
        /// <param name="wallet">Level rewards.</param>
        /// <param name="boosters">Owned boosters.</param>
        /// <param name="album">Found items.</param>
        /// <param name="levels">Stars and lost levels.</param>
        /// <param name="audio">Sound effects.</param>
        /// <param name="haptics">Vibration.</param>
        /// <param name="balance">Free undos, extended capacity, booster unlock levels and coin rewards.</param>
        /// <param name="shuffleSeeds">Seed of every Shuffle use.</param>
        public LevelSession(ILevelCurveService curves, IContentService content, IWalletService wallet,
            IBoosterInventory boosters, IAlbumService album, ILevelProgressService levels, IAudioService audio,
            IHapticsService haptics, GameBalanceConfig balance, IShuffleSeedSource shuffleSeeds)
        {
            _curves = curves;
            _content = content;
            _wallet = wallet;
            _boosters = boosters;
            _album = album;
            _levels = levels;
            _audio = audio;
            _haptics = haptics;
            _balance = balance;
            _shuffleSeeds = shuffleSeeds;
        }

        /// <summary>
        /// Generates a level without showing it and retires the level in play: its pending shipments and loss delay
        /// are cancelled and it ignores taps (<see cref="IsOver"/>), so nothing of it can pay a win or block a basket
        /// while the sprites load. Call <see cref="Start"/> next, after the sprites of
        /// <see cref="LevelItemTypes.Of"/> the returned state are loaded.
        /// </summary>
        /// <param name="level">1-based level number in the active curve.</param>
        /// <returns>The generated level.</returns>
        public LevelState Prepare(int level)
        {
            ResetEpoch();
            IsOver = true;
            _preparedDefinition = _curves.Get(level);
            var generator = new LevelGenerator(_content.Catalog.Items, _content.Catalog.Families);
            _prepared = generator.Generate(level, _preparedDefinition, new HashSet<int>(_album.Found));
            return _prepared;
        }

        /// <summary>
        /// Starts (or restarts) the level generated by <see cref="Prepare"/>.
        /// </summary>
        /// <param name="startBoosters">Paid start boosters; may be null.</param>
        /// <exception cref="InvalidOperationException">No level was prepared.</exception>
        public void Start(StartBoosters startBoosters)
        {
            if (_prepared == null)
            {
                throw new InvalidOperationException($"Call {nameof(Prepare)} before {nameof(Start)}.");
            }

            // Anything scheduled since Prepare (a Tidy Up or extra basket used during the load) belongs to the old
            // level; basket ids restart at 1 every level, so a leftover id would block the new level's basket.
            ResetEpoch();
            _hasGrantedWinReward = false;
            Definition = _preparedDefinition;
            State = _prepared;
            _prepared = null;
            _preparedDefinition = null;
            _snapshot = null;
            _combo = -1;
            _isWarned = false;
            IsOver = false;
            _undoLeft.Value = _balance.FreeUndosPerLevel;
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
            if (State == null || IsOver)
            {
                return false;
            }

            var location = LevelRules.VisibleJars(State, false).FirstOrDefault(v => v.Jar.Id == jarId);
            if (location.Jar == null)
            {
                return false;
            }

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
            if (tapped.IsTool || tapped.IsWrapped)
            {
                _audio.Play(SoundId.TapRound);
            }
            else
            {
                _audio.PlayTap(_content.Catalog.Get(tapped.Type).Silhouette);
            }
            _haptics.Pulse(HapticKind.Tap);
            if (result.Basket == null && !result.IsTool)
            {
                _combo = -1;
            }

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

            if (result.Events.Any(e => e.Type == LevelEventType.ShelfFolded))
            {
                _notice.OnNext("stock");
            }
            if (State.Counter.Count < State.Capacity - 1)
            {
                _isWarned = false;
            }
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
            if (_snapshot == null)
            {
                return UndoResult.NothingToUndo;
            }
            if (_undoLeft.Value <= 0)
            {
                return UndoResult.NoUndosLeft;
            }

            _undoLeft.Value--;
            ResetEpoch();
            // Purchases belong to the level attempt, not to the move being undone.
            _snapshot.Capacity = Math.Max(_snapshot.Capacity, State.Capacity);
            _snapshot.UnlockedBaskets = Math.Max(_snapshot.UnlockedBaskets, State.UnlockedBaskets);
            State = _snapshot;
            LevelRules.RefillBaskets(State, null);
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
            if (State == null || IsOver)
            {
                return false;
            }
            if (_bonusPick.Value <= 0 && _boosters.Get(BoosterIds.Pick) <= 0)
            {
                return false;
            }

            var snapshot = State.Clone();
            var events = new List<LevelEvent>();
            var fromStock = new Dictionary<int, int>();
            if (LevelRules.QuickPick(State, events, fromStock) < 0)
            {
                return false;
            }

            SetSnapshot(snapshot);
            if (_bonusPick.Value > 0)
            {
                _bonusPick.Value--;
            }
            else
            {
                _boosters.Add(BoosterIds.Pick, -1);
            }
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
            if (State == null || _boosters.Get(BoosterIds.Tidy) <= 0)
            {
                return false;
            }

            var snapshot = State.Clone();
            var events = new List<LevelEvent>();
            var back = new List<int>();
            if (LevelRules.Tidy(State, events, out var column, back) == 0)
            {
                return false;
            }

            SetSnapshot(snapshot);
            _boosters.Add(BoosterIds.Tidy, -1);
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
            if (State == null || IsOver || _boosters.Get(BoosterIds.Shuffle) <= 0)
            {
                return false;
            }

            var snapshot = State.Clone();
            if (!LevelRules.Shuffle(State, new Mulberry32(_shuffleSeeds.NextSeed())))
            {
                return false;
            }

            SetSnapshot(snapshot);
            _boosters.Add(BoosterIds.Shuffle, -1);
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
            State.Capacity = _balance.ExtendedCapacity;
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
            if (IsOriginalCurve)
            {
                return true;
            }

            switch (boosterId)
            {
                case BoosterIds.Undo: return LevelNumber >= _balance.UndoUnlockLevel;
                case BoosterIds.Tidy: return LevelNumber >= _balance.TidyUnlockLevel;
                case BoosterIds.Pick: return LevelNumber >= _balance.PickUnlockLevel;
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
                if (b == null || !b.IsDone || !_scheduled.Add(b.Id))
                {
                    continue;
                }

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
                _audio.PlayShip(_combo);
                _haptics.Pulse(HapticKind.Ship);
                _notice.OnNext("ship");
                await UniTask.Delay(GameplayBalance.ShipLeaveMs, cancellationToken: ct);

                var basket = State.Baskets[slot];
                if (basket == null || basket.Id != basketId)
                {
                    _pendingShips--;
                    CheckEnd();
                    return;
                }

                var events = new List<LevelEvent>();
                var type = LevelRules.CompleteBasket(State, slot, events);
                if (type.HasValue)
                {
                    var family = _album.RegisterFound(type.Value);
                    if (family != null)
                    {
                        _audio.Play(SoundId.Coin);
                        _notice.OnNext("family:" + family);
                    }
                }

                _changed.OnNext(new BoardChange(BoardChangeKind.Ship, events));
                if (events.Any(e => e.Type == LevelEventType.ShelfThawed))
                {
                    _audio.Play(SoundId.Magic);
                }
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
            if (State == null || IsOver || _pendingShips > 0)
            {
                return;
            }

            if (LevelRules.IsWon(State))
            {
                Win();
                return;
            }

            if (LevelRules.IsLost(State))
            {
                IsOver = true;
                LoseAsync(_epoch.Token, ++_lossId).Forget();
            }
        }

        private async UniTaskVoid LoseAsync(CancellationToken ct, int lossId)
        {
            try
            {
                await UniTask.Delay(GameplayBalance.LossDelayMs, cancellationToken: ct);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            // A Tidy Up during the delay reopens the level; if it is lost again a newer delay owns the outcome.
            if (!IsOver || lossId != _lossId)
            {
                return;
            }

            _levels.MarkFailed(LevelNumber);
            _audio.Play(SoundId.Fail);
            _haptics.Pulse(HapticKind.Fail);
            _finished.OnNext(LevelOutcome.Lost(State.Total - State.Collected));
        }

        private void Win()
        {
            if (IsOver)
            {
                return;
            }

            IsOver = true;
            var stars = GameplayBalance.StarsFor(State.Peak, LevelNumber);
            var reward = stars * _balance.CoinsPerStar * (IsTricky ? 2 : 1);
            if (!_hasGrantedWinReward)
            {
                _hasGrantedWinReward = true;
                _levels.Complete(LevelNumber, stars);
                _wallet.Add(reward);
            }
            _audio.Play(SoundId.Win);
            _haptics.Pulse(HapticKind.Win);
            _finished.OnNext(LevelOutcome.Won(stars, reward));
        }
    }
}
