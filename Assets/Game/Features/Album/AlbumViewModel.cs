using System.Collections.Generic;
using System.Linq;
using Game.Services.Album;
using Game.Services.Content;
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
        public AlbumViewModel(IContentService content, IAlbumService album)
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
                Sections.Add(new AlbumSection(catalog.FamilyName(family),
                    $"{n} / {items.Count} · " + (isDone ? $"collected, +{AlbumService.FamilyBonus}" : $"{AlbumService.FamilyBonus} coins when complete"),
                    isDone,
                    items.Select(i => new AlbumEntry(i.Sprite, found.Contains(i.Id) ? i.Name : "?", found.Contains(i.Id))).ToList()));
            }

            Total = $"<b>{got}</b> / {catalog.Items.Count}";
        }

        /// <summary>Departments in album order.</summary>
        public List<AlbumSection> Sections { get; } = new List<AlbumSection>();

        /// <summary>"<b>found</b> / total".</summary>
        public string Total { get; }

        /// <summary>Emits when the back button is tapped.</summary>
        public System.IObservable<Unit> CloseRequested => _closeRequested;

        /// <summary>
        /// Back button.
        /// </summary>
        public void Close() => _closeRequested.OnNext(Unit.Default);
    }
}
