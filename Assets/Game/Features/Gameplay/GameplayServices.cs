using Game.Services.Ads;
using Game.Services.Audio;
using Game.Services.Content;
using Game.Services.Flow;
using Game.Services.Levels;
using Game.Services.Progress;
using Game.Services.UI;
using Game.Services.UI.Dialogs;

namespace Game.Features.Gameplay
{
    /// <summary>
    /// Parameter object bundling the project services the gameplay presenter uses, to keep its constructor short.
    /// </summary>
    public sealed class GameplayServices
    {
        /// <summary>
        /// Creates the bundle; resolved by VContainer.
        /// </summary>
        /// <param name="dialogs">Dialog cards.</param>
        /// <param name="ui">Window state (is a popup open).</param>
        /// <param name="overlay">Toasts, coach bubbles, effects.</param>
        /// <param name="progress">Coins, boosters, flags.</param>
        /// <param name="content">Item catalog.</param>
        /// <param name="curves">Active level curve.</param>
        /// <param name="flow">Home / next level navigation.</param>
        /// <param name="ads">Rewarded ad placeholder.</param>
        /// <param name="settings">Settings dialog.</param>
        /// <param name="audio">Sound effects.</param>
        public GameplayServices(IDialogService dialogs, IUIService ui, IOverlayService overlay, IProgressService progress,
            IContentService content, ILevelCurveService curves, IGameFlow flow, IRewardedAdService ads,
            ISettingsScreen settings, IAudioService audio)
        {
            Dialogs = dialogs;
            UI = ui;
            Overlay = overlay;
            Progress = progress;
            Content = content;
            Curves = curves;
            Flow = flow;
            Ads = ads;
            Settings = settings;
            Audio = audio;
        }

        /// <summary>Dialog cards.</summary>
        public IDialogService Dialogs { get; }

        /// <summary>Window state.</summary>
        public IUIService UI { get; }

        /// <summary>Toasts, coach bubbles, effects.</summary>
        public IOverlayService Overlay { get; }

        /// <summary>Coins, boosters, flags.</summary>
        public IProgressService Progress { get; }

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
    }
}
