using System;
using Game.Services.UI;
using UniRx;

namespace Game.Features.Gameplay.Hud
{
    /// <summary>
    /// One button of the booster bar: count badge or "+", availability, pulsing and the gift glow.
    /// </summary>
    public sealed class BoosterViewModel : ViewModel
    {
        private readonly ReactiveProperty<string> _count = new ReactiveProperty<string>("+");
        private readonly ReactiveProperty<bool> _isPlus = new ReactiveProperty<bool>(true);
        private readonly ReactiveProperty<bool> _isDimmed = new ReactiveProperty<bool>();
        private readonly ReactiveProperty<bool> _isPulsing = new ReactiveProperty<bool>();
        private readonly ReactiveProperty<bool> _isGift = new ReactiveProperty<bool>();
        private readonly Action<string> _use;

        /// <summary>
        /// Creates a booster button model.
        /// </summary>
        /// <param name="id">Booster id from <c>BoosterIds</c>.</param>
        /// <param name="title">Label under the icon.</param>
        /// <param name="use">Called with <paramref name="id"/> when tapped.</param>
        public BoosterViewModel(string id, string title, Action<string> use)
        {
            Id = id;
            Title = title;
            _use = use;
            Disposables.Add(_count);
            Disposables.Add(_isPlus);
            Disposables.Add(_isDimmed);
            Disposables.Add(_isPulsing);
            Disposables.Add(_isGift);
        }

        /// <summary>Booster id; also selects the icon in the view.</summary>
        public string Id { get; }

        /// <summary>Label under the icon ("Quick Pick").</summary>
        public string Title { get; }

        /// <summary>Owned count, or "+" when a tap opens the purchase offer.</summary>
        public IReadOnlyReactiveProperty<string> Count => _count;

        /// <summary>Green "+" badge instead of the red count.</summary>
        public IReadOnlyReactiveProperty<bool> IsPlus => _isPlus;

        /// <summary>Faded (Undo before the first move).</summary>
        public IReadOnlyReactiveProperty<bool> IsDimmed => _isDimmed;

        /// <summary>Pulses to draw attention (Undo when the counter is almost full).</summary>
        public IReadOnlyReactiveProperty<bool> IsPulsing => _isPulsing;

        /// <summary>Glows after Rosie's gift until first used.</summary>
        public IReadOnlyReactiveProperty<bool> IsGift => _isGift;

        /// <summary>
        /// Updates the button from the presenter.
        /// </summary>
        /// <param name="count">Owned count; 0 or less shows "+".</param>
        /// <param name="isDimmed">Faded.</param>
        /// <param name="isPulsing">Pulsing.</param>
        public void Set(int count, bool isDimmed, bool isPulsing)
        {
            _count.Value = count > 0 ? count.ToString() : "+";
            _isPlus.Value = count <= 0;
            _isDimmed.Value = isDimmed;
            _isPulsing.Value = isPulsing;
        }

        /// <summary>
        /// Turns the gift glow on or off.
        /// </summary>
        /// <param name="isOn">Glow while the gift is unused.</param>
        public void SetGift(bool isOn) => _isGift.Value = isOn;

        /// <summary>
        /// Called by the view when tapped.
        /// </summary>
        public void Use()
        {
            _isGift.Value = false;
            _use(Id);
        }
    }
}
