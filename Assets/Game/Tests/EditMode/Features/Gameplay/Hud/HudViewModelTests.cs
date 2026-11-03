using System.Linq;
using Game.Features.Gameplay;
using Game.Features.Gameplay.Hud;
using Game.Features.Gameplay.Session;
using Game.Services.Balance;
using Game.Services.Content;
using Game.Services.Levels;
using Game.Tests.EditMode.Features.Gameplay.Session;
using Game.Tests.EditMode.Services.Content;
using Game.Tests.EditMode.Services.Localization;
using NUnit.Framework;
using UniRx;
using UnityEngine;

namespace Game.Tests.EditMode.Features.Gameplay.Hud
{
    public sealed class HudViewModelTests
    {
        private ItemCatalog _catalog;
        private LevelCurveConfig _curve;
        private GameBalanceConfig _balance;
        private FakeWalletService _wallet;
        private FakeBoosterInventory _boosters;
        private LevelSession _session;
        private HudViewModel _viewModel;

        [SetUp]
        public void SetUp()
        {
            _catalog = ScriptableObject.CreateInstance<ItemCatalog>();
            _catalog.SetContent(TestContent.Items.ToList(), TestContent.Families.ToList());
            _curve = ScriptableObject.CreateInstance<LevelCurveConfig>();
            _balance = ScriptableObject.CreateInstance<GameBalanceConfig>();
            _wallet = new FakeWalletService();
            _boosters = new FakeBoosterInventory();
        }

        [TearDown]
        public void TearDown()
        {
            _viewModel?.Dispose();
            _session?.Dispose();
            Object.DestroyImmediate(_catalog);
            Object.DestroyImmediate(_curve);
            Object.DestroyImmediate(_balance);
        }

        [Test]
        public void Constructor_WhenNoLevelStarted_KeepsDefaults()
        {
            CreateViewModel(CreateDefinition(false));

            Assert.That(_viewModel.LevelNumber.Value, Is.EqualTo("1"));
            Assert.That(_viewModel.ProgressText.Value, Is.EqualTo("0 / 0"));
            Assert.That(_viewModel.Progress.Value, Is.EqualTo(0f));
            Assert.That(_viewModel.IsTricky.Value, Is.False);
            Assert.That(_viewModel.BadgeTitle.Value, Is.Null);
        }

        [Test]
        public void Coins_FollowsWallet()
        {
            CreateViewModel(CreateDefinition(false));

            _wallet.Add(40);

            Assert.That(_viewModel.Coins.Value, Is.EqualTo(40));
        }

        [Test]
        public void LevelStart_ShowsLevelBadgeAndEmptyProgress()
        {
            CreateViewModel(CreateDefinition(false));

            StartLevel(1);

            Assert.That(_viewModel.BadgeTitle.Value, Is.EqualTo(GameplayTexts.BadgeLevel));
            Assert.That(_viewModel.IsTricky.Value, Is.False);
            Assert.That(_viewModel.LevelNumber.Value, Is.EqualTo("1"));
            Assert.That(_viewModel.ProgressText.Value, Is.EqualTo($"0 / {_session.State.Total}"));
            Assert.That(_viewModel.Progress.Value, Is.EqualTo(0f));
        }

        [Test]
        public void LevelStart_WhenLevelIsHard_ShowsTrickyBadge()
        {
            CreateViewModel(CreateDefinition(true));

            StartLevel(7);

            Assert.That(_viewModel.IsTricky.Value, Is.True);
            Assert.That(_viewModel.BadgeTitle.Value, Is.EqualTo(GameplayTexts.BadgeTricky));
            Assert.That(_viewModel.LevelNumber.Value, Is.EqualTo("7"));
        }

        [Test]
        public void Constructor_WhenLevelAlreadyStarted_ShowsItImmediately()
        {
            CreateViewModel(CreateDefinition(false), isStarted: true);

            Assert.That(_viewModel.BadgeTitle.Value, Is.EqualTo(GameplayTexts.BadgeLevel));
            Assert.That(_viewModel.ProgressText.Value, Does.StartWith("0 / "));
        }

        [Test]
        public void OpenSettings_EmitsRequest()
        {
            CreateViewModel(CreateDefinition(false));
            var requests = 0;
            using (_viewModel.SettingsRequested.Subscribe(_ => requests++))
            {
                _viewModel.OpenSettings();
                _viewModel.OpenSettings();
            }

            Assert.That(requests, Is.EqualTo(2));
        }

        private static LevelDefinition CreateDefinition(bool isHard)
        {
            var d = TestContent.Curve("levels_new_20.json")[0];
            return new LevelDefinition(d.Seed, d.JarsPerRow, d.VisibleRows, d.Sizes, d.Types, d.PerType, d.Locks,
                d.Frozen, d.Thaw, d.Boarded, d.HammerMode, d.Wrap, d.Deep, d.Family, d.Look, isHard, d.TipKey, d.Capacity,
                d.Baskets);
        }

        private void CreateViewModel(LevelDefinition definition, bool isStarted = false)
        {
            _session = new LevelSession(new FakeLevelCurveService(_curve, definition),
                new FakeContentService(_catalog), _wallet, _boosters, new FakeAlbumService(),
                new FakeLevelProgressService(), new FakeAudioService(), new FakeHapticsService(), _balance,
                new FixedShuffleSeedSource(1));
            if (isStarted)
            {
                StartLevel(1);
            }

            _viewModel = new HudViewModel(_session, _wallet, new FakeLocalizationService());
        }

        private void StartLevel(int level)
        {
            _session.Prepare(level);
            _session.Start(null);
        }
    }
}
