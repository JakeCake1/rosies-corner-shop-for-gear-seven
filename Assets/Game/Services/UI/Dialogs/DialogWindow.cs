using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Services.UI.Dialogs
{
    /// <summary>
    /// The modal card used for every popup in the game: builds its blocks from a <see cref="DialogViewModel"/>
    /// by cloning the disabled templates inside the prefab.
    /// </summary>
    public sealed class DialogWindow : WindowView<DialogViewModel>
    {
        private const float StarDelay = 0.22f;

        [Header("Frame")]
        [SerializeField] private Image _ribbon;
        [SerializeField] private Sprite _ribbonBerry;
        [SerializeField] private Sprite _ribbonGreen;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private RectTransform _body;
        [Header("Templates (disabled in the prefab)")]
        [SerializeField] private Image _iconTemplate;
        [SerializeField] private RectTransform _starsTemplate;
        [SerializeField] private Sprite _starOn;
        [SerializeField] private Sprite _starOff;
        [SerializeField] private TMP_Text _headlineTemplate;
        [SerializeField] private TMP_Text _textTemplate;
        [SerializeField] private RectTransform _statTemplate;
        [SerializeField] private RectTransform _progressTemplate;
        [SerializeField] private RectTransform _itemsTemplate;
        [SerializeField] private RectTransform _rewardTemplate;
        [SerializeField] private RectTransform _toggleTemplate;
        [SerializeField] private RectTransform _inputTemplate;
        [SerializeField] private RectTransform _gridTemplate;
        [SerializeField] private RectTransform _segmentsTemplate;
        [SerializeField] private TMP_Text _noteTemplate;
        [SerializeField] private RectTransform _rowTemplate;
        [SerializeField] private DialogButtonView _buttonTemplate;
        [SerializeField] private Sprite[] _buttonSprites = new Sprite[5];
        [SerializeField] private Color _segmentOn = new Color(0.37f, 0.6f, 0.27f);
        [SerializeField] private Color _segmentOff = new Color(0.97f, 0.93f, 0.82f);

        private readonly List<GameObject> _spawned = new List<GameObject>();
        private bool _isClosedByBlocker;

        /// <inheritdoc/>
        public override UILayer Layer => UILayer.Popups;

        /// <inheritdoc/>
        public override bool IsClosedByBlocker => _isClosedByBlocker;

        /// <inheritdoc/>
        protected override void OnBind(DialogViewModel vm)
        {
            _isClosedByBlocker = vm.IsClosedByBlocker;
            _title.SetText(vm.Title);
            _ribbon.sprite = vm.Style == RibbonStyle.Green ? _ribbonGreen : _ribbonBerry;
            vm.CloseRequested.Subscribe(_ => RequestClose()).AddTo(Bindings);

            if (vm.Icon != null) Spawn(_iconTemplate).sprite = vm.Icon;
            if (vm.Stars.HasValue) BuildStars(vm.Stars.Value);
            if (!string.IsNullOrEmpty(vm.Headline)) Spawn(_headlineTemplate).SetText(vm.Headline);
            foreach (var (label, value) in vm.Stats)
            {
                var row = Spawn(_statTemplate);
                var texts = row.GetComponentsInChildren<TMP_Text>(true);
                texts[0].SetText(label);
                texts[1].SetText(value);
            }

            if (!string.IsNullOrEmpty(vm.Text)) Spawn(_textTemplate).SetText(vm.Text);

            if (vm.Progress.HasValue) BuildProgress(vm.Progress.Value, vm.ProgressCaption);
            if (vm.Items.Count > 0) BuildItems(vm.Items);
            if (!string.IsNullOrEmpty(vm.Reward)) Spawn(_rewardTemplate).GetComponentInChildren<TMP_Text>(true).SetText(vm.Reward);
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

            if (!string.IsNullOrEmpty(vm.Note)) Spawn(_noteTemplate).SetText(vm.Note);
            BuildButtons(vm);
        }

        /// <inheritdoc/>
        protected override void OnUnbind()
        {
            foreach (var go in _spawned)
            {
                if (go != null) Destroy(go);
            }

            _spawned.Clear();
        }

        private T Spawn<T>(T template) where T : Component
        {
            var clone = Instantiate(template, _body);
            clone.gameObject.SetActive(true);
            _spawned.Add(clone.gameObject);
            return clone;
        }

        private void BuildStars(int earned)
        {
            var row = Spawn(_starsTemplate);
            for (var i = 0; i < row.childCount; i++)
            {
                var star = row.GetChild(i).GetComponent<Image>();
                var isOn = i < earned;
                star.sprite = isOn ? _starOn : _starOff;
                if (!isOn) continue;

                star.transform.localScale = Vector3.zero;
                star.transform.DOScale(1f, 0.35f).SetDelay(0.15f + i * StarDelay).SetEase(Ease.OutBack, 2f)
                    .SetUpdate(true).SetLink(star.gameObject).SetId(this);
            }
        }

        private void BuildProgress(float fraction, string caption)
        {
            var block = Spawn(_progressTemplate);
            block.GetComponentInChildren<TMP_Text>(true).SetText(caption ?? string.Empty);
            var fill = block.Find("Track/Fill") as RectTransform;
            if (fill != null) fill.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);
        }

        private void BuildItems(List<Sprite> sprites)
        {
            var row = Spawn(_itemsTemplate);
            var template = row.GetChild(0).GetComponent<Image>();
            template.gameObject.SetActive(false);
            foreach (var s in sprites)
            {
                var img = Instantiate(template, row);
                img.sprite = s;
                img.gameObject.SetActive(true);
            }
        }

        private void BuildToggle(DialogToggle toggle)
        {
            var row = Spawn(_toggleTemplate);
            row.GetComponentInChildren<TMP_Text>(true).SetText(toggle.Label);
            var button = row.GetComponentInChildren<Button>(true);
            var knob = button.transform.Find("Knob") as RectTransform;
            var track = button.GetComponent<Image>();
            var isOn = toggle.IsOn;

            void Apply()
            {
                track.color = isOn ? _segmentOn : new Color(0.75f, 0.7f, 0.64f);
                if (knob != null) knob.anchoredPosition = new Vector2(isOn ? 11f : -11f, 0f);
            }

            Apply();
            button.OnClickAsObservable().Subscribe(_ =>
            {
                isOn = toggle.Changed(!isOn);
                Apply();
            }).AddTo(Bindings);
        }

        private void BuildInput(DialogViewModel vm)
        {
            var block = Spawn(_inputTemplate);
            var field = block.GetComponentInChildren<TMP_InputField>(true);
            field.characterLimit = vm.Input.MaxLength;
            if (field.placeholder is TMP_Text placeholder) placeholder.SetText(vm.Input.Placeholder);
            field.text = vm.InputText.Value;
            field.onValueChanged.AsObservable().Subscribe(v => vm.InputText.Value = v).AddTo(Bindings);
            var ideas = block.Find("Ideas");
            var chip = ideas.GetChild(0).GetComponent<Button>();
            chip.gameObject.SetActive(false);
            foreach (var idea in vm.Input.Ideas)
            {
                var c = Instantiate(chip, ideas);
                c.gameObject.SetActive(true);
                c.GetComponentInChildren<TMP_Text>(true).SetText(idea);
                var text = idea;
                c.OnClickAsObservable().Subscribe(_ => field.text = text).AddTo(Bindings);
            }
        }

        private void BuildGrid(DialogViewModel vm)
        {
            var grid = Spawn(_gridTemplate);
            var cell = grid.GetChild(0).GetComponent<Button>();
            cell.gameObject.SetActive(false);
            foreach (var b in vm.Grid)
            {
                var c = Instantiate(cell, grid);
                c.gameObject.SetActive(true);
                c.GetComponentInChildren<TMP_Text>(true).SetText(b.Label);
                c.GetComponent<Image>().color = b.IsHighlighted ? new Color(0.78f, 0.24f, 0.31f) : new Color(0.91f, 0.72f, 0.49f);
                var button = b;
                c.OnClickAsObservable().ThrottleFirst(TimeSpan.FromMilliseconds(300))
                    .Subscribe(_ => vm.Press(button)).AddTo(Bindings);
            }
        }

        private void BuildSegments(DialogSegments segments)
        {
            var row = Spawn(_segmentsTemplate);
            row.GetComponentInChildren<TMP_Text>(true).SetText(segments.Label);
            var options = row.Find("Options");
            var template = options.GetChild(0).GetComponent<Button>();
            template.gameObject.SetActive(false);
            var images = new List<Image>();
            for (var i = 0; i < segments.Options.Count; i++)
            {
                var b = Instantiate(template, options);
                b.gameObject.SetActive(true);
                b.GetComponentInChildren<TMP_Text>(true).SetText(segments.Options[i]);
                images.Add(b.GetComponent<Image>());
                var index = i;
                b.OnClickAsObservable().Subscribe(_ =>
                {
                    for (var k = 0; k < images.Count; k++)
                    {
                        images[k].color = k == index ? _segmentOn : _segmentOff;
                    }

                    segments.SelectedChanged(index);
                }).AddTo(Bindings);
            }

            for (var k = 0; k < images.Count; k++)
            {
                images[k].color = k == segments.Selected ? _segmentOn : _segmentOff;
            }
        }

        private void BuildButtons(DialogViewModel vm)
        {
            RectTransform row = null;
            foreach (var b in vm.Buttons)
            {
                Transform parent = _body;
                if (b.IsInRow)
                {
                    if (row == null) row = Spawn(_rowTemplate);
                    parent = row;
                }
                else
                {
                    row = null;
                }

                var view = Instantiate(_buttonTemplate, parent);
                view.gameObject.SetActive(true);
                if (parent == _body) _spawned.Add(view.gameObject);
                view.Setup(b, _buttonSprites[(int)b.Style]);
                var button = b;
                view.Button.OnClickAsObservable().ThrottleFirst(TimeSpan.FromMilliseconds(300))
                    .Subscribe(_ => vm.Press(button)).AddTo(Bindings);
                b.IsEnabled.Subscribe(x => view.Button.interactable = x).AddTo(Bindings);
            }
        }
    }
}
