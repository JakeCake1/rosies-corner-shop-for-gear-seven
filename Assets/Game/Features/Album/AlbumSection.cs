using System.Collections.Generic;
using System.Linq;
using Game.Services.Content;
using Game.Services.UI;
using UniRx;
using UnityEngine;

namespace Game.Features.Album
{
    /// <summary>
    /// One department of the album.
    /// </summary>
    public sealed class AlbumSection
    {
        /// <summary>
        /// Creates a section.
        /// </summary>
        /// <param name="title">Department name.</param>
        /// <param name="count">"n / total · reward" line.</param>
        /// <param name="isDone">All items found (gold header).</param>
        /// <param name="entries">Items in catalog order.</param>
        public AlbumSection(string title, string count, bool isDone, List<AlbumEntry> entries)
        {
            Title = title;
            Count = count;
            IsDone = isDone;
            Entries = entries;
        }

        /// <summary>Department name.</summary>
        public string Title { get; }

        /// <summary>"n / total · reward" line.</summary>
        public string Count { get; }

        /// <summary>All items found.</summary>
        public bool IsDone { get; }

        /// <summary>Items in catalog order.</summary>
        public List<AlbumEntry> Entries { get; }
    }
}
