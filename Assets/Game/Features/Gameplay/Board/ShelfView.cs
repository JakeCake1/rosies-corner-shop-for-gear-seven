using System.Collections.Generic;
using DG.Tweening;
using Game.Services.Pooling;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Features.Gameplay.Board
{
    /// <summary>
    /// A shelf in a cabinet column: wooden frame with one floor plank per row, item cells, and the state overlays
    /// (twine and seal, brass key, ice, boards, "2 rows" badge). Pooled; laid out by <see cref="BoardView"/>.
    /// </summary>
    public sealed class ShelfView : MonoBehaviour, IPoolable
    {
        private const float FloorHeight = 8f;
        private const float PostWidth = 5f;
        private const float CapHeight = 6f;
        private const float GhostAlpha = 0.5f;
        private static readonly Vector2 GhostOffset = new Vector2(7f, 5f);

        [SerializeField] private RectTransform _rect;
        [SerializeField] private RectTransform _floors;
        [SerializeField] private RectTransform _cells;
        [SerializeField] private Image _floorPrefab;
        [SerializeField] private GameObject _rowsBadge;
        [SerializeField] private TMP_Text _rowsText;
        [SerializeField] private GameObject _ties;
        [SerializeField] private RectTransform _keyTag;
        [SerializeField] private GameObject _ice;
        [SerializeField] private TMP_Text _iceText;
        [SerializeField] private RectTransform _boards;
        [SerializeField] private CanvasGroup _contentGroup;

        private readonly List<RectTransform> _cellList = new List<RectTransform>();
        private readonly List<Image> _ghosts = new List<Image>();
        private readonly List<Image> _floorList = new List<Image>();
        private readonly List<(Vector2 Pos, Quaternion Rot)> _boardRest = new List<(Vector2, Quaternion)>();
        private int _rows;
        private int _perRow;

        /// <summary>The shelf's rect; the board positions it from the column bottom.</summary>
        public RectTransform Rect => _rect;

        /// <summary>Shelf id; 0 while pooled.</summary>
        public int BlockId { get; private set; }

        /// <summary>
        /// When the shelf's drop or slide animation ends (unscaled time); items wait for it before landing.
        /// </summary>
        public float SettledAt { get; set; }

        /// <summary>
        /// Builds the cells for a shelf of <paramref name="rows"/> × <paramref name="perRow"/>.
        /// </summary>
        /// <param name="blockId">Shelf id.</param>
        /// <param name="rows">Height in rows.</param>
        /// <param name="perRow">Items per row.</param>
        /// <param name="rowHeight">Row height in canvas units.</param>
        /// <param name="itemSize">Item cell size (40:64 aspect).</param>
        public void Setup(int blockId, int rows, int perRow, float rowHeight, Vector2 itemSize)
        {
            BlockId = blockId;
            _rows = rows;
            _perRow = perRow;
            EnsureCells(rows * perRow, rows);
            _rowsBadge.SetActive(rows > 1);
            if (rows > 1) _rowsText.SetText("{0} rows", rows);
            Resize(rowHeight, itemSize);
        }

        /// <summary>
        /// Lays out floors and cells for a row height; called on setup and when the screen size changes.
        /// </summary>
        /// <param name="rowHeight">Row height.</param>
        /// <param name="itemSize">Item cell size.</param>
        public void Resize(float rowHeight, Vector2 itemSize)
        {
            var width = _rect.rect.width;
            var inner = width - PostWidth * 2f;
            for (var r = 0; r < _rows; r++)
            {
                var floor = _floorList[r].rectTransform;
                floor.anchorMin = new Vector2(0f, 0f);
                floor.anchorMax = new Vector2(1f, 0f);
                floor.pivot = new Vector2(0.5f, 0f);
                floor.sizeDelta = new Vector2(0f, FloorHeight);
                floor.anchoredPosition = new Vector2(0f, r * rowHeight);
            }

            for (var i = 0; i < _rows * _perRow; i++)
            {
                var row = i / _perRow;
                var col = i % _perRow;
                var cell = _cellList[i];
                cell.anchorMin = cell.anchorMax = new Vector2(0f, 0f);
                cell.pivot = new Vector2(0.5f, 0f);
                cell.sizeDelta = itemSize;
                var x = PostWidth + inner * (col + 0.5f) / _perRow;
                cell.anchoredPosition = new Vector2(x, row * rowHeight + FloorHeight - 3f);
            }
        }

        /// <summary>
        /// Returns the cell for a slot index (row-major from the bottom).
        /// </summary>
        /// <param name="slot">Slot index.</param>
        /// <returns>The cell rect items are parented to.</returns>
        public RectTransform Cell(int slot) => _cellList[slot];

        /// <summary>
        /// Shows or hides the faded second-row item behind a cell.
        /// </summary>
        /// <param name="slot">Slot index.</param>
        /// <param name="sprite">Second-row item art, or null for none.</param>
        public void SetGhost(int slot, Sprite sprite)
        {
            var g = _ghosts[slot];
            g.gameObject.SetActive(sprite != null);
            if (sprite != null) g.sprite = sprite;
        }

        /// <summary>
        /// Applies the shelf state overlays.
        /// </summary>
        /// <param name="isLocked">Tied with twine.</param>
        /// <param name="hasKey">Holds the brass key of a tied shelf.</param>
        /// <param name="frozen">Baskets until thaw; 0 when not frozen.</param>
        /// <param name="isBoarded">Nailed shut (contents hidden).</param>
        public void SetState(bool isLocked, bool hasKey, int frozen, bool isBoarded)
        {
            _ties.SetActive(isLocked);
            _keyTag.gameObject.SetActive(hasKey);
            _ice.SetActive(frozen > 0);
            if (frozen > 0) _iceText.SetText("{0}", frozen);
            _boards.gameObject.SetActive(isBoarded);
            _contentGroup.alpha = isLocked ? 0.7f : 1f;
            _cells.gameObject.SetActive(!isBoarded);
        }

        /// <summary>
        /// Plays the boards flying off (the hammer hit) before hiding them.
        /// </summary>
        /// <returns>The animation.</returns>
        public Tween KnockBoardsOff()
        {
            var seq = DOTween.Sequence().SetLink(gameObject);
            for (var i = 0; i < _boards.childCount; i++)
            {
                var b = (RectTransform)_boards.GetChild(i);
                var dir = i % 2 == 0 ? -1f : 1f;
                seq.Join(b.DOAnchorPos(b.anchoredPosition + new Vector2(dir * 60f, -40f), 0.45f).SetEase(Ease.InQuad));
                seq.Join(b.DORotate(new Vector3(0, 0, dir * 40f), 0.45f));
            }

            return seq;
        }

        /// <summary>
        /// World position of the key tag (the key flies from here to the tied shelf).
        /// </summary>
        /// <returns>Key tag position.</returns>
        public Vector3 KeyPosition() => _keyTag.position;

        /// <inheritdoc/>
        public void OnSpawned()
        {
            _rect.localScale = Vector3.one;
        }

        /// <inheritdoc/>
        public void OnDespawned()
        {
            _rect.DOKill();
            DOTween.Kill(this);
            foreach (Transform b in _boards)
            {
                b.DOKill();
            }

            BlockId = 0;
            SetState(false, false, 0, false);
            for (var i = 0; i < _ghosts.Count; i++)
            {
                _ghosts[i].gameObject.SetActive(false);
            }

            ResetBoards();
        }

        private void Awake()
        {
            _floorPrefab.gameObject.SetActive(false);
            RecordBoardPositions();
        }

        private void RecordBoardPositions()
        {
            _boardRest.Clear();
            foreach (RectTransform b in _boards)
            {
                _boardRest.Add((b.anchoredPosition, b.localRotation));
            }
        }

        private void ResetBoards()
        {
            for (var i = 0; i < _boards.childCount && i < _boardRest.Count; i++)
            {
                var b = (RectTransform)_boards.GetChild(i);
                b.anchoredPosition = _boardRest[i].Pos;
                b.localRotation = _boardRest[i].Rot;
            }
        }

        private void EnsureCells(int count, int rows)
        {
            while (_cellList.Count < count)
            {
                var cell = new GameObject("Cell", typeof(RectTransform)).GetComponent<RectTransform>();
                cell.SetParent(_cells, false);
                var ghost = new GameObject("Ghost", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                ghost.rectTransform.SetParent(cell, false);
                ghost.rectTransform.anchorMin = Vector2.zero;
                ghost.rectTransform.anchorMax = Vector2.one;
                ghost.rectTransform.offsetMin = GhostOffset;
                ghost.rectTransform.offsetMax = GhostOffset;
                ghost.color = new Color(0.85f, 0.85f, 0.85f, GhostAlpha);
                ghost.raycastTarget = false;
                ghost.preserveAspect = true;
                ghost.gameObject.SetActive(false);
                _cellList.Add(cell);
                _ghosts.Add(ghost);
            }

            for (var i = 0; i < _cellList.Count; i++)
            {
                _cellList[i].gameObject.SetActive(i < count);
            }

            while (_floorList.Count < rows)
            {
                var floor = Instantiate(_floorPrefab, _floors);
                floor.gameObject.SetActive(true);
                _floorList.Add(floor);
            }

            for (var i = 0; i < _floorList.Count; i++)
            {
                _floorList[i].gameObject.SetActive(i < rows);
            }
        }
    }
}
