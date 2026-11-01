using System;
using System.Collections.Generic;
using Game.Services.UI;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Features.Album
{
    /// <summary>
    /// Full-screen album: department headers and six-per-row item grids in a scroll view.
    /// </summary>
    /// <remarks>
    /// 162 static cells are built once per opening; a recycled list is not worth it at this size (YAGNI).
    /// </remarks>
    public sealed class AlbumWindow : WindowView<AlbumViewModel>
    {
        private static readonly Color Silhouette = new Color(0.36f, 0.25f, 0.16f, 0.35f);

        [SerializeField] private RectTransform _content;
        [SerializeField] private RectTransform _headerTemplate;
        [SerializeField] private RectTransform _gridTemplate;
        [SerializeField] private RectTransform _cellTemplate;
        [SerializeField] private TMP_Text _total;
        [SerializeField] private Button _back;
        [SerializeField] private ScrollRect _scroll;
        [SerializeField] private Color _doneColor = new Color(0.93f, 0.71f, 0.25f);

        private readonly List<GameObject> _spawned = new List<GameObject>();

        /// <inheritdoc/>
        public override UILayer Layer => UILayer.Screens;

        /// <inheritdoc/>
        protected override void OnBind(AlbumViewModel vm)
        {
            _total.SetText(vm.Total);
            vm.CloseRequested.Subscribe(_ => RequestClose()).AddTo(Bindings);
            _back.OnClickAsObservable().ThrottleFirst(TimeSpan.FromMilliseconds(300)).Subscribe(_ => vm.Close()).AddTo(Bindings);
            foreach (var section in vm.Sections)
            {
                var header = Spawn(_headerTemplate, _content);
                var texts = header.GetComponentsInChildren<TMP_Text>(true);
                texts[0].SetText(section.Title);
                texts[1].SetText(section.Count);
                if (section.IsDone) texts[0].color = _doneColor;
                var grid = Spawn(_gridTemplate, _content);
                foreach (var entry in section.Entries)
                {
                    var cell = Instantiate(_cellTemplate, grid);
                    cell.gameObject.SetActive(true);
                    var image = cell.GetComponentInChildren<Image>(true);
                    image.sprite = entry.Sprite;
                    image.color = entry.IsFound ? Color.white : Silhouette;
                    cell.GetComponentInChildren<TMP_Text>(true).SetText(entry.Label);
                }
            }

            _scroll.verticalNormalizedPosition = 1f;
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

        private RectTransform Spawn(RectTransform template, RectTransform parent)
        {
            var clone = Instantiate(template, parent);
            clone.gameObject.SetActive(true);
            _spawned.Add(clone.gameObject);
            return clone;
        }
    }
}
