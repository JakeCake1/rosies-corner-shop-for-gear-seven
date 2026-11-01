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
        private const string ContentName = "content";
        private const string FloorsName = "floors";
        private const string CellsName = "cells";
        private const string RowsBadgeName = "rows-badge";
        private const string RowsTextName = "rows-text";
        private const string TiesName = "ties";
        private const string KeyTagName = "key-tag";
        private const string IceName = "ice";
        private const string IceTextName = "ice-text";
        private const string BoardsName = "boards";
        private const string BoardClass = "shelf__board";
        private const float FloorHeight = 8f;
        private const float PostWidth = 5f;
        private const float CellLift = 5f;

        private readonly List<VisualElement> _cells = new List<VisualElement>();
        private readonly List<VisualElement> _ghosts = new List<VisualElement>();
        private readonly List<VisualElement> _floors = new List<VisualElement>();
        private readonly List<VisualElement> _boards = new List<VisualElement>();
        private VisualElement _content;
        private VisualElement _floorLayer;
        private VisualElement _cellLayer;
        private VisualElement _rowsBadge;
        private Label _rowsText;
        private VisualElement _ties;
        private VisualElement _keyTag;
        private VisualElement _ice;
        private Label _iceText;
        private VisualElement _boardLayer;
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
        public void Setup(int blockId, int rows, int perRow, float width, float rowHeight, Vector2 itemSize,
            string rowsText)
        {
            BlockId = blockId;
            _rows = rows;
            _perRow = perRow;
            EnsureParts(rows * perRow, rows);
            _rowsBadge.SetVisible(rows > 1);
            if (rows > 1) _rowsText.text = rowsText;
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
                cell.style.bottom = row * rowHeight + FloorHeight - 3f;
            }
        }

        /// <summary>
        /// Returns an item cell.
        /// </summary>
        /// <param name="slot">Slot index (row-major from the bottom).</param>
        /// <returns>The cell element.</returns>
        public VisualElement Cell(int slot) => _cells[slot];

        /// <summary>
        /// Shows the faded back-row item behind a slot.
        /// </summary>
        /// <param name="slot">Slot index.</param>
        /// <param name="sprite">Item behind, or null.</param>
        public void SetGhost(int slot, Sprite sprite)
        {
            var g = _ghosts[slot];
            g.SetVisible(sprite != null);
            if (sprite != null) g.SetSprite(sprite);
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
            _ties.SetVisible(isLocked);
            _keyTag.SetVisible(hasKey);
            _ice.SetVisible(frozen > 0);
            if (frozen > 0) _iceText.text = frozen.ToString();
            _boardLayer.SetVisible(isBoarded);
            _content.style.opacity = isLocked ? 0.7f : 1f;
            _cellLayer.SetVisible(!isBoarded);
        }

        /// <summary>
        /// Knocks the nailed boards off.
        /// </summary>
        /// <returns>The animation.</returns>
        public Tween KnockBoardsOff()
        {
            var seq = DOTween.Sequence().SetId(this);
            for (var i = 0; i < _boards.Count; i++)
            {
                var b = _boards[i];
                var dir = i % 2 == 0 ? -1f : 1f;
                seq.Join(b.DOTranslate(new Vector2(dir * 60f, 40f), 0.45f).SetEase(Ease.InQuad));
                seq.Join(b.DORotate(VisualElementTweens.GetRotation(b) + dir * 40f, 0.45f));
                seq.Join(b.DOFade(0f, 0.45f));
            }

            return seq;
        }

        /// <summary>
        /// Centre of the key tag in panel space.
        /// </summary>
        /// <returns>Panel point.</returns>
        public Vector2 KeyCenter() => _keyTag.worldBound.center;

        /// <inheritdoc/>
        public override void OnDespawned()
        {
            base.OnDespawned();
            BlockId = 0;
            SetState(false, false, 0, false);
            foreach (var g in _ghosts)
            {
                g.SetVisible(false);
            }

            foreach (var b in _boards)
            {
                b.Kill();
                b.style.translate = StyleKeyword.Null;
                b.style.rotate = StyleKeyword.Null;
                b.style.opacity = StyleKeyword.Null;
            }
        }

        /// <inheritdoc/>
        protected override void OnAttached()
        {
            _content = Q(ContentName);
            _floorLayer = Q(FloorsName);
            _cellLayer = Q(CellsName);
            _rowsBadge = Q(RowsBadgeName);
            _rowsText = Q<Label>(RowsTextName);
            _ties = Q(TiesName);
            _keyTag = Q(KeyTagName);
            _ice = Q(IceName);
            _iceText = Q<Label>(IceTextName);
            _boardLayer = Q(BoardsName);
            _boardLayer.Query(className: BoardClass).ForEach(_boards.Add);
            SetState(false, false, 0, false);
            _rowsBadge.SetVisible(false);
        }

        private void EnsureParts(int cells, int rows)
        {
            while (_cells.Count < cells)
            {
                var cell = CreateElement("shelf__cell");
                cell.pickingMode = PickingMode.Ignore;
                var ghost = CreateElement("shelf__ghost");
                ghost.pickingMode = PickingMode.Ignore;
                ghost.SetVisible(false);
                cell.Add(ghost);
                _cells.Add(cell);
                _ghosts.Add(ghost);
            }

            for (var i = 0; i < _cells.Count; i++)
            {
                if (i < cells) _cellLayer.Add(_cells[i]);
                else _cells[i].RemoveFromHierarchy();
            }

            while (_floors.Count < rows)
            {
                var floor = CreateElement("shelf__floor", "kit-shelf-floor");
                floor.pickingMode = PickingMode.Ignore;
                _floors.Add(floor);
            }

            for (var i = 0; i < _floors.Count; i++)
            {
                if (i < rows) _floorLayer.Add(_floors[i]);
                else _floors[i].RemoveFromHierarchy();
            }
        }
    }
}
