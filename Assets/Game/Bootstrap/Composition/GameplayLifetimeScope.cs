using Game.Features.Gameplay;
using Game.Features.Gameplay.Board;
using Game.Features.Gameplay.Session;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Bootstrap.Composition
{
    /// <summary>
    /// Scope of the gameplay scene; its services are disposed when the scene unloads.
    /// </summary>
    public sealed class GameplayLifetimeScope : LifetimeScope
    {
        [SerializeField] private BoardView _board;
        [SerializeField] private GameplayViews _views;
        [SerializeField] private GameplayArt _art;

        /// <inheritdoc/>
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(_board);
            builder.RegisterComponent(_views);
            builder.RegisterInstance(_art);
            builder.Register<LevelSession>(Lifetime.Singleton);
            builder.Register<GameplayServices>(Lifetime.Singleton);
            builder.RegisterEntryPoint<GameplayPresenter>();
        }
    }
}
