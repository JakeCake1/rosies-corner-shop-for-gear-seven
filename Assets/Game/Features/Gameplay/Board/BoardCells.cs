using System.Collections.Generic;
using Game.Features.Gameplay.Model;
using Game.Services.Audio;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Features.Gameplay.Board
{
    /// <summary>
    /// Answers where things are on the board for <see cref="ItemLayerView"/>: the cell of every item, the stock tabs
    /// and the shelves. Reads the cabinet, the counter and the basket ledge after they rendered the same change.
    /// </summary>
    public sealed class BoardCells
    {
        private readonly CabinetView _cabinet;
        private readonly CounterView _counter;
        private readonly BasketLedgeView _baskets;
        private readonly BoardLayout _layout;

        /// <summary>Size of an item picture on a shelf; items leave the stock tab at this size.</summary>
        public Vector2 ItemSize => _layout.ItemSize;

        /// <summary>
        /// Creates the lookup.
        /// </summary>
        /// <param name="cabinet">Shelf cells and stock tabs.</param>
        /// <param name="counter">Counter slots.</param>
        /// <param name="baskets">Basket slots.</param>
        /// <param name="layout">Item size for items coming out of the stock.</param>
        public BoardCells(CabinetView cabinet, CounterView counter, BasketLedgeView baskets, BoardLayout layout)
        {
            _cabinet = cabinet;
            _counter = counter;
            _baskets = baskets;
            _layout = layout;
        }

        /// <summary>
        /// Returns the cell of every item of <paramref name="state"/>: shelves first, then the counter, then baskets.
        /// </summary>
        /// <param name="state">Current level state; its shelves must already have views.</param>
        /// <returns>Targets keyed by jar id.</returns>
        public Dictionary<int, BoardCell> Collect(LevelState state)
        {
            var wanted = new Dictionary<int, BoardCell>();
            AddShelfItems(state, wanted);
            for (var i = 0; i < state.Counter.Count; i++)
            {
                wanted[state.Counter[i].Id] = new BoardCell(_counter.Slot(i), state.Counter[i], 0f, true, SoundId.Counter);
            }

            AddBasketItems(state, wanted);
            return wanted;
        }

        /// <summary>
        /// Returns the center of a column's stock tab.
        /// </summary>
        /// <param name="column">Column index.</param>
        /// <returns>Panel coordinates.</returns>
        public Vector2 StockCenter(int column) => _cabinet.StockCenter(column);

        /// <summary>
        /// Returns whether an element is a counter slot.
        /// </summary>
        /// <param name="element">An item destination.</param>
        /// <returns>True for a counter slot.</returns>
        public bool IsCounterSlot(VisualElement element) => _counter.IsSlot(element);

        /// <summary>
        /// Returns the center of a block's shelf.
        /// </summary>
        /// <param name="blockId">Block id.</param>
        /// <param name="center">Panel coordinates of the shelf center.</param>
        /// <returns>False when the block has no shelf on the board.</returns>
        public bool TryGetShelfCenter(int blockId, out Vector2 center)
        {
            var found = _cabinet.TryGet(blockId, out var shelf);
            center = found ? shelf.Root.worldBound.center : Vector2.zero;
            return found;
        }

        private void AddShelfItems(LevelState state, Dictionary<int, BoardCell> wanted)
        {
            foreach (var column in state.Columns)
            {
                foreach (var block in column.Blocks)
                {
                    _cabinet.TryGet(block.Id, out var shelf);
                    for (var i = 0; i < block.Slots.Length; i++)
                    {
                        var jar = block.Slots[i];
                        if (jar != null)
                        {
                            wanted[jar.Id] = new BoardCell(shelf.Cell(i), jar, shelf.SettledAt, false, null);
                        }
                    }
                }
            }
        }

        private void AddBasketItems(LevelState state, Dictionary<int, BoardCell> wanted)
        {
            foreach (var basket in state.Baskets)
            {
                if (basket == null || !_baskets.TryGet(basket.Id, out var view))
                {
                    continue;
                }

                for (var i = 0; i < basket.Jars.Count; i++)
                {
                    wanted[basket.Jars[i].Id] = new BoardCell(view.Slot(i), basket.Jars[i], view.SettledAt, false,
                        SoundId.Clink);
                }
            }
        }
    }
}
