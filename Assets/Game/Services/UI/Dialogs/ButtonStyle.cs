using System;
using System.Collections.Generic;
using UniRx;

namespace Game.Services.UI.Dialogs
{
    /// <summary>
    /// Pressable button colors, matching the prototype's button variants.
    /// </summary>
    public enum ButtonStyle
    {
        /// <summary>Berry red: the main action.</summary>
        Berry,

        /// <summary>Green: free and rewarded actions.</summary>
        Green,

        /// <summary>Oak: secondary actions.</summary>
        Wood,

        /// <summary>Gold: coin purchases.</summary>
        Gold,

        /// <summary>Small oak: "Not now", "Back".</summary>
        WoodSmall,
    }
}
