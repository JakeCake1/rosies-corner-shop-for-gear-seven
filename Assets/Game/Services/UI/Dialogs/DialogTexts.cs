namespace Game.Services.UI.Dialogs
{
    /// <summary>
    /// Keys of texts the dialog card adds by itself, in the <c>UI</c> string table.
    /// </summary>
    /// <remarks>
    /// Every key must exist in the table; <c>LocalizationKeysTests</c> checks it. English text lives in the
    /// table (<c>Services/Localization/Tables</c>), the summaries quote it so call sites stay readable.
    /// </remarks>
    internal static class DialogTexts
    {
        /// <summary>English: "AD".</summary>
        internal const string AdBadge = "dialog.ad_badge";
    }
}
