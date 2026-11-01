using Game.Features.Gameplay;
using Game.Features.Gameplay.Session;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;
using VContainer.Unity;

namespace Game.Bootstrap.Composition
{
    /// <summary>
    /// Scope of the gameplay scene. Its services, and the gameplay screen elements, are disposed when the scene
    /// unloads.
    /// </summary>
    public sealed class GameplayLifetimeScope : LifetimeScope
    {
        [SerializeField] private GameplayArt _art;

        [Header("Layouts")]
        [SerializeField] private VisualTreeAsset _screenLayout;
        [SerializeField] private VisualTreeAsset _shelfLayout;
        [SerializeField] private VisualTreeAsset _basketLayout;
        [SerializeField] private VisualTreeAsset _boosterLayout;
        [SerializeField] private VisualTreeAsset _bannerLayout;

        /// <inheritdoc/>
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterInstance(_art);
            builder.RegisterInstance(new GameplayLayouts(_screenLayout, _shelfLayout, _basketLayout, _boosterLayout, _bannerLayout));
            builder.Register<GameplayScreen>(Lifetime.Singleton);
            builder.Register<LevelSession>(Lifetime.Singleton);
            builder.Register<GameplayServices>(Lifetime.Singleton);
            builder.RegisterEntryPoint<GameplayPresenter>();
        }
    }
}
