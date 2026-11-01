namespace Game.Services.Flow
{
    /// <summary>
    /// The level the gameplay scene should start; written by the flow before the scene loads and read by the
    /// gameplay presenter on start.
    /// </summary>
    public sealed class GameplayRequest
    {
        /// <summary>1-based level number in the active curve.</summary>
        public int Level { get; set; } = 1;

        /// <summary>Paid start boosters; never null.</summary>
        public StartBoosters StartBoosters { get; set; } = StartBoosters.None;
    }
}
