namespace Game.Services.UI
{
    /// <summary>
    /// Stacking layers of the UI root, bottom to top; each is its own container with a fixed order.
    /// </summary>
    public enum UILayer
    {
        /// <summary>Always-visible overlays of a screen (unused while the board has its own HUD).</summary>
        HUD,

        /// <summary>Full-screen screens: home, album.</summary>
        Screens,

        /// <summary>Modal popups with a dimmed blocker behind the top one.</summary>
        Popups,

        /// <summary>Toasts, banners, coach bubbles, the rewarded-ad placeholder.</summary>
        Overlay,
    }
}
