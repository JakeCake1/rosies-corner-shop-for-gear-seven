namespace Game.Services.Pooling
{
    /// <summary>
    /// A pooled object that resets itself when taken from or returned to its pool.
    /// </summary>
    public interface IPoolable
    {
        /// <summary>
        /// Called by the pool right after the object is taken; per-spawn data is passed separately.
        /// </summary>
        void OnSpawned();

        /// <summary>
        /// Called by the pool before the object goes back: kill tweens, drop references, reset state.
        /// </summary>
        void OnDespawned();
    }
}
