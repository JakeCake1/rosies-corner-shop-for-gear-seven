using System;
using System.Collections.Generic;
using UniRx;

namespace Game.Services.UI.Dialogs
{
    /// <summary>
    /// A text input with suggestion chips.
    /// </summary>
    public sealed class DialogInput
    {
        /// <summary>
        /// Creates an input.
        /// </summary>
        /// <param name="placeholder">Grey hint text.</param>
        /// <param name="maxLength">Character limit.</param>
        /// <param name="ideas">Suggestion chips that fill the input.</param>
        public DialogInput(string placeholder, int maxLength, IReadOnlyList<string> ideas)
        {
            Placeholder = placeholder;
            MaxLength = maxLength;
            Ideas = ideas;
        }

        /// <summary>Grey hint text.</summary>
        public string Placeholder { get; }

        /// <summary>Character limit.</summary>
        public int MaxLength { get; }

        /// <summary>Suggestion chips.</summary>
        public IReadOnlyList<string> Ideas { get; }
    }
}
