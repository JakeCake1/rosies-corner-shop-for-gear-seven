using System;
using DG.Tweening;
using Game.Services.UI;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Features.Gameplay.Hud
{
    /// <summary>
    /// A booster button: icon tile, count badge (red count or green "+"), label, pulse and gift glow.
    /// </summary>
    public sealed class BoosterButtonView : View<BoosterViewModel>
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private Image _badge;
        [SerializeField] private TMP_Text _count;
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private Image _glow;
        [SerializeField] private Color _countColor = new Color(0.78f, 0.24f, 0.31f);
        [SerializeField] private Color _plusColor = new Color(0.37f, 0.6f, 0.27f);

        private Tween _pulse;
        private Tween _glowTween;

        /// <summary>
        /// Sets the icon; the board picks it by booster id.
        /// </summary>
        /// <param name="icon">Booster icon.</param>
        public void SetIcon(Sprite icon) => _icon.sprite = icon;

        /// <inheritdoc/>
        protected override void OnBind(BoosterViewModel vm)
        {
            gameObject.SetActive(true);
            _title.SetText(vm.Title);
            vm.Count.Subscribe(c => _count.SetText(c)).AddTo(Bindings);
            vm.IsPlus.Subscribe(p => _badge.color = p ? _plusColor : _countColor).AddTo(Bindings);
            vm.IsDimmed.Subscribe(d => _group.alpha = d ? 0.5f : 1f).AddTo(Bindings);
            vm.IsPulsing.Subscribe(SetPulse).AddTo(Bindings);
            vm.IsGift.Subscribe(SetGlow).AddTo(Bindings);
            _button.OnClickAsObservable().ThrottleFirst(TimeSpan.FromMilliseconds(300))
                .Subscribe(_ => vm.Use()).AddTo(Bindings);
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
            transform.localScale = Vector3.one;
            if (isOn) _pulse = transform.DOScale(1.08f, 0.45f).SetLoops(-1, LoopType.Yoyo).SetLink(gameObject);
        }

        private void SetGlow(bool isOn)
        {
            _glowTween?.Kill();
            _glow.gameObject.SetActive(isOn);
            if (!isOn) return;

            _glow.color = new Color(1f, 0.85f, 0.4f, 0.9f);
            _glowTween = _glow.DOFade(0.3f, 0.6f).SetLoops(-1, LoopType.Yoyo).SetLink(gameObject);
        }
    }
}
