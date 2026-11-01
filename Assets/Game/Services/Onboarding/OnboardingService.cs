using Game.Services.Save;

namespace Game.Services.Onboarding
{
    /// <summary>
    /// Default <see cref="IOnboardingService"/>.
    /// </summary>
    public sealed class OnboardingService : ProfileChunk<OnboardingData>, IOnboardingService
    {
        /// <inheritdoc/>
        public override string Key => "onboarding";

        /// <inheritdoc/>
        public bool HasFlag(string flag) => Data.Flags.Contains(flag);

        /// <inheritdoc/>
        public void SetFlag(string flag)
        {
            if (Data.Flags.Add(flag)) MarkChanged();
        }

        /// <inheritdoc/>
        public bool HasSeenTip(string tip) => Data.SeenTips.Contains(tip);

        /// <inheritdoc/>
        public void MarkTipSeen(string tip)
        {
            if (Data.SeenTips.Add(tip)) MarkChanged();
        }
    }
}
