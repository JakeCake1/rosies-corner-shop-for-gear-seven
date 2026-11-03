namespace Game.Features.Gameplay.Session
{
    /// <summary>
    /// Supplies the seed of each Shuffle booster use, so tests can make the shuffle deterministic.
    /// </summary>
    public interface IShuffleSeedSource
    {
        /// <summary>
        /// Returns the seed for the next shuffle.
        /// </summary>
        /// <returns>A seed for <see cref="Model.Mulberry32"/>; different on every call in production.</returns>
        uint NextSeed();
    }
}
