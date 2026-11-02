using System.Collections.Generic;
using Game.Services.UI.Elements;
using UniRx;
using UnityEngine.UIElements;

namespace Game.Services.UI.Dialogs
{
    /// <summary>
    /// Builds the interactive blocks of a <see cref="DialogWindow"/>: toggles, selectable tiles, the text input with
    /// idea chips, the button grid and segmented choices. Each method returns a block for the window to add to its body.
    /// </summary>
    public static class DialogControls
    {
        private const string ButtonClass = "btn";
        private const string RoundedWhiteClass = "kit-rounded-white";
        private const string SwitchOnClass = "dialog-switch--on";
        private const string SegmentOnClass = "dialog-segment--on";
        private const string TileOnClass = "dialog-tile--on";
        private const string PriceBadgeClass = "dialog-tile__badge--price";

        /// <summary>
        /// Builds a labelled on/off switch.
        /// </summary>
        /// <param name="toggle">The toggle; <see cref="DialogToggle.Changed"/> decides the new state.</param>
        /// <param name="bindings">Subscriptions of the window's current binding.</param>
        /// <returns>The toggle row.</returns>
        public static VisualElement Toggle(DialogToggle toggle, CompositeDisposable bindings)
        {
            var row = ElementView.CreateElement("dialog-toggle", RoundedWhiteClass);
            row.Add(ElementView.CreateLabel(toggle.Label, "dialog-toggle__label"));
            var track = CreateButton(null, "dialog-switch", "kit-pill-white");
            track.Add(ElementView.CreateElement("dialog-switch__knob", "kit-circle"));
            row.Add(track);
            var isOn = toggle.IsOn;
            track.EnableInClassList(SwitchOnClass, isOn);
            track.OnClickAsObservable().Subscribe(_ =>
            {
                isOn = toggle.Changed(!isOn);
                track.EnableInClassList(SwitchOnClass, isOn);
            }).AddTo(bindings);
            return row;
        }

        /// <summary>
        /// Builds the text field bound to <see cref="DialogViewModel.InputText"/> and its idea chips.
        /// </summary>
        /// <param name="vm">The dialog with an <see cref="DialogViewModel.Input"/>.</param>
        /// <param name="bindings">Subscriptions of the window's current binding.</param>
        /// <returns>The input block.</returns>
        public static VisualElement Input(DialogViewModel vm, CompositeDisposable bindings)
        {
            var block = ElementView.CreateElement("dialog-input");
            var field = new TextField { maxLength = vm.Input.MaxLength, value = vm.InputText.Value };
            field.AddToClassList("dialog-input__field");
            field.textEdition.placeholder = vm.Input.Placeholder;
            field.RegisterValueChangedCallback(e => vm.InputText.Value = e.newValue);
            block.Add(field);
            var ideas = ElementView.CreateElement("dialog-input__ideas");
            block.Add(ideas);
            foreach (var idea in vm.Input.Ideas)
            {
                var chip = CreateButton(idea, "dialog-chip", "kit-chip");
                chip.OnClickAsObservable().Subscribe(_ => field.value = idea).AddTo(bindings);
                ideas.Add(chip);
            }

            return block;
        }

        /// <summary>
        /// Builds a row of selectable tiles (dashed when off, green when on) with a corner badge.
        /// </summary>
        /// <param name="tiles">The tiles; <see cref="DialogTile.Changed"/> decides each new state.</param>
        /// <param name="bindings">Subscriptions of the window's current binding.</param>
        /// <returns>The tile row.</returns>
        public static VisualElement Tiles(IReadOnlyList<DialogTile> tiles, CompositeDisposable bindings)
        {
            var row = ElementView.CreateElement("dialog-tiles");
            foreach (var tile in tiles)
            {
                var button = CreateButton(null, "dialog-tile");
                var icon = ElementView.CreateElement("dialog-tile__icon");
                icon.pickingMode = PickingMode.Ignore;
                icon.SetSprite(tile.Icon);
                button.Add(icon);
                button.Add(ElementView.CreateLabel(tile.Name, "dialog-tile__name"));
                button.Add(ElementView.CreateLabel(tile.Info, "dialog-tile__info"));
                var badge = ElementView.CreateElement("dialog-tile__badge");
                badge.pickingMode = PickingMode.Ignore;
                var badgeText = ElementView.CreateLabel(string.Empty, "dialog-tile__badge-text");
                badge.Add(badgeText);
                button.Add(badge);
                row.Add(button);

                var isOn = false;
                void Draw()
                {
                    button.EnableInClassList(TileOnClass, isOn);
                    badgeText.text = tile.Badge();
                    badge.EnableInClassList(PriceBadgeClass, tile.IsPriceBadge());
                }

                Draw();
                button.OnClickAsObservable().Subscribe(_ =>
                {
                    isOn = tile.Changed(!isOn);
                    Draw();
                }).AddTo(bindings);
            }

            return row;
        }

        /// <summary>
        /// Builds the grid of <see cref="DialogViewModel.Grid"/> buttons.
        /// </summary>
        /// <param name="vm">The dialog; a cell press goes to <see cref="DialogViewModel.Press"/>.</param>
        /// <param name="bindings">Subscriptions of the window's current binding.</param>
        /// <returns>The grid.</returns>
        public static VisualElement Grid(DialogViewModel vm, CompositeDisposable bindings)
        {
            var grid = ElementView.CreateElement("dialog-grid");
            foreach (var button in vm.Grid)
            {
                var cell = CreateButton(button.Label, "dialog-grid__cell", RoundedWhiteClass);
                cell.EnableInClassList("dialog-grid__cell--current", button.IsHighlighted);
                cell.OnTapAsObservable().Subscribe(_ => vm.Press(button)).AddTo(bindings);
                grid.Add(cell);
            }

            return grid;
        }

        /// <summary>
        /// Builds a labelled row of mutually exclusive options.
        /// </summary>
        /// <param name="segments">Options and the selected index.</param>
        /// <param name="bindings">Subscriptions of the window's current binding.</param>
        /// <returns>The segments block.</returns>
        public static VisualElement Segments(DialogSegments segments, CompositeDisposable bindings)
        {
            var block = ElementView.CreateElement("dialog-segments");
            block.Add(ElementView.CreateLabel(segments.Label, "dialog-segments__label"));
            var options = ElementView.CreateElement("dialog-segments__options");
            block.Add(options);
            var buttons = new List<Button>();
            for (var i = 0; i < segments.Options.Count; i++)
            {
                var option = CreateButton(segments.Options[i], "dialog-segment", RoundedWhiteClass);
                buttons.Add(option);
                options.Add(option);
                var index = i;
                option.OnClickAsObservable().Subscribe(_ =>
                {
                    Select(buttons, index);
                    segments.SelectedChanged(index);
                }).AddTo(bindings);
            }

            Select(buttons, segments.Selected);
            return block;
        }

        private static Button CreateButton(string text, params string[] classes)
        {
            var button = new Button { text = text };
            button.AddToClassList(ButtonClass);
            foreach (var c in classes)
            {
                button.AddToClassList(c);
            }

            return button;
        }

        private static void Select(List<Button> buttons, int index)
        {
            for (var k = 0; k < buttons.Count; k++)
            {
                buttons[k].EnableInClassList(SegmentOnClass, k == index);
            }
        }
    }
}
