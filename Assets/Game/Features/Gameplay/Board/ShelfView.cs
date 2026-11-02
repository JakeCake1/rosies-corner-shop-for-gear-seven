using System.Collections.Generic;
using DG.Tweening;
using Game.Services.Pooling;
using Game.Services.UI.Elements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Features.Gameplay.Board
{
    /// <summary>
    /// One shelf block in a cabinet column (<c>Shelf.uxml</c>): floors per row, item cells with back-row ghosts,
    /// and the lock (twine), key tag, ice and nailed-boards states.
    /// </summary>
    public sealed class ShelfView : PooledElementView
    {
        private const string FloorsName = "floors";
        private const string CellsName = "cells";
        private const string BackMarksName = "back-marks";
        private const string RowsBadgeName = "rows-badge";
        private const string RowsTextName = "rows-text";
        private const string CellClass = "shelf__cell";
        private const string GhostClass = "shelf__ghost";
        private const string BackMarkClass = "shelf__back-mark";
        private const string FloorClass = "shelf__floor";
        private const string FloorKitClass = "kit-shelf-floor";
        private const string TwoRowsClass = "shelf--h2";
        private const string ThreeRowsClass = "shelf--h3";
        private const float FloorHeight = 8f;
        private const float PostWidth = 5f;
        private const float CellDrop = 3f;
        private const float BackMarkWidth = 26f;
        private const float BackMarkHeight = 16f;

        private readonly List<VisualElement> _cells = new List<VisualElement>();
        private readonly List<VisualElement> _ghosts = new List<VisualElement>();
        private readonly List<Label> _backMarks = new List<Label>();
        private readonly List<VisualElement> _floors = new List<VisualElement>();
        private VisualElement _floorLayer;
        private VisualElement _cellLayer;
        private VisualElement _backMarkLayer;
        private VisualElement _rowsBadge;
        private Label _rowsText;
        private ShelfMarks _marks;
        private string _backMarkText;
        private int _rows;
        private int _perRow;

        /// <summary>Block id shown by this view (0 when pooled).</summary>
        public int BlockId { get; private set; }

        /// <summary>Time (unscaled) when the shelf stops moving; items wait for it before flying in.</summary>
        public float SettledAt { get; set; }

        /// <summary>
        /// Prepares the shelf for a block.
        /// </summary>
        /// <param name="blockId">Block id.</param>
        /// <param name="rows">Rows in the block.</param>
        /// <param name="perRow">Items per row.</param>
        /// <param name="width">Shelf width (column width).</param>
        /// <param name="rowHeight">Row height.</param>
        /// <param name="itemSize">Item cell size.</param>
        /// <param name="rowsText">
        /// Caption of the rows badge ("3 rows"); shown only when <paramref name="rows"/> &gt; 1, may be null otherwise.
        /// </param>
        /// <param name="backMarkText">Sticker on a slot with an item behind it ("+1").</param>
        public void Setup(int blockId, int rows, int perRow, float width, float rowHeight, Vector2 itemSize,
            string rowsText, string backMarkText)
        {
            BlockId = blockId;
            _backMarkText = backMarkText;
            _rows = rows;
            _perRow = perRow;
            EnsureParts(rows * perRow, rows);
            // Frame tone by block height, as the prototype's .blk.h2 / .blk.h3.
            Root.EnableInClassList(TwoRowsClass, rows == 2);
            Root.EnableInClassList(ThreeRowsClass, rows >= 3);
            _rowsBadge.SetVisible(rows > 1);
            if (rows > 1)
            {
                _rowsText.text = rowsText;
            }

            Resize(width, rowHeight, itemSize);
        }

        /// <summary>
        /// Lays out floors and cells.
        /// </summary>
        /// <param name="width">Shelf width.</param>
        /// <param name="rowHeight">Row height.</param>
        /// <param name="itemSize">Item cell size.</param>
        public void Resize(float width, float rowHeight, Vector2 itemSize)
        {
            var inner = width - PostWidth * 2f;
            for (var r = 0; r < _rows; r++)
            {
                _floors[r].style.bottom = r * rowHeight;
            }

            for (var i = 0; i < _rows * _perRow; i++)
            {
                var row = i / _perRow;
                var col = i % _perRow;
                var cell = _cells[i];
                cell.style.width = itemSize.x;
                cell.style.height = itemSize.y;
                cell.style.left = PostWidth + inner * (col + 0.5f) / _perRow - itemSize.x / 2f;
                cell.style.bottom = row * rowHeight + FloorHeight - CellDrop;
                // Sticker at the slot's top right (.s.hasBack::after: top -2px, right 0).
                var mark = _backMarks[i];
                mark.style.left = PostWidth + inner * (col + 1f) / _perRow - BackMarkWidth;
                mark.style.bottom = (row + 1) * rowHeight - BackMarkHeight;
            }
        }

        /// <summary>
        /// Returns an item cell.
        /// </summary>
        /// <param name="slot">Slot index (row-major from the bottom).</param>
        /// <returns>The cell element.</returns>
        public VisualElement Cell(int slot) => _cells[slot];

        /// <summary>
        /// Shows the faded back-row item behind a slot and its "+1" sticker.
        /// </summary>
        /// <param name="slot">Slot index.</param>
        /// <param name="sprite">Item behind, or null.</param>
        public void SetGhost(int slot, Sprite sprite)
        {
            var g = _ghosts[slot];
            var mark = _backMarks[slot];
            g.SetVisible(sprite != null);
            mark.SetVisible(sprite != null);
            if (sprite != null)
            {
                g.SetSprite(sprite);
                mark.text = _backMarkText;
            }
        }

        /// <summary>
        /// Applies the block's mechanics.
        /// </summary>
        /// <param name="isLocked">Tied with twine.</param>
        /// <param name="hasKey">Holds the key of another shelf.</param>
        /// <param name="frozen">Moves left until the ice melts.</param>
        /// <param name="isBoarded">Nailed shut.</param>
        public void SetState(bool isLocked, bool hasKey, int frozen, bool isBoarded)
        {
            _marks.Set(isLocked, hasKey, frozen, isBoarded);
        }

        /// <summary>
        /// Knocks the nailed boards off.
        /// </summary>
        /// <returns>The animation.</returns>
        public Tween KnockBoardsOff() => _marks.KnockBoardsOff(this);

        /// <summary>
        /// Centre of the key tag in panel space.
        /// </summary>
        /// <returns>Panel point.</returns>
        public Vector2 KeyCenter() => _marks.KeyCenter();

        /// <inheritdoc/>
        public override void OnDespawned()
        {
            base.OnDespawned();
            BlockId = 0;
            _marks.Reset();
            foreach (var ghost in _ghosts)
            {
                ghost.SetVisible(false);
            }

            foreach (var mark in _backMarks)
            {
                mark.SetVisible(false);
            }
        }

        /// <inheritdoc/>
        protected override void OnAttached()
        {
            // Shelves drop and slide with translate; the hint moves the whole subtree (items included) on the GPU.
            Root.usageHints = UsageHints.DynamicTransform;
            _floorLayer = Q(FloorsName);
            _cellLayer = Q(CellsName);
            _backMarkLayer = Q(BackMarksName);
            _rowsBadge = Q(RowsBadgeName);
            _rowsText = Q<Label>(RowsTextName);
            _marks = new ShelfMarks(Root);
            _marks.Reset();
            _rowsBadge.SetVisible(false);
        }

        private static void ShowFirst<T>(List<T> parts, VisualElement layer, int count) where T : VisualElement
        {
            for (var i = 0; i < parts.Count; i++)
            {
                if (i < count)
                {
                    layer.Add(parts[i]);
                }
                else
                {
                    parts[i].RemoveFromHierarchy();
                }
            }
        }

        private void EnsureParts(int cells, int rows)
        {
            while (_cells.Count < cells)
            {
                AddCell();
            }

            while (_floors.Count < rows)
            {
                var floor = CreateElement(FloorClass, FloorKitClass);
                floor.pickingMode = PickingMode.Ignore;
                _floors.Add(floor);
            }

            ShowFirst(_cells, _cellLayer, cells);
            ShowFirst(_backMarks, _backMarkLayer, cells);
            ShowFirst(_floors, _floorLayer, rows);
        }

        private void AddCell()
        {
            var cell = CreateElement(CellClass);
            cell.pickingMode = PickingMode.Ignore;
            var ghost = CreateElement(GhostClass);
            ghost.pickingMode = PickingMode.Ignore;
            ghost.SetVisible(false);
            cell.Add(ghost);
            // Items are snapped into the cell, so the sticker lives on its own layer above the cells (z-index 6).
            var mark = new Label { pickingMode = PickingMode.Ignore };
            mark.AddToClassList(BackMarkClass);
            mark.SetVisible(false);
            _cells.Add(cell);
            _ghosts.Add(ghost);
            _backMarks.Add(mark);
        }
    }
}
