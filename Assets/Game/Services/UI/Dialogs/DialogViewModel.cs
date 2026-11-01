using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

namespace Game.Services.UI.Dialogs
{
    /// <summary>
    /// Content of a modal card: a ribbon title plus any of the optional blocks below it, top to bottom in the
    /// order they are declared here. Features fill one in and open it with <see cref="DialogWindow"/>.
    /// </summary>
    public sealed class DialogViewModel : ViewModel
    {
        private readonly Subject<Unit> _closeRequested = new Subject<Unit>();
        private readonly ReactiveProperty<string> _inputText = new ReactiveProperty<string>(string.Empty);

        /// <summary>
        /// Creates an empty dialog.
        /// </summary>
        /// <param name="title">Ribbon text.</param>
        /// <param name="style">Ribbon color.</param>
        public DialogViewModel(string title, RibbonStyle style)
        {
            Title = title;
            Style = style;
            Disposables.Add(_closeRequested);
            Disposables.Add(_inputText);
        }

        /// <summary>Ribbon text on the card's top edge.</summary>
        public string Title { get; }

        /// <summary>Berry (default) or green ribbon.</summary>
        public RibbonStyle Style { get; }

        /// <summary>Optional large picture under the title (booster icon, basket).</summary>
        public Sprite Icon { get; set; }

        /// <summary>Earned stars (0..3) for the level-complete card; null hides the stars row.</summary>
        public int? Stars { get; set; }

        /// <summary>Optional heading ("Beautifully kept!").</summary>
        public string Headline { get; set; }

        /// <summary>Optional body text, rich text allowed.</summary>
        public string Text { get; set; }

        /// <summary>Label/value rows ("Moves", "12").</summary>
        public List<(string Label, string Value)> Stats { get; } = new List<(string, string)>();

        /// <summary>Optional progress bar fill (0..1) with <see cref="ProgressCaption"/> above it.</summary>
        public float? Progress { get; set; }

        /// <summary>Caption of the progress bar ("Only 6 items left!").</summary>
        public string ProgressCaption { get; set; }

        /// <summary>Row of item pictures (items stuck on the counter).</summary>
        public List<Sprite> Items { get; } = new List<Sprite>();

        /// <summary>Optional reward line under the stats ("+30").</summary>
        public string Reward { get; set; }

        /// <summary>On/off switches (settings).</summary>
        public List<DialogToggle> Toggles { get; } = new List<DialogToggle>();

        /// <summary>Text input with suggestion chips (naming the shop); null hides it.</summary>
        public DialogInput Input { get; set; }

        /// <summary>The input's current text; bound two-way by the view.</summary>
        public ReactiveProperty<string> InputText => _inputText;

        /// <summary>Compact grid of buttons (reviewers' level jump).</summary>
        public List<DialogButton> Grid { get; } = new List<DialogButton>();

        /// <summary>Labelled segmented choices (reviewers' curve and offer variants).</summary>
        public List<DialogSegments> Segments { get; } = new List<DialogSegments>();

        /// <summary>Optional small print under the main content.</summary>
        public string Note { get; set; }

        /// <summary>Buttons, top to bottom; consecutive buttons marked <see cref="DialogButton.IsInRow"/> share a row.</summary>
        public List<DialogButton> Buttons { get; } = new List<DialogButton>();

        /// <summary>Whether tapping outside or pressing back closes the dialog.</summary>
        public bool IsClosedByBlocker { get; set; }

        /// <summary>Emits when an action asks the dialog to close.</summary>
        public IObservable<Unit> CloseRequested => _closeRequested;

        /// <summary>
        /// Asks the dialog to close (buttons with <see cref="DialogButton.IsClosing"/> do this automatically).
        /// </summary>
        public void Close() => _closeRequested.OnNext(Unit.Default);

        /// <summary>
        /// Runs a button's action and closes the dialog if the button says so. Called by the view.
        /// </summary>
        /// <param name="button">The tapped button.</param>
        public void Press(DialogButton button)
        {
            if (!button.IsEnabled.Value) return;

            if (button.IsClosing) Close();
            button.Action?.Invoke();
        }
    }
}
