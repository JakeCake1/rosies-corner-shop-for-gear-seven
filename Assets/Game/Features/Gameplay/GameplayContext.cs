using System.Threading;
using Game.Features.Gameplay.Board;
using Game.Features.Gameplay.Session;

namespace Game.Features.Gameplay
{
    /// <summary>
    /// What every part of the gameplay UI shares: the session, the screen, the art, the services and the level
    /// lifetime. Created once per scene by <see cref="GameplayPresenter"/> and passed to the parts it builds.
    /// </summary>
    public sealed class GameplayContext
    {
        private readonly LevelLifetime _lifetime;

        /// <summary>Level rules and state; <see cref="LevelSession.State"/> is null until the first level starts.</summary>
        public LevelSession Session { get; }

        /// <summary>The gameplay screen.</summary>
        public GameplayScreen Screen { get; }

        /// <summary>The board of <see cref="Screen"/>: shelves, counter and baskets.</summary>
        public BoardView Board => Screen.Board;

        /// <summary>Booster and basket sprites.</summary>
        public GameplayArt Art { get; }

        /// <summary>Shared services used by the gameplay UI.</summary>
        public GameplayServices Services { get; }

        /// <summary>
        /// Cancelled when the level in play ends or the scene unloads; every delayed tip, gift, dialog and ad of a
        /// level awaits with it.
        /// </summary>
        public CancellationToken LevelToken => _lifetime.LevelToken;

        /// <summary>Number of the level in play (1-based).</summary>
        public int Level => Session.LevelNumber;

        /// <summary>True from the level where rewarded-ad buttons appear in dialogs.</summary>
        public bool HasAds => Level >= GameplayBalance.AdsFromLevel;

        /// <summary>
        /// Creates the context.
        /// </summary>
        /// <param name="session">Level rules and state of the scene.</param>
        /// <param name="screen">The gameplay screen: board, HUD, booster bar and banner.</param>
        /// <param name="art">Booster and basket sprites for buttons and dialogs.</param>
        /// <param name="services">Shared services used by the gameplay UI.</param>
        /// <param name="lifetime">Scene and level cancellation scopes; owned by the presenter.</param>
        public GameplayContext(LevelSession session, GameplayScreen screen, GameplayArt art,
            GameplayServices services, LevelLifetime lifetime)
        {
            Session = session;
            Screen = screen;
            Art = art;
            Services = services;
            _lifetime = lifetime;
        }
    }
}
