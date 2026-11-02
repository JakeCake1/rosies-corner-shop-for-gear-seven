using System.Collections.Generic;
using System.Linq;
using Game.Services.Album;
using Game.Services.Content;
using Game.Services.Localization;
using Game.Services.UI;
using UniRx;
using UnityEngine;

namespace Game.Features.Album
{
    /// <summary>
    /// The album: every catalog item by department, found ones with their name, the rest as silhouettes.
    /// </summary>
    public sealed class AlbumViewModel : ViewModel
    {
        private readonly Subject<Unit> _closeRequested = new Subject<Unit>();

        /// <summary>
        /// Builds the album content from the catalog and the found items.
        /// </summary>
        /// <param name="content">Item catalog.</param>
        /// <param name="album">Found items and completed departments.</param>
        /// <param name="texts">Section, item and sign texts; resolved once, the album is rebuilt on every open.</param>
        public AlbumViewModel(IContentService content, IAlbumService album, ILocalizationService texts)
        {
            Disposables.Add(_closeRequested);
            var catalog = content.Catalog;
            var found = new HashSet<int>(album.Found);
            var got = 0;
            foreach (var family in catalog.Families)
            {
                var items = catalog.Items.Where(i => i.Family == family).ToList();
                var n = items.Count(i => found.Contains(i.Id));
                got += n;
                var isDone = album.IsFamilyCompleted(family);
                var count = texts.Format(isDone ? AlbumTexts.SectionDone : AlbumTexts.SectionOpen, n, items.Count,
                    AlbumService.FamilyBonus);
                var unknown = texts.Get(AlbumTexts.UnknownItem);
                var entries = items
                    .Select(i => new AlbumEntry(i.Sprite, found.Contains(i.Id) ? texts.GetItemName(i.Id) : unknown,
                        found.Contains(i.Id)))
                    .ToList();
                Sections.Add(new AlbumSection(texts.GetFamilyName(family), count, isDone, entries));
            }

            Total = texts.Format(AlbumTexts.Total, got, catalog.Items.Count);
            BackText = texts.Get(AlbumTexts.Back);
            Title = texts.Get(AlbumTexts.Title);
            Subtitle = texts.Get(AlbumTexts.Subtitle);
        }

        /// <summary>Departments in album order.</summary>
        public List<AlbumSection> Sections { get; } = new List<AlbumSection>();

        /// <summary>"<b>found</b> / total".</summary>
        public string Total { get; }

        /// <summary>Caption of the back button.</summary>
        public string BackText { get; }

        /// <summary>Title on the album sign ("My Album").</summary>
        public string Title { get; }

        /// <summary>Line under the sign title.</summary>
        public string Subtitle { get; }

        /// <summary>Emits when the back button is tapped.</summary>
        public System.IObservable<Unit> CloseRequested => _closeRequested;

        /// <summary>
        /// Back button.
        /// </summary>
        public void Close() => _closeRequested.OnNext(Unit.Default);
    }
}
