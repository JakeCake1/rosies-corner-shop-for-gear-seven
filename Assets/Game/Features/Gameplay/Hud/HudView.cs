using DG.Tweening;
using Game.Services.UI;
using Game.Services.UI.Elements;
using UniRx;
using UnityEngine.UIElements;

namespace Game.Features.Gameplay.Hud
{
    /// <summary>
    /// Gameplay HUD: level badge, progress bar, coins and the settings gear.
    /// </summary>
    public sealed class HudView : View<HudViewModel>
    {
        private const string BadgeName = "badge";
        private const string BadgeTitleName = "badge-title";
        private const string LevelNumberName = "level-number";
        private const string ProgressName = "progress";
        private const string ProgressFillName = "progress-fill";
        private const string ProgressTextName = "progress-text";
        private const string CoinsName = "coins-value";
        private const string SettingsName = "settings";
        private const string GoldBadgeClass = "kit-badge-gold";
        private const string RedBadgeClass = "kit-badge-red";
        private const float FillSeconds = 0.35f;

        private VisualElement _badge;
        private Label _badgeTitle;
        private Label _levelNumber;
        private VisualElement _fill;
        private Label _progressText;
        private Label _coins;
        private Button _settings;

        /// <summary>The progress bar (shipped baskets fly into it).</summary>
        public VisualElement ProgressBar { get; private set; }

        /// <inheritdoc/>
        protected override void OnAttached()
        {
            _badge = Q(BadgeName);
            _badgeTitle = Q<Label>(BadgeTitleName);
            _levelNumber = Q<Label>(LevelNumberName);
            ProgressBar = Q(ProgressName);
            _fill = Q(ProgressFillName);
            _progressText = Q<Label>(ProgressTextName);
            _coins = Q<Label>(CoinsName);
            _settings = Q<Button>(SettingsName);
        }

        /// <inheritdoc/>
        protected override void OnBind(HudViewModel vm)
        {
            vm.BadgeTitle.Subscribe(t => _badgeTitle.text = t).AddTo(Bindings);
            vm.LevelNumber.Subscribe(t => _levelNumber.text = t).AddTo(Bindings);
            vm.IsTricky.Subscribe(x =>
            {
                _badge.EnableInClassList(RedBadgeClass, x);
                _badge.EnableInClassList(GoldBadgeClass, !x);
            }).AddTo(Bindings);
            vm.ProgressText.Subscribe(t => _progressText.text = t).AddTo(Bindings);
            vm.Progress.Subscribe(SetFill).AddTo(Bindings);
            vm.Coins.Subscribe(c => _coins.text = c.ToString()).AddTo(Bindings);
            _settings.OnTapAsObservable().Subscribe(_ => vm.OpenSettings()).AddTo(Bindings);
        }

        private void SetFill(float value)
        {
            _fill.Kill();
            _fill.SetVisible(value > 0f);
            _fill.DOWidthPercent(UnityEngine.Mathf.Clamp01(value), FillSeconds).SetEase(Ease.OutCubic).SetId(this);
        }
    }
}
