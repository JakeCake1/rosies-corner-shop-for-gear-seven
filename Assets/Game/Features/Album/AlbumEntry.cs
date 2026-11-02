using System.Collections.Generic;
using System.Linq;
using Game.Services.Content;
using Game.Services.UI;
using UniRx;
using UnityEngine;

namespace Game.Features.Album
{
    /// <summary>
    /// One album cell.
    /// </summary>
    public sealed class AlbumEntry
    {
        /// <summary>Item art.</summary>
        public Sprite Sprite { get; }

        /// <summary>Item name or "?".</summary>
        public string Label { get; }

        /// <summary>Shipped at least once.</summary>
        public bool IsFound { get; }

        /// <summary>
        /// Creates a cell.
        /// </summary>
        /// <param name="sprite">Item art (drawn as a silhouette when not found).</param>
        /// <param name="label">Item name or "?".</param>
        /// <param name="isFound">Shipped at least once.</param>
        public AlbumEntry(Sprite sprite, string label, bool isFound)
        {
            Sprite = sprite;
            Label = label;
            IsFound = isFound;
        }
    }
}
