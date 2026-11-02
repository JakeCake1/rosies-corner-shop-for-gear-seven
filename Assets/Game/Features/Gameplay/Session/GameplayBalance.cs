namespace Game.Features.Gameplay.Session
{
    /// <summary>
    /// Fixed prototype rules of a level: the star thresholds, the pacing of shipments and result popups and the
    /// "Next level" quirk. Tunable economy and progression numbers live in
    /// <see cref="Game.Services.Balance.GameBalanceConfig"/>.
    /// </summary>
    public static class GameplayBalance
    {
        /// <summary>Last level whose "Next level" button goes straight to the next level (prototype quirk).</summary>
        public const int DirectNextLevelBelow = 9;

        /// <summary>Delay before a full basket ships, in milliseconds.</summary>
        public const int ShipDelayMs = 480;

        /// <summary>Delay between the napkin and the basket leaving, in milliseconds.</summary>
        public const int ShipLeaveMs = 420;

        /// <summary>Delay before the win popup after the last shipment, in milliseconds.</summary>
        public const int WinDelayMs = 650;

        /// <summary>Delay before the loss popup, in milliseconds.</summary>
        public const int LossDelayMs = 450;

        private const int StrictLevels = 3;
        private const int StrictThreshold = 2;
        private const int Threshold = 3;
        private const int MaxStars = 3;

        /// <summary>
        /// Stars for a finished level from the counter peak (prototype <c>starsFor</c>).
        /// </summary>
        /// <param name="peak">Most items ever on the counter.</param>
        /// <param name="level">Level number; levels 1–3 use a stricter threshold.</param>
        /// <returns>1 to 3 stars.</returns>
        public static int StarsFor(int peak, int level)
        {
            var threshold = StarThreshold(level);
            return peak <= threshold ? MaxStars : peak <= threshold + 1 ? MaxStars - 1 : 1;
        }

        /// <summary>
        /// Counter peak that still earns three stars.
        /// </summary>
        /// <param name="level">Level number.</param>
        /// <returns>2 on levels 1–3, otherwise 3.</returns>
        public static int StarThreshold(int level) => level <= StrictLevels ? StrictThreshold : Threshold;
    }
}
