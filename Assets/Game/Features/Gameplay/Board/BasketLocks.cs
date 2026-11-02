using System;
using System.Linq;
using Game.Features.Gameplay.Model;
using Game.Services.UI.Elements;
using UniRx;
using UnityEngine.UIElements;

namespace Game.Features.Gameplay.Board
{
    /// <summary>
    /// The "+" buttons of locked basket slots on the ledge. A button is created when its slot first shows a lock and
    /// is removed from the tree while the slot is open.
    /// </summary>
    public sealed class BasketLocks : IDisposable
    {
        private const string ButtonClass = "btn";
        private const string LockClass = "basket-lock";
        private const string GhostClass = "basket-lock__ghost";
        private const string PlusClass = "basket-lock__plus";
        private const string CircleClass = "kit-circle";
        private const string PlusTextClass = "basket-lock__plus-text";
        private const string PlusText = "+";

        private readonly VisualElement[] _locks = new VisualElement[LevelState.MaxBaskets];
        private readonly Subject<Unit> _clicked = new Subject<Unit>();
        private readonly CompositeDisposable _taps = new CompositeDisposable();

        /// <summary>Emits when any lock button is tapped.</summary>
        public IObservable<Unit> Clicked => _clicked;

        /// <summary>
        /// Shows or hides the lock of one slot.
        /// </summary>
        /// <param name="ledge">The basket ledge the buttons live in.</param>
        /// <param name="slot">Slot index, 0..2.</param>
        /// <param name="isVisible">True while the slot is locked and the level shows extra baskets.</param>
        /// <param name="left">Left edge of the slot on the ledge.</param>
        public void Set(VisualElement ledge, int slot, bool isVisible, float left)
        {
            if (!isVisible)
            {
                _locks[slot]?.RemoveFromHierarchy();
                return;
            }

            if (_locks[slot] == null)
            {
                _locks[slot] = CreateLock();
            }

            ledge.Add(_locks[slot]);
            _locks[slot].style.left = left;
        }

        /// <summary>
        /// Returns the first lock button that is on screen.
        /// </summary>
        /// <returns>The button, or null when every slot is open.</returns>
        public VisualElement First() => _locks.FirstOrDefault(l => l != null && l.parent != null);

        /// <inheritdoc/>
        public void Dispose()
        {
            _taps.Dispose();
            _clicked.Dispose();
        }

        private VisualElement CreateLock()
        {
            var button = new Button();
            button.AddToClassList(ButtonClass);
            button.AddToClassList(LockClass);
            var plus = CreatePart(PlusClass, CircleClass);
            var text = new Label(PlusText) { pickingMode = PickingMode.Ignore };
            text.AddToClassList(PlusTextClass);
            plus.Add(text);
            button.Add(CreatePart(GhostClass));
            button.Add(plus);
            button.OnTapAsObservable().Subscribe(_ => _clicked.OnNext(Unit.Default)).AddTo(_taps);
            return button;
        }

        private static VisualElement CreatePart(params string[] classes)
        {
            var element = new VisualElement { pickingMode = PickingMode.Ignore };
            foreach (var c in classes)
            {
                element.AddToClassList(c);
            }

            return element;
        }
    }
}
