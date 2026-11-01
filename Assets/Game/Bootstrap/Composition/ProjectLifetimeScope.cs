using System.Collections.Generic;
using Game.Features.Ads;
using Game.Features.Album;
using Game.Features.Gameplay;
using Game.Features.Home;
using Game.Features.Reviewers;
using Game.Features.Settings;
using Game.Flow;
using Game.Flow.States;
using Game.Services.Assets;
using Game.Services.Audio;
using Game.Services.Content;
using Game.Services.Flow;
using Game.Services.Haptics;
using Game.Services.Levels;
using Game.Services.Progress;
using Game.Services.Save;
using Game.Services.UI;
using Game.Services.UI.Dialogs;
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

        /// <inheritdoc/>
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponentInNewPrefab(_uiRootPrefab, Lifetime.Singleton).DontDestroyOnLoad();
            builder.RegisterInstance<IReadOnlyList<LevelCurveConfig>>(_curves);

            builder.Register<AssetProvider>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<ISaveStorage>(_ => new FileSaveStorage(Application.persistentDataPath), Lifetime.Singleton);
            builder.Register<SaveService>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<ContentService>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<ProgressService>(Lifetime.Singleton).AsImplementedInterfaces();
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
            builder.RegisterEntryPoint<GameEntryPoint>();
        }
    }
}
