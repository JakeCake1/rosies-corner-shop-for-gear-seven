namespace Game.Features.Settings
{
    /// <summary>
    /// Keys of the settings card texts in the <c>UI</c> string table.
    /// </summary>
    /// <remarks>
    /// Every key must exist in the table; <c>LocalizationKeysTests</c> checks it. English text lives in the
    /// table (<c>Services/Localization/Tables</c>), the summaries quote it so call sites stay readable.
    /// </remarks>
    internal static class SettingsTexts
    {
        /// <summary>English: "Settings".</summary>
        internal const string Title = "settings.title";

        /// <summary>English: "Sound".</summary>
        internal const string Sound = "settings.sound";

        /// <summary>English: "Music".</summary>
        internal const string Music = "settings.music";

        /// <summary>English: "Vibration".</summary>
        internal const string Vibration = "settings.vibration";

        /// <summary>English: "Restart level".</summary>
        internal const string RestartLevel = "settings.restart_level";

        /// <summary>English: "Back to the shop".</summary>
        internal const string BackHome = "settings.back_home";

        /// <summary>English: "Rename the shop".</summary>
        internal const string Rename = "settings.rename";

        /// <summary>English: "Close".</summary>
        internal const string Close = "settings.close";

        /// <summary>English: "Reset progress".</summary>
        internal const string Reset = "settings.reset";

        /// <summary>English: "Tap again to erase everything".</summary>
        internal const string ResetConfirm = "settings.reset_confirm";
    }
}
