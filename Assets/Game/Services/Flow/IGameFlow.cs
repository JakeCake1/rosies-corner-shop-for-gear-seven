using Cysharp.Threading.Tasks;

namespace Game.Services.Flow
{
    /// <summary>
    /// Lets features move between the home screen and a level without referencing the state machine
    /// (dependencies point Flow → Features, never back).
    /// </summary>
    public interface IGameFlow
    {
        /// <summary>
        /// Leaves the current level (if any) and shows the home screen.
        /// </summary>
        /// <returns>Completes when the home screen is shown.</returns>
        UniTask GoHomeAsync();

        /// <summary>
        /// Starts a level. From level 11 on the new curve the start-boosters popup comes first unless
        /// <paramref name="startBoosters"/> is given.
        /// </summary>
        /// <param name="level">1-based level number in the active curve.</param>
        /// <param name="startBoosters">Chosen start boosters; null to ask the player when the curve requires it.</param>
        /// <returns>Completes when the level is on screen.</returns>
        UniTask PlayLevelAsync(int level, StartBoosters startBoosters = null);
    }
}
