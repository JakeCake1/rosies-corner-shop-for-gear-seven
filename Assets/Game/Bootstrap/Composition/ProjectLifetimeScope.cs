using System.Collections.Generic;
using Game.Features.Ads;
using Game.Features.Album;
using Game.Features.Gameplay;
using Game.Features.Gameplay.Boosters;
using Game.Features.Home;
using Game.Features.Reviewers;
using Game.Features.Settings;
using Game.Flow;
using Game.Flow.States;
using Game.Services.Album;
using Game.Services.Assets;
using Game.Services.Audio;
using Game.Services.Balance;
using Game.Services.Content;
using Game.Services.Experiments;
using Game.Services.Flow;
using Game.Services.Haptics;
using Game.Services.Levels;
using Game.Services.Localization;
using Game.Services.Onboarding;
using Game.Services.Save;
using Game.Services.Settings;
using Game.Services.UI;
using Game.Services.UI.Dialogs;
using Game.Services.Wallet;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Bootstrap.Composition
{
    /// <summary>
    /// Root scope (assigned in VContainerSettings): app-wide services, the UI root, screens and the flow.
    /// </summary>
    public sealed class ProjectLifetimeScope : LifetimeScope
    {
        [SerializeField] private UIRoot _uiRootPrefab;
        [Tooltip("Level curves; the first is the default ('New · 20').")]
        [SerializeField] private List<LevelCurveConfig> _curves = new List<LevelCurveConfig>();
        [Tooltip("Prices, gifts and the levels where boosters, ads and milestones appear.")]
        [SerializeField] private GameBalanceConfig _balance;

        /// <inheritdoc/>
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponentInNewPrefab(_uiRootPrefab, Lifetime.Singleton).DontDestroyOnLoad();
            builder.RegisterInstance<IReadOnlyList<LevelCurveConfig>>(_curves);
            builder.RegisterInstance(_balance);

            builder.Register<AssetProvider>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<ISaveStorage>(_ => new FileSaveStorage(Application.persistentDataPath), Lifetime.Singleton);
            builder.Register<SaveService>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<ContentService>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<LocalizationService>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<ProfileService>(Lifetime.Singleton).AsImplementedInterfaces();
            RegisterProfileChunks(builder);
            builder.Register<LevelCurveService>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<AudioService>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<HapticsService>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<UIService>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<OverlayService>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<DialogService>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<FakeRewardedAdService>(Lifetime.Singleton).AsImplementedInterfaces();

            builder.Register<SettingsScreen>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<ShopNamePrompt>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<AlbumScreen>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<ReviewersScreen>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<HomeProgress>(Lifetime.Singleton);
            builder.Register<HomeScreen>(Lifetime.Singleton).AsSelf().AsImplementedInterfaces();
            builder.Register<StartBoostersPrompt>(Lifetime.Singleton);

            builder.Register<GameplayRequest>(Lifetime.Singleton);
            builder.Register<SceneLoader>(Lifetime.Singleton);
            builder.Register<GameStateMachine>(Lifetime.Singleton);
            builder.Register<GameFlow>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<BootstrapState>(Lifetime.Singleton);
            builder.Register<LoadProgressState>(Lifetime.Singleton);
            builder.Register<HomeState>(Lifetime.Singleton);
            builder.Register<GameplayState>(Lifetime.Singleton);

            builder.RegisterComponentOnNewGameObject<AppLifecycleListener>(Lifetime.Singleton, "[AppLifecycle]")
                .DontDestroyOnLoad();
            builder.RegisterEntryPoint<FlowFailureRecovery>();
            builder.RegisterEntryPoint<GameEntryPoint>();
        }

        private static void RegisterProfileChunks(IContainerBuilder builder)
        {
            // Every owner is also an IProfileChunk; ProfileService receives them all as a collection.
            builder.Register<WalletService>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<LevelProgressService>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<AlbumService>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<BoosterInventory>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<OnboardingService>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<ShopProfile>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<SettingsService>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<ExperimentsService>(Lifetime.Singleton).AsImplementedInterfaces();
        }
    }
}
