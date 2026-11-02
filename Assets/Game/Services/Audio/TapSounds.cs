namespace Game.Services.Audio
{
    /// <summary>
    /// Picks the tap sound for an item's outline class, as the prototype's <c>AU.mat</c> does.
    /// </summary>
    public static class TapSounds
    {
        /// <summary>
        /// Returns the tap sound for a catalog silhouette.
        /// </summary>
        /// <param name="silhouette">
        /// <c>ItemDefinition.Silhouette</c>: "box", "can", "bottle", "stick" or "round"; anything else (unknown or
        /// empty) falls back to the round thud, which the prototype also uses for the hammer and paper bags.
        /// </param>
        /// <returns>The sound to play.</returns>
        public static SoundId For(string silhouette)
        {
            switch (silhouette)
            {
                case "box": return SoundId.TapBox;
                case "can": return SoundId.TapCan;
                case "bottle": return SoundId.TapBottle;
                case "stick": return SoundId.TapStick;
                default: return SoundId.TapRound;
            }
        }
    }
}
