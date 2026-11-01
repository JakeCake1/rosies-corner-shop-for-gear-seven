using System.Collections.Generic;
using DG.Tweening;
using Game.Services.UI.Elements;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Services.UI.Dialogs
{
    /// <summary>
    /// Generic modal card: ribbon title and a body assembled from <see cref="DialogViewModel"/>
    /// (icon, stars, headline, stats, text, progress, items, reward, toggles, input, grid, segments, note, buttons).
    /// </summary>
    public sealed class DialogWindow : WindowView<DialogViewModel>
    {
        private const string RibbonName = "ribbon";
        private const string TitleName = "title";
        private const string BodyName = "body";
        private const string RibbonGreenClass = "kit-ribbon-green";
        private const string RibbonBerryClass = "kit-ribbon-berry";
        private const float StarDelay = 0.22f;

        private VisualElement _ribbon;
        private Label _title;
        private VisualElement _body;
        private bool _isClosedByBlocker;

        /// <inheritdoc/>
        public override UILayer Layer => UILayer.Popups;

        /// <inheritdoc/>
        public override bool IsClosedByBlocker => _isClosedByBlocker;

        /// <inheritdoc/>
        protected override void OnLayoutAttached()
        {
            _ribbon = Q(RibbonName);
            _title = Q<Label>(TitleName);
            _body = Q(BodyName);
        }

        /// <inheritdoc/>
        protected override void OnBind(DialogViewModel vm)
        {
            _isClosedByBlocker = vm.IsClosedByBlocker;
            _title.text = vm.Title;
            _ribbon.EnableInClassList(RibbonGreenClass, vm.Style == RibbonStyle.Green);
            _ribbon.EnableInClassList(RibbonBerryClass, vm.Style != RibbonStyle.Green);
            vm.CloseRequested.Subscribe(_ => RequestClose()).AddTo(Bindings);

            if (vm.Icon != null)
            {
                var icon = Spawn(_body, CreateElement("dialog-icon"));
                icon.SetSprite(vm.Icon);
            }

            if (vm.Stars.HasValue) BuildStars(vm.Stars.Value);
            if (!string.IsNullOrEmpty(vm.Headline)) Spawn(_body, CreateLabel(vm.Headline, "dialog-headline"));
            foreach (var (label, value) in vm.Stats)
            {
                var row = Spawn(_body, CreateElement("dialog-stat", "kit-rounded-white"));
                row.Add(CreateLabel(label, "dialog-stat__label"));
                row.Add(CreateLabel(value, "dialog-stat__value"));
            }

            if (!string.IsNullOrEmpty(vm.Text)) Spawn(_body, CreateLabel(vm.Text, "dialog-text"));
            if (vm.Progress.HasValue) BuildProgress(vm.Progress.Value, vm.ProgressCaption);
            if (vm.Items.Count > 0) BuildItems(vm.Items);
            if (!string.IsNullOrEmpty(vm.Reward))
            {
                var reward = Spawn(_body, CreateElement("dialog-reward"));
                reward.Add(CreateElement("dialog-reward__coin", "icon-coin"));
                reward.Add(CreateLabel(vm.Reward, "dialog-reward__amount"));
            }

            foreach (var t in vm.Toggles)
            {
                BuildToggle(t);
            }

            if (vm.Input != null) BuildInput(vm);
            if (vm.Grid.Count > 0) BuildGrid(vm);
            foreach (var s in vm.Segments)
            {
                BuildSegments(s);
            }

            if (!string.IsNullOrEmpty(vm.Note)) Spawn(_body, CreateLabel(vm.Note, "dialog-note"));
            BuildButtons(vm);
        }

        private void BuildStars(int earned)
        {
            var row = Spawn(_body, CreateElement("dialog-stars"));
            for (var i = 0; i < 3; i++)
            {
                var isOn = i < earned;
                var star = CreateElement("dialog-star", isOn ? "icon-star-on" : "icon-star-off");
                star.EnableInClassList("dialog-star--big", i == 1);
                row.Add(star);
                if (!isOn) continue;

                VisualElementTweens.SetScale(star, Vector2.zero);
                star.DOScale(1f, 0.35f).SetDelay(0.15f + i * StarDelay).SetEase(Ease.OutBack, 2f).SetUpdate(true).SetId(this);
            }
        }

        private void BuildProgress(float fraction, string caption)
        {
            var block = Spawn(_body, CreateElement("dialog-progress"));
            block.Add(CreateLabel(caption ?? string.Empty, "dialog-progress__caption"));
            var track = CreateElement("dialog-progress__track", "kit-bar-track");
            var fill = CreateElement("dialog-progress__fill", "kit-bar-fill");
            fill.style.width = Length.Percent(Mathf.Clamp01(fraction) * 100f);
            track.Add(fill);
            block.Add(track);
        }

        private void BuildItems(List<Sprite> sprites)
        {
            var row = Spawn(_body, CreateElement("dialog-items"));
            foreach (var s in sprites)
            {
                var item = CreateElement("dialog-items__item");
                item.SetSprite(s);
                row.Add(item);
            }
        }

        private void BuildToggle(DialogToggle toggle)
        {
            var row = Spawn(_body, CreateElement("dialog-toggle", "kit-rounded-white"));
            row.Add(CreateLabel(toggle.Label, "dialog-toggle__label"));
            var track = new Button();
            track.AddToClassList("btn");
            track.AddToClassList("dialog-switch");
            track.AddToClassList("kit-pill-white");
            var knob = CreateElement("dialog-switch__knob", "kit-circle");
            track.Add(knob);
            row.Add(track);
            var isOn = toggle.IsOn;

            void Apply() => track.EnableInClassList("dialog-switch--on", isOn);

            Apply();
            track.OnClickAsObservable().Subscribe(_ =>
            {
                isOn = toggle.Changed(!isOn);
                Apply();
            }).AddTo(Bindings);
        }

        private void BuildInput(DialogViewModel vm)
        {
            var block = Spawn(_body, CreateElement("dialog-input"));
            var field = new TextField { maxLength = vm.Input.MaxLength, value = vm.InputText.Value };
            field.AddToClassList("dialog-input__field");
            field.textEdition.placeholder = vm.Input.Placeholder;
            field.RegisterValueChangedCallback(e => vm.InputText.Value = e.newValue);
            block.Add(field);
            var ideas = CreateElement("dialog-input__ideas");
            block.Add(ideas);
            foreach (var idea in vm.Input.Ideas)
            {
                var chip = new Button { text = idea };
                chip.AddToClassList("btn");
                chip.AddToClassList("dialog-chip");
                chip.AddToClassList("kit-chip");
                var text = idea;
                chip.OnClickAsObservable().Subscribe(_ => field.value = text).AddTo(Bindings);
                ideas.Add(chip);
            }
        }

        private void BuildGrid(DialogViewModel vm)
        {
            var grid = Spawn(_body, CreateElement("dialog-grid"));
            foreach (var b in vm.Grid)
            {
                var cell = new Button { text = b.Label };
                cell.AddToClassList("btn");
                cell.AddToClassList("dialog-grid__cell");
                cell.AddToClassList("kit-rounded-white");
                cell.EnableInClassList("dialog-grid__cell--current", b.IsHighlighted);
                var button = b;
                cell.OnTapAsObservable().Subscribe(_ => vm.Press(button)).AddTo(Bindings);
                grid.Add(cell);
            }
        }

        private void BuildSegments(DialogSegments segments)
        {
            var block = Spawn(_body, CreateElement("dialog-segments"));
            block.Add(CreateLabel(segments.Label, "dialog-segments__label"));
            var options = CreateElement("dialog-segments__options");
            block.Add(options);
            var buttons = new List<Button>();
            for (var i = 0; i < segments.Options.Count; i++)
            {
                var option = new Button { text = segments.Options[i] };
                option.AddToClassList("btn");
                option.AddToClassList("dialog-segment");
                option.AddToClassList("kit-rounded-white");
                buttons.Add(option);
                options.Add(option);
                var index = i;
                option.OnClickAsObservable().Subscribe(_ =>
                {
                    Select(buttons, index);
                    segments.SelectedChanged(index);
                }).AddTo(Bindings);
            }

            Select(buttons, segments.Selected);
        }

        private static void Select(List<Button> buttons, int index)
        {
            for (var k = 0; k < buttons.Count; k++)
            {
                buttons[k].EnableInClassList("dialog-segment--on", k == index);
            }
        }

        private void BuildButtons(DialogViewModel vm)
        {
            VisualElement row = null;
            foreach (var b in vm.Buttons)
            {
                var parent = _body;
                if (b.IsInRow)
                {
                    if (row == null) row = Spawn(_body, CreateElement("dialog-row"));
                    parent = row;
                }
                else
                {
                    row = null;
                }

                var view = DialogButtonView.Create(b);
                if (parent == _body) Spawn(_body, view.Root);
                else parent.Add(view.Root);
                var button = b;
                view.Button.OnTapAsObservable().Subscribe(_ => vm.Press(button)).AddTo(Bindings);
                b.IsEnabled.Subscribe(x => view.Button.SetEnabled(x)).AddTo(Bindings);
            }
        }
    }
}
