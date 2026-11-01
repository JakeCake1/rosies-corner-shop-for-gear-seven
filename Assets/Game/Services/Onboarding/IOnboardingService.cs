namespace Game.Services.Onboarding
{
    /// <summary>
    /// One-time flags (milestones, first-time popups) and coach tips already shown; owns the <c>onboarding</c>
    /// profile chunk.
    /// </summary>
    public interface IOnboardingService
    {
        /// <summary>
        /// Returns whether a one-time flag is set.
        /// </summary>
        /// <param name="flag">One of <see cref="Flags"/>.</param>
        /// <returns>True once <see cref="SetFlag"/> was called for it.</returns>
        bool HasFlag(string flag);

        /// <summary>
        /// Sets a one-time flag.
        /// </summary>
        /// <param name="flag">One of <see cref="Flags"/>.</param>
        void SetFlag(string flag);

        /// <summary>
        /// Returns whether a coach tip was already shown.
        /// </summary>
        /// <param name="tip">Tip key ("counter", "ship", "stock").</param>
        /// <returns>True once <see cref="MarkTipSeen"/> was called for it.</returns>
        bool HasSeenTip(string tip);

        /// <summary>
        /// Remembers that a coach tip was shown so it never shows again.
        /// </summary>
        /// <param name="tip">Tip key.</param>
        void MarkTipSeen(string tip);
    }
}
