using System;
using System.Threading;

namespace Game.Features.Gameplay
{
    /// <summary>
    /// Cancellation scopes of the gameplay scene: one for the whole scene and one for the level in play.
    /// </summary>
    /// <remarks>
    /// Owned and disposed by <see cref="GameplayPresenter"/>. The level token is cancelled on restart, replay, the
    /// next level and scene unload, so delayed tips, gifts, result dialogs and ads of an old level never show over a
    /// new one.
    /// </remarks>
    public sealed class LevelLifetime : IDisposable
    {
        private readonly CancellationTokenSource _scene = new CancellationTokenSource();
        private CancellationTokenSource _level;
        // Cached: reading Token from a disposed source throws instead of reporting cancellation.
        private readonly CancellationToken _sceneToken;

        /// <summary>Cancelled when the gameplay scene unloads.</summary>
        public CancellationToken SceneToken => _sceneToken;

        /// <summary>Cancelled when the level in play ends (restart, replay, next level) or the scene unloads.</summary>
        public CancellationToken LevelToken { get; private set; }

        /// <summary>
        /// Creates the scopes; the level scope starts linked to the scene scope.
        /// </summary>
        public LevelLifetime()
        {
            _sceneToken = _scene.Token;
            Reset();
        }

        /// <summary>
        /// Cancels everything started for the previous level and opens a new level scope. Called when a level starts.
        /// </summary>
        public void Reset()
        {
            if (_level != null)
            {
                _level.Cancel();
                _level.Dispose();
            }

            _level = CancellationTokenSource.CreateLinkedTokenSource(_sceneToken);
            LevelToken = _level.Token;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _level.Cancel();
            _level.Dispose();
            _scene.Cancel();
            _scene.Dispose();
        }
    }
}
