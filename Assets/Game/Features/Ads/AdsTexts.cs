namespace Game.Features.Ads
{
    /// <summary>
    /// Keys of the placeholder rewarded-ad card in the <c>UI</c> string table.
    /// </summary>
    /// <remarks>
    /// Every key must exist in the table; <c>LocalizationKeysTests</c> checks it. English text lives in the
    /// table (<c>Services/Localization/Tables</c>), the summaries quote it so call sites stay readable.
    /// </remarks>
    internal static class AdsTexts
    {
        /// <summary>English: "Rewarded ad".</summary>
        internal const string Title = "ads.title";

        /// <summary>English: "A rewarded video plays here in the real game.".</summary>
        internal const string Text = "ads.text";

        /// <summary>English: "Playing…".</summary>
        internal const string Playing = "ads.playing";
    }
}
