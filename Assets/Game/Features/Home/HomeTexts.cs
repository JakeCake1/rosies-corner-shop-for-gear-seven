namespace Game.Features.Home
{
    /// <summary>
    /// Keys of the home screen and shop naming texts in the <c>UI</c> string table.
    /// </summary>
    /// <remarks>
    /// Every key must exist in the table; <c>LocalizationKeysTests</c> checks it. English text lives in the
    /// table (<c>Services/Localization/Tables</c>), the summaries quote it so call sites stay readable.
    /// </remarks>
    internal static class HomeTexts
    {
        /// <summary>English: "Rosie's Corner Shop".</summary>
        internal const string DefaultShopName = "home.default_shop_name";

        /// <summary>English: "GROCERIES · BAKERY · SWEETS".</summary>
        internal const string Tagline = "home.tagline";

        /// <summary>English: "&lt;u&gt;For reviewers&lt;/u&gt;".</summary>
        internal const string Reviewers = "home.reviewers";

        /// <summary>English: "Album".</summary>
        internal const string Album = "home.album";

        /// <summary>English: "Open the shop".</summary>
        internal const string PlayTitle = "home.play_title";

        /// <summary>English: "Level {0}". Arguments: {0} next level number.</summary>
        internal const string PlayLine = "home.play_line";

        /// <summary>English: "9+".</summary>
        internal const string AlbumBadgeMax = "home.album_badge_max";

        /// <summary>English: "Level {0} · Tricky · Rewards ×2". Arguments: {0} next level number.</summary>
        internal const string PlayLineTricky = "home.play_line_tricky";

        /// <summary>
        /// English: "The album opens after level {0}. Keep going!". Arguments: {0} level that unlocks the album.
        /// </summary>
        internal const string AlbumLocked = "home.album_locked";

        /// <summary>English: "You're my partner now!".</summary>
        internal const string NameTitle = "home.name_title";

        /// <summary>
        /// English: "Rosie: "Ten busy days! Let's put your name on the sign. What shall we call the shop?"".
        /// </summary>
        internal const string NameText = "home.name_text";

        /// <summary>English: "Rosie &amp; Me".</summary>
        internal const string NameIdea1 = "home.name_idea_1";

        /// <summary>English: "Maple Street Market".</summary>
        internal const string NameIdea2 = "home.name_idea_2";

        /// <summary>English: "The Corner Pantry".</summary>
        internal const string NameIdea3 = "home.name_idea_3";

        /// <summary>English: "Hang the sign".</summary>
        internal const string NameConfirm = "home.name_confirm";

        /// <summary>English: "Keep Rosie's name".</summary>
        internal const string NameKeep = "home.name_keep";
    }
}
