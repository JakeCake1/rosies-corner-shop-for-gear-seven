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
    /// it would help, glow when it was just gifted.
    /// </summary>
    public sealed class BoosterButtonView : View<BoosterViewModel>
    {
        private const string TileName = "tile";
        private const string IconName = "icon";
        private const string TitleName = "title";
        private const string BadgeName = "badge";
        private const string CountName = "count";
        private const string GlowName = "glow";
        private const string PlusClass = "booster__badge--plus";

        private Button _tile;
        private VisualElement _icon;
        private Label _title;
        private VisualElement _badge;
        private Label _count;
        private VisualElement _glow;
        private Tween _pulse;
        private Tween _glowTween;

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
            _glow = Q(GlowName);
            _glow.SetVisible(false);
        }

        /// <inheritdoc/>
        protected override void OnBind(BoosterViewModel vm)
        {
            _title.text = vm.Title;
            vm.Count.Subscribe(c => _count.text = c).AddTo(Bindings);
            vm.IsPlus.Subscribe(p => _badge.EnableInClassList(PlusClass, p)).AddTo(Bindings);
            vm.IsDimmed.Subscribe(d => Root.style.opacity = d ? 0.5f : 1f).AddTo(Bindings);
            vm.IsPulsing.Subscribe(SetPulse).AddTo(Bindings);
            vm.IsGift.Subscribe(SetGlow).AddTo(Bindings);
            _tile.OnTapAsObservable().Subscribe(_ => vm.Use()).AddTo(Bindings);
        }

        /// <inheritdoc/>
        protected override void OnUnbind()
        {
            SetPulse(false);
            SetGlow(false);
        }

        private void SetPulse(bool isOn)
        {
            _pulse?.Kill();
            VisualElementTweens.SetScale(Root, Vector2.one);
            if (isOn)
            {
                _pulse = Root.DOScale(1.08f, 0.45f).SetLoops(-1, LoopType.Yoyo).SetId(this);
            }
        }

        private void SetGlow(bool isOn)
        {
            _glowTween?.Kill();
            _glow.SetVisible(isOn);
            if (!isOn)
            {
                return;
            }

            _glow.style.opacity = 0.9f;
            _glowTween = _glow.DOFade(0.3f, 0.6f).SetLoops(-1, LoopType.Yoyo).SetId(this);
        }
    }
}
