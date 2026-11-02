namespace Game.Features.Album
{
    /// <summary>
    /// Keys of the album screen and album intro texts in the <c>UI</c> string table.
    /// </summary>
    /// <remarks>
    /// Every key must exist in the table; <c>LocalizationKeysTests</c> checks it. English text lives in the
    /// table (<c>Services/Localization/Tables</c>), the summaries quote it so call sites stay readable.
    /// </remarks>
    internal static class AlbumTexts
    {
        /// <summary>English: "Back".</summary>
        internal const string Back = "album.back";

        /// <summary>English: "My Album".</summary>
        internal const string Title = "album.title";

        /// <summary>English: "PACK A BASKET TO ADD ITS ITEM".</summary>
        internal const string Subtitle = "album.subtitle";

        /// <summary>English: "&lt;b&gt;{0}&lt;/b&gt; / {1}". Arguments: {0} found items, {1} catalog size.</summary>
        internal const string Total = "album.total";

        /// <summary>
        /// English: "{0} / {1} · collected, +{2}". Arguments: {0} found, {1} in the department, {2} bonus coins.
        /// </summary>
        internal const string SectionDone = "album.section_done";

        /// <summary>
        /// English: "{0} / {1} · {2} coins when complete". Arguments: {0} found, {1} in the department, {2} bonus
        /// coins.
        /// </summary>
        internal const string SectionOpen = "album.section_open";

        /// <summary>English: "?".</summary>
        internal const string UnknownItem = "album.unknown_item";

        /// <summary>English: "New: the Album!".</summary>
        internal const string IntroTitle = "album.intro_title";

        /// <summary>
        /// English: "Rosie: "I kept one of everything you sold. {0} items so far! Fill a whole department and I'll pay
        /// you {1} coins."". Arguments: {0} found items, {1} bonus coins.
        /// </summary>
        internal const string IntroText = "album.intro_text";

        /// <summary>English: "Open the album".</summary>
        internal const string IntroOpen = "album.intro_open";

        /// <summary>English: "Later".</summary>
        internal const string IntroLater = "album.intro_later";
    }
}
