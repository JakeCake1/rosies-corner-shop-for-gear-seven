using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Game.Features.Gameplay.Model;
using Game.Features.Gameplay.Session;
using Game.Services.Audio;
using Game.Services.Localization;
using Game.Services.Pooling;
using Game.Services.UI;
using Game.Services.UI.Elements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Features.Gameplay.Board
{
    /// <summary>
    /// The cabinet: three shelf columns under their stock tabs. Shelves drop in when they appear, slide down when a
    /// shelf below them leaves and fold away when emptied.
    /// </summary>
    public sealed class CabinetView : ElementView, IDisposable
    {
        private const string ColumnsName = "columns";
        private const string TabsName = "stock-tabs";
        private const string ColumnClass = "cabinet__column-clip";
        private const int PoolCapacity = 24;
        private const int PoolMaxSize = 60;

        private readonly VisualElement[] _columns = new VisualElement[LevelState.ColumnCount];
        private readonly Dictionary<int, ShelfView> _shelves = new Dictionary<int, ShelfView>();
        private readonly HashSet<ShelfView> _leaving = new HashSet<ShelfView>();
        private readonly VisualTreeAsset _shelfLayout;
        private readonly BoardSprites _sprites;
        private readonly IOverlayService _overlay;
        private readonly IAudioService _audio;
        private readonly ILocalizationService _texts;
        private readonly object _tweenId;
        private StockTabsView _tabs;
        private ShelfAnimator _animator;
        private VisualElement _columnsRoot;
        private ElementPool<ShelfView> _pool;

        /// <summary>The stock tabs row (anchor for the "stock" tip).</summary>
        public VisualElement StockTabs => _tabs.Root;

        /// <summary>
        /// Creates the cabinet; attach it to the board's cabinet element.
        /// </summary>
        /// <param name="shelfLayout">Shelf template.</param>
        /// <param name="sprites">Pictures of the items waiting behind shelf items.</param>
        /// <param name="overlay">Dust when a shelf folds away.</param>
        /// <param name="audio">Fold and roll sounds.</param>
        /// <param name="texts">Stock tabs and shelf row badges.</param>
        /// <param name="tweenId">Id of every shelf tween; killed by the board on rebuild.</param>
        public CabinetView(VisualTreeAsset shelfLayout, BoardSprites sprites, IOverlayService overlay,
            IAudioService audio, ILocalizationService texts, object tweenId)
        {
            _shelfLayout = shelfLayout;
            _sprites = sprites;
            _overlay = overlay;
            _audio = audio;
            _texts = texts;
            _tweenId = tweenId;
        }

        /// <summary>
        /// Sizes the columns area for the level's visible rows.
        /// </summary>
        /// <param name="layout">The board layout.</param>
        public void Layout(BoardLayout layout)
        {
            _columnsRoot.style.height = layout.ColumnsHeight;
        }

        /// <summary>
        /// Updates the stock tabs and reconciles the shelves with <paramref name="state"/>.
        /// </summary>
        /// <param name="state">Current level state.</param>
        /// <param name="change">What changed; a level start drops the shelves in column by column.</param>
        /// <param name="layout">The board layout.</param>
        /// <param name="now">Unscaled time of the change.</param>
        /// <param name="isInstant">Snap everything (undo rebuild, resize).</param>
        public void Render(LevelState state, BoardChange change, BoardLayout layout, float now, bool isInstant)
        {
            var live = new HashSet<int>();
            var isStart = change.Kind == BoardChangeKind.LevelStart;
            for (var c = 0; c < LevelState.ColumnCount; c++)
            {
                var column = state.Columns[c];
                _tabs.Render(c, column);
                var offset = 0;
                var fresh = 0;
                foreach (var block in column.Blocks)
                {
                    live.Add(block.Id);
                    var bottom = offset * layout.RowHeight;
                    if (!_shelves.TryGetValue(block.Id, out var view))
                    {
                        view = AddShelf(c, block, layout);
                        _animator.Enter(view, bottom, layout, c, fresh, isStart, now, isInstant);
                        fresh += isInstant ? 0 : 1;
                    }
                    else
                    {
                        _animator.MoveTo(view, bottom, now, isInstant);
                    }

                    ShowBlock(view, block);
                    // Bottom to top, each sent to the back of the column: the lower shelf ends up drawn last, so its "N rows"
                    // badge and "+1" stickers that stick out over the shelf above stay visible.
                    view.Root.SendToBack();
                    offset += block.Height;
                }
            }

            RemoveGone(live, isInstant);
        }

        /// <summary>
        /// Returns the shelf view of a block.
        /// </summary>
        /// <param name="blockId">Block id.</param>
        /// <param name="view">The shelf, or null.</param>
        /// <returns>False when the block has no shelf on the board.</returns>
        public bool TryGet(int blockId, out ShelfView view) => _shelves.TryGetValue(blockId, out view);

        /// <summary>
        /// Returns the center of a column's stock tab, where items come from and go back to.
        /// </summary>
        /// <param name="column">Column index.</param>
        /// <returns>Panel coordinates.</returns>
        public Vector2 StockCenter(int column) => _tabs.Center(column);

        /// <summary>
        /// Returns every shelf to the pool. Called on rebuild; shelf tweens are killed by the board.
        /// </summary>
        public void Clear()
        {
            foreach (var view in _shelves.Values.Concat(_leaving))
            {
                _pool.Release(view);
            }

            _shelves.Clear();
            _leaving.Clear();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _pool?.Dispose();
        }

        /// <inheritdoc/>
        protected override void OnAttached()
        {
            _columnsRoot = Q(ColumnsName);
            _tabs = new StockTabsView(_texts);
            _tabs.Attach(Q(TabsName));
            var columns = _columnsRoot.Query(className: ColumnClass).ToList();
            for (var c = 0; c < LevelState.ColumnCount; c++)
            {
                _columns[c] = columns[c];
            }

            _pool = new ElementPool<ShelfView>(() => LayoutViews.Create<ShelfView>(_shelfLayout), PoolCapacity,
                PoolMaxSize);
            _animator = new ShelfAnimator(_audio, _overlay, _tweenId);
        }

        private ShelfView AddShelf(int column, Block block, BoardLayout layout)
        {
            var view = _pool.Get();
            _columns[column].Add(view.Root);
            var rowsText = block.Height > 1 ? _texts.Format(GameplayTexts.ShelfRows, block.Height) : null;
            _animator.Prepare(view, block.Height, layout);
            view.Setup(block.Id, block.Height, layout.JarsPerRow, layout.ColumnWidth, layout.RowHeight,
                layout.ItemSize, rowsText, _texts.Get(GameplayTexts.BackMark));
            _shelves[block.Id] = view;
            return view;
        }

        private void ShowBlock(ShelfView view, Block block)
        {
            view.SetState(block.IsLocked, block.KeyTargetId != 0, block.Frozen, block.IsBoarded);
            for (var i = 0; i < block.Slots.Length; i++)
            {
                var back = block.Slots[i]?.Back;
                view.SetGhost(i, back != null ? _sprites.OfType(back.Type) : null);
            }
        }

        private void RemoveGone(HashSet<int> live, bool isInstant)
        {
            foreach (var id in _shelves.Keys.Where(id => !live.Contains(id)).ToList())
            {
                var view = _shelves[id];
                _shelves.Remove(id);
                if (isInstant)
                {
                    _pool.Release(view);
                    continue;
                }

                _leaving.Add(view);
                _animator.FoldAway(view, () =>
                {
                    _leaving.Remove(view);
                    _pool.Release(view);
                });
            }
        }
    }
}
