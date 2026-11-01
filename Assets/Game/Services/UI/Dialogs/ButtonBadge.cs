using System;
using System.Collections.Generic;
using UniRx;

namespace Game.Services.UI.Dialogs
{
    /// <summary>
    /// Leading badge of a button.
    /// </summary>
    public enum ButtonBadge
    {
        /// <summary>No badge.</summary>
        None,

        /// <summary>White "AD" chip (rewarded video placeholder).</summary>
        Ad,

        /// <summary>Coin icon with <see cref="DialogButton.Price"/>.</summary>
        Coins,
    }
}
