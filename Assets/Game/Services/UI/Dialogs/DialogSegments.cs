using System;
using System.Collections.Generic;
using UniRx;

namespace Game.Services.UI.Dialogs
{
    /// <summary>
    /// A labelled segmented control.
    /// </summary>
    public sealed class DialogSegments
    {
        /// <summary>
        /// Creates a segmented control.
        /// </summary>
        /// <param name="label">Row label.</param>
        /// <param name="options">Option labels.</param>
        /// <param name="selected">Initially selected index.</param>
        /// <param name="selectedChanged">Called with the new index.</param>
        public DialogSegments(string label, IReadOnlyList<string> options, int selected, Action<int> selectedChanged)
        {
            Label = label;
            Options = options;
            Selected = selected;
            SelectedChanged = selectedChanged;
        }

        /// <summary>Row label.</summary>
        public string Label { get; }

        /// <summary>Option labels.</summary>
        public IReadOnlyList<string> Options { get; }

        /// <summary>Initially selected index.</summary>
        public int Selected { get; }

        /// <summary>Called with the new index.</summary>
        public Action<int> SelectedChanged { get; }
    }
}
