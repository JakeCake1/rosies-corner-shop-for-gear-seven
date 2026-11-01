namespace Game.Services.Onboarding
{
    /// <summary>
    /// Names of one-time progress flags stored in the save.
    /// </summary>
    public static class Flags
    {
        /// <summary>The first extra basket was given for free.</summary>
        public const string FreeBasket = "freeBasket";

        /// <summary>Rosie gave the two start boosters of each kind.</summary>
        public const string StartBoosterGift = "preGift";

        /// <summary>The start-boosters popup was shown once.</summary>
        public const string StartBoostersSeen = "preSeen";

        /// <summary>The "New: the Album!" popup was shown.</summary>
        public const string AlbumIntro = "albumIntro";

        /// <summary>The album was opened at least once (stops the button pulse).</summary>
        public const string AlbumOpened = "albumOpened";

        /// <summary>The player was asked to name the shop.</summary>
        public const string NameAsked = "nameAsked";
    }
}
