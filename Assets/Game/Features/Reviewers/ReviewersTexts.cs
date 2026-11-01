namespace Game.Features.Reviewers
{
    /// <summary>
    /// Keys of the reviewers panel texts in the <c>UI</c> string table.
    /// </summary>
    /// <remarks>
    /// Every key must exist in the table; <c>LocalizationKeysTests</c> checks it. English text lives in the
    /// table (<c>Services/Localization/Tables</c>), the summaries quote it so call sites stay readable.
    /// </remarks>
    internal static class ReviewersTexts
    {
        /// <summary>English: "For reviewers".</summary>
        internal const string Title = "reviewers.title";

        /// <summary>English: "Jump to any level, or switch the variants that would be A/B tested.".</summary>
        internal const string Text = "reviewers.text";

        /// <summary>
        /// English: "Hard levels are red. Ads and purchases are placeholders. Vibration works on Android only.".
        /// </summary>
        internal const string Note = "reviewers.note";

        /// <summary>English: "Level curve".</summary>
        internal const string Curve = "reviewers.curve";

        /// <summary>English: "Rewarded offer on a loss".</summary>
        internal const string Offer = "reviewers.offer";

        /// <summary>English: "+1 customer".</summary>
        internal const string OfferCustomer = "reviewers.offer_customer";

        /// <summary>English: "+1 counter spot".</summary>
        internal const string OfferSpot = "reviewers.offer_spot";

        /// <summary>English: "Reset progress".</summary>
        internal const string Reset = "reviewers.reset";

        /// <summary>English: "Close".</summary>
        internal const string Close = "reviewers.close";
    }
}
