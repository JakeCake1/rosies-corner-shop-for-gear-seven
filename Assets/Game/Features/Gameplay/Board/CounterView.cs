using System.Collections.Generic;
using Game.Features.Gameplay.Model;
using Game.Services.UI.Elements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Features.Gameplay.Board
{
    /// <summary>
    /// The counter panel: one doily slot per counter place, which glow when the counter is almost full. Items on the
    /// counter are drawn by <see cref="ItemLayerView"/> on top of the slots.
    /// </summary>
    public sealed class CounterView : ElementView
    {
        private const string SlotsName = "counter-slots";
        private const string SlotClass = "counter-slot";
        private const string DoilyClass = "counter-slot__doily";
        private const string HotDoilyClass = "counter-slot__doily--hot";
        private const int MinSlotsWide = 5;
        private const float SlotGap = 8f;
        private const float SlotBottom = 24f;
        private const float SlotHeightShare = 0.62f;
        private const float ShakeSeconds = 0.4f;
        private const float ShakeDistance = 10f;
        private const int ShakeVibrato = 18;

        private readonly List<VisualElement> _slots = new List<VisualElement>();
        private readonly List<VisualElement> _doilies = new List<VisualElement>();
        private VisualElement _slotLayer;

        /// <summary>
        /// Shows one slot per counter place, sized and spaced for the counter width.
        /// </summary>
        /// <param name="capacity">Counter places of the level.</param>
        /// <param name="slot">Width of one slot from <see cref="BoardLayout.CounterSlot"/>.</param>
        /// <param name="inner">Counter width.</param>
        public void Layout(int capacity, float slot, float inner)
        {
            while (_slots.Count < capacity)
            {
                AddSlot();
            }

            var count = Mathf.Max(capacity, MinSlotsWide);
            var total = count * slot + (count - 1) * SlotGap;
            for (var i = 0; i < _slots.Count; i++)
            {
                if (i >= capacity)
                {
                    _slots[i].RemoveFromHierarchy();
                    continue;
                }

                PlaceSlot(_slots[i], (inner - total) / 2f + i * (slot + SlotGap), slot);
            }
        }

        /// <summary>
        /// Makes the occupied doilies glow when one place or none is left.
        /// </summary>
        /// <param name="state">Current level state.</param>
        public void Render(LevelState state)
        {
            var isHot = state.Counter.Count >= state.Capacity - 1;
            for (var i = 0; i < _doilies.Count; i++)
            {
                _doilies[i].EnableInClassList(HotDoilyClass, isHot && i < state.Counter.Count);
            }
        }

        /// <summary>
        /// Shakes the panel when the counter refuses an item.
        /// </summary>
        public void Shake()
        {
            Root.Kill(true);
            Root.DOShake(ShakeSeconds, new Vector2(ShakeDistance, 0f), ShakeVibrato);
        }

        /// <summary>
        /// Returns the slot element that the item at counter place <paramref name="index"/> sits in.
        /// </summary>
        /// <param name="index">Counter place, 0 on the left.</param>
        /// <returns>The slot element.</returns>
        public VisualElement Slot(int index) => _slots[index];

        /// <summary>
        /// Returns whether an item's destination is a counter slot (items sliding along the counter only hop).
        /// </summary>
        /// <param name="element">An item destination.</param>
        /// <returns>True for a counter slot.</returns>
        public bool IsSlot(VisualElement element) => _slots.Contains(element);

        /// <inheritdoc/>
        protected override void OnAttached()
        {
            _slotLayer = Q(SlotsName);
        }

        private void AddSlot()
        {
            var cell = CreateElement(SlotClass);
            cell.pickingMode = PickingMode.Ignore;
            var doily = CreateElement(DoilyClass);
            doily.pickingMode = PickingMode.Ignore;
            cell.Add(doily);
            _slots.Add(cell);
            _doilies.Add(doily);
        }

        private void PlaceSlot(VisualElement cell, float left, float slot)
        {
            _slotLayer.Add(cell);
            cell.style.width = slot;
            cell.style.height = slot / BoardLayout.ItemAspect * SlotHeightShare;
            cell.style.left = left;
            cell.style.bottom = SlotBottom;
        }
    }
}
