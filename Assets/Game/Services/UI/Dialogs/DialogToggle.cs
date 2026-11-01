using System;
using System.Collections.Generic;
using UniRx;

namespace Game.Services.UI.Dialogs
{
    /// <summary>
    /// An on/off switch row.
    /// </summary>
    public sealed class DialogToggle
    {
        /// <summary>
        /// Creates a switch.
        /// </summary>
        /// <param name="label">Row label.</param>
        /// <param name="isOn">Initial state.</param>
        /// <param name="changed">Called with the requested state; returns the state actually applied (a paid
        /// option can refuse to turn on).</param>
        public DialogToggle(string label, bool isOn, Func<bool, bool> changed)
        {
            Label = label;
            IsOn = isOn;
            Changed = changed;
        }

        /// <summary>Row label.</summary>
        public string Label { get; }

        /// <summary>Initial state.</summary>
        public bool IsOn { get; }

        /// <summary>Called with the requested state; returns the applied state.</summary>
        public Func<bool, bool> Changed { get; }
    }
}
