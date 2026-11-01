namespace Game.Services.UI
{
    /// <summary>
    /// UI layers, bottom to top.
    /// </summary>
    public enum UILayer
    {
        /// <summary>The scene's own UI (the gameplay board), under everything else.</summary>
        Scene,

        /// <summary>Persistent HUD.</summary>
        HUD,

        /// <summary>Full-screen windows (home, album).</summary>
        Screens,

        /// <summary>Modal popups with the dimmed blocker.</summary>
        Popups,

        /// <summary>Toasts, coach tips and effects.</summary>
        Overlay,
    }
}
