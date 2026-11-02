using Game.Features.Gameplay.Model;
using Game.Services.Audio;
using UnityEngine.UIElements;

namespace Game.Features.Gameplay.Board
{
    /// <summary>
    /// Where an item belongs on the board after a change: its cell and how to arrive there.
    /// </summary>
    public readonly struct BoardCell
    {
        /// <summary>Shelf cell, counter slot or basket slot.</summary>
        public VisualElement Cell { get; }

        /// <summary>The item.</summary>
        public Jar Jar { get; }

        /// <summary>Unscaled time when the cell stops moving (a dropping shelf, an arriving basket).</summary>
        public float ReadyAt { get; }

        /// <summary>True for a counter slot.</summary>
        public bool IsCounter { get; }

        /// <summary>Sound when the item lands there, or null.</summary>
        public SoundId? Landing { get; }

        /// <summary>
        /// Creates the target.
        /// </summary>
        /// <param name="cell">Shelf cell, counter slot or basket slot.</param>
        /// <param name="jar">The item.</param>
        /// <param name="readyAt">Unscaled time when the cell stops moving; 0 for the counter.</param>
        /// <param name="isCounter">True for a counter slot.</param>
        /// <param name="landing">Sound when the item lands there, or null for shelves.</param>
        public BoardCell(VisualElement cell, Jar jar, float readyAt, bool isCounter, SoundId? landing)
        {
            Cell = cell;
            Jar = jar;
            ReadyAt = readyAt;
            IsCounter = isCounter;
            Landing = landing;
        }
    }
}
