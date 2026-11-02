using System;
using System.Collections.Generic;
using UniRx;

namespace Game.Services.UI.Dialogs
{
    /// <summary>
    /// A dialog button.
    /// </summary>
    public sealed class DialogButton
    {
        /// <summary>Whether the button can be pressed (e.g. enough coins).</summary>
        public ReactiveProperty<bool> IsEnabled { get; } = new ReactiveProperty<bool>(true);

        /// <summary>Main label.</summary>
        public string Label { get; }

        /// <summary>Optional second line ("Tricky · Rewards ×2").</summary>
        public string SubLabel { get; set; }

        /// <summary>Color.</summary>
        public ButtonStyle Style { get; }

        /// <summary>Leading badge: "AD" for rewarded ads, or a coin price.</summary>
        public ButtonBadge Badge { get; set; }

        /// <summary>Coin price shown with the coin badge.</summary>
        public int Price { get; set; }

        /// <summary>Shares a row with the previous button marked the same way.</summary>
        public bool IsInRow { get; set; }

        /// <summary>Highlighted as the current choice (segments, red tricky levels in the grid).</summary>
        public bool IsHighlighted { get; set; }

        /// <summary>Closes the dialog before running <see cref="Action"/>.</summary>
        public bool IsClosing { get; }

        /// <summary>The action.</summary>
        public Action Action { get; }

        /// <summary>
        /// Creates a button.
        /// </summary>
        /// <param name="label">Main label.</param>
        /// <param name="style">Color.</param>
        /// <param name="action">What happens on tap (after closing when <paramref name="isClosing"/>).</param>
        /// <param name="isClosing">Close the dialog first.</param>
        public DialogButton(string label, ButtonStyle style, Action action, bool isClosing = true)
        {
            Label = label;
            Style = style;
            Action = action;
            IsClosing = isClosing;
        }
    }
}
