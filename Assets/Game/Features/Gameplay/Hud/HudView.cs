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
    /// The HUD row: level badge, progress bar, coin chip and settings gear.
    /// </summary>
    public sealed class HudView : View<HudViewModel>
    {
        private const float FillSeconds = 0.35f;

        [SerializeField] private Image _badge;
        [SerializeField] private Sprite _badgeGold;
        [SerializeField] private Sprite _badgeRed;
        [SerializeField] private TMP_Text _badgeTitle;
        [SerializeField] private TMP_Text _levelNumber;
        [SerializeField] private RectTransform _progressFill;
        [SerializeField] private TMP_Text _progressText;
        [SerializeField] private TMP_Text _coins;
        [SerializeField] private Button _gear;

        /// <inheritdoc/>
        protected override void OnBind(HudViewModel vm)
        {
            vm.BadgeTitle.Subscribe(t => _badgeTitle.SetText(t)).AddTo(Bindings);
            vm.LevelNumber.Subscribe(t => _levelNumber.SetText(t)).AddTo(Bindings);
            vm.IsTricky.Subscribe(x => _badge.sprite = x ? _badgeRed : _badgeGold).AddTo(Bindings);
            vm.ProgressText.Subscribe(t => _progressText.SetText(t)).AddTo(Bindings);
            vm.Progress.Subscribe(SetFill).AddTo(Bindings);
            vm.Coins.Subscribe(c => _coins.SetText("{0}", c)).AddTo(Bindings);
            _gear.OnClickAsObservable().ThrottleFirst(TimeSpan.FromMilliseconds(300))
                .Subscribe(_ => vm.OpenSettings()).AddTo(Bindings);
        }

        private void SetFill(float value)
        {
            _progressFill.DOKill();
            _progressFill.DOAnchorMax(new Vector2(Mathf.Clamp01(value), 1f), FillSeconds)
                .SetEase(Ease.OutCubic).SetLink(gameObject).SetId(this);
        }
    }
}
