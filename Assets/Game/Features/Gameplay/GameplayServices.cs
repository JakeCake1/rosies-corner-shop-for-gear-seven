using Game.Features.Gameplay.Boosters;
using Game.Services.Ads;
using Game.Services.Audio;
using Game.Services.Content;
using Game.Services.Experiments;
using Game.Services.Flow;
using Game.Services.Levels;
using Game.Services.Localization;
using Game.Services.Onboarding;
using Game.Services.UI;
using Game.Services.UI.Dialogs;
using Game.Services.Wallet;

namespace Game.Features.Gameplay
{
    /// <summary>
    /// Parameter object bundling the project services the gameplay presenter uses, to keep its constructor short.
    /// </summary>
    public sealed class GameplayServices
    {
        /// <summary>Dialog cards.</summary>
        public IDialogService Dialogs { get; }

        /// <summary>Window state.</summary>
        public IUIService UI { get; }

        /// <summary>Toasts, coach bubbles, effects.</summary>
        public IOverlayService Overlay { get; }

        /// <summary>Coins.</summary>
        public IWalletService Wallet { get; }

        /// <summary>Owned boosters and unlock gifts.</summary>
        public IBoosterInventory Boosters { get; }

        /// <summary>One-time flags and coach tips.</summary>
        public IOnboardingService Onboarding { get; }

        /// <summary>Lost levels (hint).</summary>
        public ILevelProgressService Levels { get; }

        /// <summary>Rewarded offer variant.</summary>
        public IExperimentsService Experiments { get; }

        /// <summary>Item catalog.</summary>
        public IContentService Content { get; }

        /// <summary>Active level curve.</summary>
        public ILevelCurveService Curves { get; }

        /// <summary>Home / next level navigation.</summary>
        public IGameFlow Flow { get; }

        /// <summary>Rewarded ad placeholder.</summary>
        public IRewardedAdService Ads { get; }

        /// <summary>Settings dialog.</summary>
        public ISettingsScreen Settings { get; }

        /// <summary>Sound effects.</summary>
        public IAudioService Audio { get; }

        /// <summary>Player-facing text from the string tables.</summary>
        public ILocalizationService Texts { get; }

        /// <summary>
        /// Creates the bundle; resolved by VContainer.
        /// </summary>
        /// <param name="dialogs">Dialog cards.</param>
        /// <param name="ui">Window state (is a popup open).</param>
        /// <param name="overlay">Toasts, coach bubbles, effects.</param>
        /// <param name="wallet">Coins.</param>
        /// <param name="boosters">Owned boosters and unlock gifts.</param>
        /// <param name="onboarding">One-time flags and coach tips.</param>
        /// <param name="levels">Lost levels (hint).</param>
        /// <param name="experiments">Rewarded offer variant.</param>
        /// <param name="content">Item catalog.</param>
        /// <param name="curves">Active level curve.</param>
        /// <param name="flow">Home / next level navigation.</param>
        /// <param name="ads">Rewarded ad placeholder.</param>
        /// <param name="settings">Settings dialog.</param>
        /// <param name="audio">Sound effects.</param>
        /// <param name="texts">Dialog, toast and tip texts.</param>
        public GameplayServices(IDialogService dialogs, IUIService ui, IOverlayService overlay, IWalletService wallet,
            IBoosterInventory boosters, IOnboardingService onboarding, ILevelProgressService levels,
            IExperimentsService experiments, IContentService content, ILevelCurveService curves, IGameFlow flow, IRewardedAdService ads,
            ISettingsScreen settings, IAudioService audio, ILocalizationService texts)
        {
            Texts = texts;
            Dialogs = dialogs;
            UI = ui;
            Overlay = overlay;
            Wallet = wallet;
            Boosters = boosters;
            Onboarding = onboarding;
            Levels = levels;
            Experiments = experiments;
            Content = content;
            Curves = curves;
            Flow = flow;
            Ads = ads;
            Settings = settings;
            Audio = audio;
        }
    }
}
