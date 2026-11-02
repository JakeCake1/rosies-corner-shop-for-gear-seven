using DG.Tweening;
using Game.Services.UI;
using Game.Services.UI.Elements;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Features.Gameplay.Hud
{
    /// <summary>
    /// One booster button under the cabinet (<c>Booster.uxml</c>): tile with icon, count badge, title, pulse when
    /// it would help, a pulsing icon while Rosie's gift is unused.
    /// </summary>
    public sealed class BoosterButtonView : View<BoosterViewModel>
    {
        private const string TileName = "tile";
        private const string IconName = "icon";
        private const string TitleName = "title";
        private const string BadgeName = "badge";
        private const string CountName = "count";
        private const string PlusClass = "booster__badge--plus";
        private const float PulseScale = 1.08f;
        private const float PulseSeconds = 0.45f;
        private const float GiftScale = 1.18f;
        private const float GiftSeconds = 0.5f;

        private Button _tile;
        private VisualElement _icon;
        private Label _title;
        private VisualElement _badge;
        private Label _count;
        private Tween _pulse;
        private Tween _giftTween;

        /// <summary>
        /// Sets the booster picture.
        /// </summary>
        /// <param name="icon">Icon.</param>
        public void SetIcon(Sprite icon) => _icon.SetSprite(icon);

        /// <inheritdoc/>
        protected override void OnAttached()
        {
            _tile = Q<Button>(TileName);
            _icon = Q(IconName);
            _title = Q<Label>(TitleName);
            _badge = Q(BadgeName);
            _count = Q<Label>(CountName);
        }

        /// <inheritdoc/>
        protected override void OnBind(BoosterViewModel vm)
        {
            _title.text = vm.Title;
            vm.Count.Subscribe(c => _count.text = c).AddTo(Bindings);
            vm.IsPlus.Subscribe(p => _badge.EnableInClassList(PlusClass, p)).AddTo(Bindings);
            vm.IsDimmed.Subscribe(d => Root.style.opacity = d ? 0.5f : 1f).AddTo(Bindings);
            vm.IsPulsing.Subscribe(SetPulse).AddTo(Bindings);
            vm.IsGift.Subscribe(SetGiftPulse).AddTo(Bindings);
            _tile.OnTapAsObservable().Subscribe(_ => vm.Use()).AddTo(Bindings);
        }

        /// <inheritdoc/>
        protected override void OnUnbind()
        {
            SetPulse(false);
            SetGiftPulse(false);
        }

        private void SetPulse(bool isOn)
        {
            _pulse?.Kill();
            VisualElementTweens.SetScale(Root, Vector2.one);
            if (isOn)
            {
                _pulse = Root.DOScale(PulseScale, PulseSeconds).SetLoops(-1, LoopType.Yoyo).SetId(this);
            }
        }

        private void SetGiftPulse(bool isOn)
        {
            _giftTween?.Kill();
            VisualElementTweens.SetScale(_icon, Vector2.one);
            if (isOn)
            {
                _giftTween = _icon.DOScale(GiftScale, GiftSeconds).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo)
                    .SetId(this);
            }
        }
    }
}
