using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Features.Gameplay.Model;
using Game.Features.Gameplay.Session;
using Game.Services.Audio;
using Game.Services.Balance;
using Game.Services.Content;
using Game.Services.Flow;
using Game.Services.Levels;
using NUnit.Framework;
using Game.Tests.EditMode.Services.Content;
using UniRx;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.EditMode.Features.Gameplay.Session
{
    public sealed class LevelSessionTests
    {
        private const int LossTimeoutMs = 3000;

        private ItemCatalog _catalog;
        private LevelCurveConfig _curve;
        private GameBalanceConfig _balance;
        private FakeBoosterInventory _boosters;
        private FakeWalletService _wallet;
        private FakeLevelProgressService _levels;
        private FakeAudioService _audio;
        private FixedShuffleSeedSource _seeds;
        private LevelSession _session;

        [SetUp]
        public void SetUp()
        {
            _catalog = ScriptableObject.CreateInstance<ItemCatalog>();
            _catalog.SetContent(TestContent.Items, TestContent.Families.ToList());
            _curve = ScriptableObject.CreateInstance<LevelCurveConfig>();
            _curve.SetContent("new", "label", "subtitle", false, new List<LevelDefinition>());
            _balance = ScriptableObject.CreateInstance<GameBalanceConfig>();
            _boosters = new FakeBoosterInventory();
            _wallet = new FakeWalletService();
            _levels = new FakeLevelProgressService();
            _audio = new FakeAudioService();
            _seeds = new FixedShuffleSeedSource(7);
            var definition = TestContent.Curve("levels_new_20.json")[0];
            _session = new LevelSession(new FakeLevelCurveService(_curve, definition), new FakeContentService(_catalog),
                _wallet, _boosters, new FakeAlbumService(), _levels, _audio, new FakeHapticsService(), _balance, _seeds);
        }

        [TearDown]
        public void TearDown()
        {
            _session.Dispose();
            UnityEngine.Object.DestroyImmediate(_catalog);
            UnityEngine.Object.DestroyImmediate(_curve);
            UnityEngine.Object.DestroyImmediate(_balance);
        }

        [Test]
        public void Start_WithoutPrepare_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => _session.Start(null));
        }

        [Test]
        public void Start_AfterPrepare_EmitsLevelStartAndGrantsFreeUndos()
        {
            var kinds = new List<BoardChangeKind>();
            using (_session.Changed.Subscribe(change => kinds.Add(change.Kind)))
            {
                StartLevel();
            }

            Assert.That(kinds, Is.EqualTo(new[] { BoardChangeKind.LevelStart }));
            Assert.That(_session.UndoLeft.Value, Is.EqualTo(_balance.FreeUndosPerLevel));
            Assert.That(_session.CanUndo.Value, Is.False);
            Assert.That(_session.IsOver, Is.False);
        }

        [Test]
        public void Tap_WhenBasketWantsItem_CollectsItAndEnablesUndo()
        {
            StartLevel();
            var wanted = FindWantedJar();

            var isTaken = _session.Tap(wanted.Id);

            Assert.That(isTaken, Is.True);
            Assert.That(_session.State.Moves, Is.EqualTo(1));
            Assert.That(_session.CanUndo.Value, Is.True);
        }

        [Test]
        public void Tap_WhenJarIsUnknown_ReturnsFalseWithoutChanges()
        {
            StartLevel();

            var isTaken = _session.Tap(-12345);

            Assert.That(isTaken, Is.False);
            Assert.That(_session.State.Moves, Is.EqualTo(0));
        }

        [Test]
        public void Tap_WhenCounterFullAndNoBasketWantsItem_EmitsRefusedAndKeepsState()
        {
            StartLevel();
            var unwanted = FindUnwantedJar();
            FillCounter(unwanted.Type);
            var refused = new List<int>();
            bool isTaken;
            using (_session.Refused.Subscribe(refused.Add))
            {
                isTaken = _session.Tap(unwanted.Id);
            }

            Assert.That(isTaken, Is.False);
            Assert.That(refused, Is.EqualTo(new[] { unwanted.Id }));
            Assert.That(_session.State.Moves, Is.EqualTo(0));
            Assert.That(_audio.Played, Does.Contain(SoundId.Fail));
        }

        [Test]
        public void Undo_WhenNothingWasDone_ReportsNothingToUndo()
        {
            StartLevel();

            Assert.That(_session.Undo(), Is.EqualTo(UndoResult.NothingToUndo));
        }

        [Test]
        public void Undo_AfterTap_RestoresMoveAndSpendsAnUndo()
        {
            StartLevel();
            _session.Tap(FindWantedJar().Id);

            var result = _session.Undo();

            Assert.That(result, Is.EqualTo(UndoResult.Done));
            Assert.That(_session.State.Moves, Is.EqualTo(0));
            Assert.That(_session.UndoLeft.Value, Is.EqualTo(_balance.FreeUndosPerLevel - 1));
            Assert.That(_session.CanUndo.Value, Is.False);
        }

        [Test]
        public void Undo_WhenNoUndosLeft_KeepsTheMove()
        {
            StartLevel();
            _session.Tap(FindWantedJar().Id);
            _session.AddUndos(-_session.UndoLeft.Value);

            var result = _session.Undo();

            Assert.That(result, Is.EqualTo(UndoResult.NoUndosLeft));
            Assert.That(_session.State.Moves, Is.EqualTo(1));
        }

        [Test]
        public void UseQuickPick_WhenNoneOwned_ReturnsFalse()
        {
            StartLevel();

            Assert.That(_session.UseQuickPick(), Is.False);
        }

        [Test]
        public void UseQuickPick_WhenOwned_SpendsOneAndEnablesUndo()
        {
            StartLevel();
            _boosters.Add(BoosterIds.Pick, 2);

            var isUsed = _session.UseQuickPick();

            Assert.That(isUsed, Is.True);
            Assert.That(_boosters.Get(BoosterIds.Pick), Is.EqualTo(1));
            Assert.That(_session.CanUndo.Value, Is.True);
        }

        [Test]
        public void UseQuickPick_WhenStartBoosterGrantsPick_SpendsTheBonusFirst()
        {
            _session.Prepare(1);
            _session.Start(new StartBoosters(false, true));
            _boosters.Add(BoosterIds.Pick, 1);

            _session.UseQuickPick();

            Assert.That(_session.BonusPick.Value, Is.EqualTo(0));
            Assert.That(_boosters.Get(BoosterIds.Pick), Is.EqualTo(1));
        }

        [Test]
        public void UseTidy_WhenCounterHoldsItems_ReturnsThemAndSpendsOne()
        {
            StartLevel();
            _boosters.Add(BoosterIds.Tidy, 1);
            _session.Tap(FindUnwantedJar().Id);

            var isUsed = _session.UseTidy();

            Assert.That(isUsed, Is.True);
            Assert.That(_boosters.Get(BoosterIds.Tidy), Is.EqualTo(0));
            Assert.That(_session.State.Counter, Is.Empty);
        }

        [Test]
        public void UseTidy_WhenNoneOwned_ReturnsFalse()
        {
            StartLevel();
            _session.Tap(FindUnwantedJar().Id);

            Assert.That(_session.UseTidy(), Is.False);
            Assert.That(_session.State.Counter, Is.Not.Empty);
        }

        [Test]
        public void UseShuffle_WhenOwned_AsksForOneSeedAndSpendsOne()
        {
            StartLevel();
            _boosters.Add(BoosterIds.Shuffle, 1);

            var isUsed = _session.UseShuffle();

            Assert.That(isUsed, Is.True);
            Assert.That(_seeds.Requests, Is.EqualTo(1));
            Assert.That(_boosters.Get(BoosterIds.Shuffle), Is.EqualTo(0));
        }

        [Test]
        public void UseShuffle_WhenNoneOwned_DoesNotAskForSeed()
        {
            StartLevel();

            Assert.That(_session.UseShuffle(), Is.False);
            Assert.That(_seeds.Requests, Is.EqualTo(0));
        }

        [Test]
        public void ExtendCounter_RaisesCapacityToConfiguredValue()
        {
            StartLevel();

            _session.ExtendCounter();

            Assert.That(_session.State.Capacity, Is.EqualTo(_balance.ExtendedCapacity));
        }

        [Test]
        public void OpenExtraBasket_UnlocksEveryBasketSlot()
        {
            StartLevel();

            _session.OpenExtraBasket();

            Assert.That(_session.State.UnlockedBaskets, Is.EqualTo(LevelState.MaxBaskets));
        }

        [Test]
        public void Finish_WhenEverythingIsCollected_WinsAndPaysStars()
        {
            StartLevel();
            var outcomes = new List<LevelOutcome>();
            using (_session.Finished.Subscribe(outcomes.Add))
            {
                _session.State.Collected = _session.State.Total;

                _session.ExtendCounter();
            }

            Assert.That(outcomes, Has.Count.EqualTo(1));
            Assert.That(outcomes[0].IsWon, Is.True);
            Assert.That(_session.IsOver, Is.True);
            Assert.That(outcomes[0].Reward, Is.GreaterThan(0));
            Assert.That(_wallet.Coins.Value, Is.EqualTo(outcomes[0].Reward));
            Assert.That(_levels.IsCompleted(1), Is.True);
        }

        [Test]
        public void Finish_WhenWinIsReportedTwice_PaysTheRewardOnce()
        {
            StartLevel();
            _session.State.Collected = _session.State.Total;
            _session.ExtendCounter();
            var paid = _wallet.Coins.Value;

            _session.ExtendCounter();

            Assert.That(_wallet.Coins.Value, Is.EqualTo(paid));
        }

        [Test]
        public void Tap_WhenLevelIsOver_IsIgnored()
        {
            StartLevel();
            _session.State.Collected = _session.State.Total;
            _session.ExtendCounter();

            Assert.That(_session.Tap(FindWantedJar().Id), Is.False);
        }

        [Test]
        public void Tap_BetweenPrepareAndStart_IsIgnored()
        {
            StartLevel();
            var wanted = FindWantedJar();

            _session.Prepare(1);

            Assert.That(_session.IsOver, Is.True);
            Assert.That(_session.Tap(wanted.Id), Is.False);
            Assert.That(_session.State.Moves, Is.EqualTo(0));
        }

        [Test]
        public void Start_AfterPrepare_ReopensTheLevel()
        {
            StartLevel();
            _session.Prepare(1);

            _session.Start(null);

            Assert.That(_session.IsOver, Is.False);
            Assert.That(_session.Tap(FindWantedJar().Id), Is.True);
        }

        [UnityTest]
        public IEnumerator Finish_WhenNoMoveCanHelp_LosesAndMarksLevelFailed() => UniTask.ToCoroutine(async () =>
        {
            StartLevel();
            _session.ExtendCounter();
            foreach (var basket in _session.State.Baskets.Where(b => b != null))
            {
                basket.Type = int.MaxValue;
            }

            FillCounter(int.MaxValue - 1);
            using var timeout = new CancellationTokenSource(LossTimeoutMs);
            var outcome = _session.Finished.ToUniTask(useFirstValue: true, cancellationToken: timeout.Token);

            _session.ExtendCounter();

            var result = await outcome;
            Assert.That(result.IsWon, Is.False);
            Assert.That(_levels.HasFailed(1), Is.True);
        });

        [TestCase(BoosterIds.Undo, 1, false)]
        [TestCase(BoosterIds.Undo, 2, true)]
        [TestCase(BoosterIds.Tidy, 3, false)]
        [TestCase(BoosterIds.Tidy, 4, true)]
        [TestCase(BoosterIds.Pick, 5, false)]
        [TestCase(BoosterIds.Pick, 6, true)]
        [TestCase(BoosterIds.Shuffle, 100, false)]
        public void IsUnlocked_OnNewCurve_FollowsUnlockLevels(string boosterId, int level, bool isExpected)
        {
            StartLevel();
            _session.State.LevelNumber = level;

            Assert.That(_session.IsUnlocked(boosterId), Is.EqualTo(isExpected));
        }

        private void StartLevel()
        {
            _session.Prepare(1);
            _session.Start(null);
        }

        private Jar FindWantedJar()
        {
            var basket = _session.State.Baskets.First(b => b != null);
            return LevelRules.VisibleJars(_session.State, false).First(v => v.Jar.Type == basket.Type).Jar;
        }

        private Jar FindUnwantedJar() =>
            LevelRules.VisibleJars(_session.State, false)
                .First(v => !v.Jar.IsTool && LevelRules.MatchBasket(_session.State, v.Jar.Type) == null).Jar;

        private void FillCounter(int type)
        {
            while (_session.State.Counter.Count < _session.State.Capacity)
            {
                _session.State.Counter.Add(new Jar { Id = 1000 + _session.State.Counter.Count, Type = type });
            }
        }
    }
}
