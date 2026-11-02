using Game.Services.UI;
using Game.Services.UI.Elements;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Features.Album
{
    /// <summary>
    /// The album: departments with their items; found ones in colour with a name, the rest as silhouettes.
    /// </summary>
    public sealed class AlbumWindow : WindowView<AlbumViewModel>
    {
        private const string TotalName = "total";
        private const string BackName = "back";
        private const string BackTextName = "back-text";
        private const string TitleName = "title";
        private const string SubtitleName = "subtitle";
        private const string ScrollName = "scroll";
        private const int PerRow = 6;
        private static readonly Color Silhouette = new Color(0.36f, 0.25f, 0.16f, 0.35f);

        private VisualElement _content;
        private Label _total;
        private Label _backText;
        private Label _title;
        private Label _subtitle;
        private Button _back;
        private ScrollView _scroll;

        /// <inheritdoc/>
        public override UILayer Layer => UILayer.Screens;

        /// <inheritdoc/>
        protected override void OnLayoutAttached()
        {
            _scroll = Q<ScrollView>(ScrollName);
            _content = _scroll.contentContainer;
            _total = Q<Label>(TotalName);
            _back = Q<Button>(BackName);
            _backText = Q<Label>(BackTextName);
            _title = Q<Label>(TitleName);
            _subtitle = Q<Label>(SubtitleName);
        }

        /// <inheritdoc/>
        protected override void OnBind(AlbumViewModel vm)
        {
            _total.text = vm.Total;
            _backText.text = vm.BackText;
            _title.text = vm.Title;
            _subtitle.text = vm.Subtitle;
            vm.CloseRequested.Subscribe(_ => RequestClose()).AddTo(Bindings);
            OnTap(_back, vm.Close);
            foreach (var section in vm.Sections)
            {
                var header = Spawn(_content, CreateElement("album-section"));
                var title = CreateLabel(section.Title, "album-section__title");
                title.EnableInClassList("album-section__title--done", section.IsDone);
                header.Add(title);
                header.Add(CreateLabel(section.Count, "album-section__count"));
                for (var first = 0; first < section.Entries.Count; first += PerRow)
                {
                    var row = Spawn(_content, CreateElement("album-row"));
                    for (var i = first; i < first + PerRow && i < section.Entries.Count; i++)
                    {
                        row.Add(CreateCell(section.Entries[i]));
                    }

                    Spawn(_content, CreateElement("album-plank", "kit-plank"));
                }
            }

            _scroll.scrollOffset = Vector2.zero;
        }

        private static VisualElement CreateCell(AlbumEntry entry)
        {
            var cell = CreateElement("album-cell");
            var image = CreateElement("album-cell__image");
            image.SetSprite(entry.Sprite);
            image.SetTint(entry.IsFound ? Color.white : Silhouette);
            cell.Add(image);
            cell.Add(CreateLabel(entry.Label, "album-cell__name"));
            return cell;
        }
    }
}
