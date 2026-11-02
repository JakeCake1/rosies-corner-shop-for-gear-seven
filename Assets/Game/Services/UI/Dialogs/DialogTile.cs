using System;
using UnityEngine;

namespace Game.Services.UI.Dialogs
{
    /// <summary>
    /// A selectable tile (start boosters): icon, name, short info and a corner badge with the owned count or the price.
    /// Tiles of one dialog share a row.
    /// </summary>
    public sealed class DialogTile
    {
        /// <summary>Picture.</summary>
        public Sprite Icon { get; }

        /// <summary>Bold name ("Extra basket").</summary>
        public string Name { get; }

        /// <summary>Short description ("Start with three baskets").</summary>
        public string Info { get; }

        /// <summary>Called with the requested state; returns the applied state.</summary>
        public Func<bool, bool> Changed { get; }

        /// <summary>Badge text, read again after every change ("2", or a price).</summary>
        public Func<string> Badge { get; }

        /// <summary>Whether the badge shows a price (gold) rather than an owned count (red); read after every change.</summary>
        public Func<bool> IsPriceBadge { get; }

        /// <summary>
        /// Creates a tile, initially off.
        /// </summary>
        /// <param name="icon">Picture.</param>
        /// <param name="name">Bold name.</param>
        /// <param name="info">Short description.</param>
        /// <param name="changed">Called with the requested state; returns the state actually applied (a paid
        /// option can refuse to turn on).</param>
        /// <param name="badge">Badge text.</param>
        /// <param name="isPriceBadge">Whether the badge is a price.</param>
        public DialogTile(Sprite icon, string name, string info, Func<bool, bool> changed, Func<string> badge,
            Func<bool> isPriceBadge)
        {
            Icon = icon;
            Name = name;
            Info = info;
            Changed = changed;
            Badge = badge;
            IsPriceBadge = isPriceBadge;
        }
    }
}
