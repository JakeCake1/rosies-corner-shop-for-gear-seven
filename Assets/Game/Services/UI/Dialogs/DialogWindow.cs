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
        private const int StarCount = 3;
        private const int BigStar = 1;
        private const float StarSeconds = 0.35f;
        private const float StarFirstDelay = 0.15f;
        private const float StarDelay = 0.22f;
        private const float StarOvershoot = 2f;

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

            BuildHeader(vm);
            BuildSummary(vm);
            BuildControls(vm);
            if (!string.IsNullOrEmpty(vm.Note))
            {
                Spawn(_body, CreateLabel(vm.Note, "dialog-note"));
            }

            BuildButtons(vm);
        }

        private void BuildHeader(DialogViewModel vm)
        {
            if (vm.Icon != null)
            {
                Spawn(_body, CreateElement("dialog-icon")).SetSprite(vm.Icon);
            }

            if (vm.Stars.HasValue)
            {
                BuildStars(vm.Stars.Value);
            }

            if (!string.IsNullOrEmpty(vm.Headline))
            {
                Spawn(_body, CreateLabel(vm.Headline, "dialog-headline"));
            }

            foreach (var (label, value) in vm.Stats)
            {
                var row = Spawn(_body, CreateElement("dialog-stat", "kit-rounded-white"));
                row.Add(CreateLabel(label, "dialog-stat__label"));
                row.Add(CreateLabel(value, "dialog-stat__value"));
            }
        }

        private void BuildSummary(DialogViewModel vm)
        {
            if (!string.IsNullOrEmpty(vm.Text))
            {
                Spawn(_body, CreateLabel(vm.Text, "dialog-text"));
            }

            if (vm.Progress.HasValue)
            {
                BuildProgress(vm.Progress.Value, vm.ProgressCaption);
            }

            if (vm.Items.Count > 0)
            {
                BuildItems(vm.Items);
            }

            if (!string.IsNullOrEmpty(vm.Reward))
            {
                var reward = Spawn(_body, CreateElement("dialog-reward"));
                reward.Add(CreateElement("dialog-reward__coin", "icon-coin"));
                reward.Add(CreateLabel(vm.Reward, "dialog-reward__amount"));
            }
        }

        private void BuildControls(DialogViewModel vm)
        {
            foreach (var toggle in vm.Toggles)
            {
                Spawn(_body, DialogControls.Toggle(toggle, Bindings));
            }

            if (vm.Tiles.Count > 0)
            {
                Spawn(_body, DialogControls.Tiles(vm.Tiles, Bindings));
            }

            if (vm.Input != null)
            {
                Spawn(_body, DialogControls.Input(vm, Bindings));
            }

            if (vm.Grid.Count > 0)
            {
                Spawn(_body, DialogControls.Grid(vm, Bindings));
            }

            foreach (var segments in vm.Segments)
            {
                Spawn(_body, DialogControls.Segments(segments, Bindings));
            }
        }

        private void BuildStars(int earned)
        {
            var row = Spawn(_body, CreateElement("dialog-stars"));
            for (var i = 0; i < StarCount; i++)
            {
                var isOn = i < earned;
                var icon = isOn ? "icon-star-on" : "icon-star-off";
                var star = CreateElement("dialog-star");
                star.Add(CreateElement("dialog-star__shadow", icon));
                star.Add(CreateElement("dialog-star__face", icon));
                star.EnableInClassList("dialog-star--big", i == BigStar);
                row.Add(star);
                if (!isOn)
                {
                    continue;
                }

                VisualElementTweens.SetScale(star, Vector2.zero);
                star.DOScale(1f, StarSeconds).SetDelay(StarFirstDelay + i * StarDelay).SetEase(Ease.OutBack, StarOvershoot)
                    .SetUpdate(true).SetId(this);
            }
        }

        private void BuildProgress(float fraction, string caption)
        {
            var block = Spawn(_body, CreateElement("dialog-progress"));
            block.Add(CreateLabel(caption ?? string.Empty, "dialog-progress__caption"));
            var track = CreateElement("dialog-progress__track", "kit-rounded-white");
            var fill = CreateElement("dialog-progress__fill", "kit-rounded-white");
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

        private void BuildButtons(DialogViewModel vm)
        {
            VisualElement row = null;
            foreach (var b in vm.Buttons)
            {
                var parent = _body;
                if (b.IsInRow)
                {
                    if (row == null)
                    {
                        row = Spawn(_body, CreateElement("dialog-row"));
                    }

                    parent = row;
                }
                else
                {
                    row = null;
                }

                var view = DialogButtonView.Create(b, vm.AdBadgeText);
                if (parent == _body)
                {
                    Spawn(_body, view.Root);
                }
                else
                {
                    parent.Add(view.Root);
                }

                if (!string.IsNullOrEmpty(b.Note))
                {
                    Spawn(_body, CreateLabel(b.Note, "dialog-note", "dialog-note--button"));
                    row = null;
                }

                var button = b;
                view.Button.OnTapAsObservable().Subscribe(_ => vm.Press(button)).AddTo(Bindings);
                b.IsEnabled.Subscribe(x => view.Button.SetEnabled(x)).AddTo(Bindings);
            }
        }
    }
}
