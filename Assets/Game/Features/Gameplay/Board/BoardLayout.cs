using Game.Features.Gameplay.Model;
using UnityEngine;

namespace Game.Features.Gameplay.Board
{
    /// <summary>
    /// Sizes and positions of the board parts for one level on one screen size: the basket ledge, the counter, the
    /// cabinet and the size of shelf rows and items. A port of the prototype's layout function, in panel pixels.
    /// </summary>
    /// <remarks>
    /// Recomputed by <see cref="BoardView"/> on every rebuild and resize; the board parts read it while rendering.
    /// </remarks>
    public sealed class BoardLayout
    {
        /// <summary>Width to height ratio of an item picture (40:64).</summary>
        public const float ItemAspect = 0.625f;

        /// <summary>Width of a basket on the ledge.</summary>
        public const float BasketWidth = 108f;

        /// <summary>Height of a basket on the ledge.</summary>
        public const float BasketHeight = 92f;

        private const float MaxContentWidth = 430f;
        private const float SidePadding = 16f;
        private const float TopPadding = 8f;
        private const float HudHeight = 48f;
        private const float BasketAreaHeight = 104f;
        private const float CounterTopGap = 6f;
        private const float CounterPadding = 30f;
        private const float MaxCounterSlot = 58f;
        private const float CounterSideSpace = 60f;
        private const float CounterSlotHeightRatio = 1.3f;
        private const float CabinetMinGap = 38f;
        private const float CabinetOverhead = 72f;
        private const float CabinetBottomGap = 8f;
        private const float CabinetSides = 24f;
        private const float ColumnPadding = 10f;
        private const float ColumnInset = 9f;
        private const float ColumnGap = 2f;
        private const float BoosterBarHeight = 100f;
        private const float MinRowHeight = 40f;
        private const float RowPadding = 10f;

        /// <summary>Panel size the layout was computed for; a different size means the board must be rebuilt.</summary>
        public Vector2 Size { get; private set; }

        /// <summary>Rect of the basket ledge in board coordinates.</summary>
        public Rect BasketArea { get; private set; }

        /// <summary>Rect of the counter panel in board coordinates.</summary>
        public Rect Counter { get; private set; }

        /// <summary>Rect of the cabinet in board coordinates.</summary>
        public Rect Cabinet { get; private set; }

        /// <summary>Size of an item picture on a shelf.</summary>
        public Vector2 ItemSize { get; private set; }

        /// <summary>Width of the content column; also the width of the ledge, the counter and the cabinet.</summary>
        public float Inner { get; private set; }

        /// <summary>Width of one counter slot.</summary>
        public float CounterSlot { get; private set; }

        /// <summary>Height of one shelf row.</summary>
        public float RowHeight { get; private set; }

        /// <summary>Width of one shelf column.</summary>
        public float ColumnWidth { get; private set; }

        /// <summary>Rows of shelves visible in a column.</summary>
        public int VisibleRows { get; private set; }

        /// <summary>Items per shelf row.</summary>
        public int JarsPerRow { get; private set; }

        /// <summary>Height of the shelf columns area.</summary>
        public float ColumnsHeight => VisibleRows * RowHeight;

        /// <summary>
        /// Computes the layout of <paramref name="state"/> on a board of <paramref name="size"/>.
        /// </summary>
        /// <param name="state">The level: counter capacity, visible rows, items per row.</param>
        /// <param name="size">Size of the board element in panel pixels.</param>
        public void Compute(LevelState state, Vector2 size)
        {
            Size = size;
            Inner = Mathf.Min(MaxContentWidth, size.x) - SidePadding * 2f;
            VisibleRows = state.VisibleRows;
            JarsPerRow = state.JarsPerRow;
            var left = (size.x - Inner) / 2f;
            var y = TopPadding + HudHeight;
            BasketArea = new Rect(left, y, Inner, BasketAreaHeight);
            y += BasketAreaHeight + CounterTopGap;
            CounterSlot = Mathf.Floor(Mathf.Min(MaxCounterSlot, (Inner - CounterSideSpace) / state.Capacity));
            var counterHeight = CounterSlot * CounterSlotHeightRatio + CounterPadding;
            Counter = new Rect(left, y, Inner, counterHeight);
            y += counterHeight;
            ComputeShelves(state, size, left, y);
        }

        /// <summary>
        /// Returns the left edge of a basket slot on the ledge (three slots spread evenly).
        /// </summary>
        /// <param name="slot">Slot index, 0..2.</param>
        /// <returns>Left edge in ledge coordinates.</returns>
        public float BasketSlotLeft(int slot) => Inner * (slot * 2 + 1) / 6f - BasketWidth / 2f;

        private void ComputeShelves(LevelState state, Vector2 size, float left, float top)
        {
            var columnWidth = (Inner - CabinetSides) / 3f;
            var maxItemWidth = (columnWidth - ColumnPadding) / state.JarsPerRow;
            var available = size.y - top - BoosterBarHeight - CabinetMinGap - CabinetBottomGap - CabinetOverhead;
            RowHeight = Mathf.Max(MinRowHeight,
                Mathf.Min(maxItemWidth / ItemAspect + RowPadding, available / state.VisibleRows));
            var itemHeight = RowHeight - RowPadding;
            ItemSize = new Vector2(Mathf.Min(itemHeight * ItemAspect, maxItemWidth), itemHeight);
            var cabinetHeight = CabinetOverhead + state.VisibleRows * RowHeight;
            var spare = Mathf.Max(0f, available - state.VisibleRows * RowHeight);
            Cabinet = new Rect(left, top + CabinetMinGap + spare / 2f, Inner, cabinetHeight);
            ColumnWidth = (Inner - ColumnInset * 2f) / LevelState.ColumnCount - ColumnGap * 2f;
        }
    }
}
