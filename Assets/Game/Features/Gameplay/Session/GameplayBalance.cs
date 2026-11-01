namespace Game.Features.Gameplay.Session
{
    /// <summary>
    /// Economy and progression numbers of the prototype, in one place.
    /// </summary>
    public static class GameplayBalance
    {
        /// <summary>Free undos at the start of every level.</summary>
        public const int FreeUndosPerLevel = 2;

        /// <summary>Price of two more undos.</summary>
        public const int UndoPackPrice = 60;

        /// <summary>Undos in the coin pack.</summary>
        public const int UndoPackSize = 2;

        /// <summary>Price of one Tidy Up, Quick Pick or Shuffle.</summary>
        public const int BoosterPrice = 100;

        /// <summary>Price of opening the third basket for a level.</summary>
        public const int ExtraBasketPrice = 150;

        /// <summary>Boosters Rosie gives when a booster unlocks.</summary>
        public const int BoosterGift = 2;

        /// <summary>Level where Undo appears.</summary>
        public const int UndoUnlockLevel = 2;

        /// <summary>Level where Tidy Up appears.</summary>
        public const int TidyUnlockLevel = 4;

        /// <summary>Level where Quick Pick appears.</summary>
        public const int PickUnlockLevel = 6;

        /// <summary>Level from which the "+" extra basket slot is shown.</summary>
        public const int ExtraBasketSlotLevel = 3;

        /// <summary>Level from which rewarded-ad buttons appear anywhere.</summary>
        public const int AdsFromLevel = 6;

        /// <summary>Level from which the start-boosters popup appears (new curve).</summary>
        public const int StartBoostersFromLevel = 11;

        /// <summary>Last level whose "Next level" button goes straight to the next level (prototype quirk).</summary>
        public const int DirectNextLevelBelow = 9;

        /// <summary>Album unlock level (stars on it open the album).</summary>
        public const int AlbumFromLevel = 9;

        /// <summary>Level after which the player names the shop.</summary>
        public const int NameShopAfterLevel = 10;

        /// <summary>Coins per star; tricky levels pay double.</summary>
        public const int CoinsPerStar = 10;

        /// <summary>Last level with coach tips and the always-on hidden hint.</summary>
        public const int CoachTipsUntilLevel = 4;

        /// <summary>Last level where the idle hint works without a previous loss.</summary>
        public const int HintUntilLevel = 5;

        /// <summary>Idle time before the hidden hint highlights a move, in seconds.</summary>
        public const float HintIdleSeconds = 6f;

        /// <summary>Counter capacity after the "+1 counter spot" offer.</summary>
        public const int ExtendedCapacity = 6;

        /// <summary>Delay before a full basket ships, in milliseconds.</summary>
        public const int ShipDelayMs = 480;

        /// <summary>Delay between the napkin and the basket leaving, in milliseconds.</summary>
        public const int ShipLeaveMs = 420;

        /// <summary>Delay before the win popup after the last shipment, in milliseconds.</summary>
        public const int WinDelayMs = 650;

        /// <summary>Delay before the loss popup, in milliseconds.</summary>
        public const int LossDelayMs = 450;

        /// <summary>
        /// Stars for a finished level from the counter peak (prototype <c>starsFor</c>).
        /// </summary>
        /// <param name="peak">Most items ever on the counter.</param>
        /// <param name="level">Level number; levels 1–3 use a stricter threshold.</param>
        /// <returns>1 to 3 stars.</returns>
        public static int StarsFor(int peak, int level)
        {
            var threshold = StarThreshold(level);
            return peak <= threshold ? 3 : peak <= threshold + 1 ? 2 : 1;
        }

        /// <summary>
        /// Counter peak that still earns three stars.
        /// </summary>
        /// <param name="level">Level number.</param>
        /// <returns>2 on levels 1–3, otherwise 3.</returns>
        public static int StarThreshold(int level) => level <= 3 ? 2 : 3;
    }
}
