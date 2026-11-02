using System;
using Game.Features.Gameplay.Session;
using Game.Services.Localization;
using Game.Services.Wallet;
using Game.Services.UI;
using UniRx;

namespace Game.Features.Gameplay.Hud
{
    /// <summary>
    /// State of the gameplay HUD: level badge, packed-items progress, coins, settings button.
    /// </summary>
    public sealed class HudViewModel : ViewModel
    {
        private readonly ReactiveProperty<string> _badgeTitle = new ReactiveProperty<string>();
        private readonly ReactiveProperty<string> _levelNumber = new ReactiveProperty<string>("1");
        private readonly ReactiveProperty<bool> _isTricky = new ReactiveProperty<bool>();
        private readonly ReactiveProperty<string> _progressText = new ReactiveProperty<string>("0 / 0");
        private readonly ReactiveProperty<float> _progress = new ReactiveProperty<float>();
        private readonly Subject<Unit> _settingsRequested = new Subject<Unit>();
        private readonly LevelSession _session;
        private readonly ILocalizationService _texts;

        /// <summary>"LEVEL" or "TRICKY" above the number.</summary>
        public IReadOnlyReactiveProperty<string> BadgeTitle => _badgeTitle;

        /// <summary>Level number shown in the badge.</summary>
        public IReadOnlyReactiveProperty<string> LevelNumber => _levelNumber;

        /// <summary>Red badge on tricky levels.</summary>
        public IReadOnlyReactiveProperty<bool> IsTricky => _isTricky;

        /// <summary>"collected / total".</summary>
        public IReadOnlyReactiveProperty<string> ProgressText => _progressText;

        /// <summary>Progress fill 0..1.</summary>
        public IReadOnlyReactiveProperty<float> Progress => _progress;

        /// <summary>Current coins.</summary>
        public IReadOnlyReactiveProperty<int> Coins { get; }

        /// <summary>Emits when the gear is tapped; the presenter opens settings.</summary>
        public IObservable<Unit> SettingsRequested => _settingsRequested;

        /// <summary>
        /// Creates the view model and follows the session and the wallet.
        /// </summary>
        /// <param name="session">Level in play.</param>
        /// <param name="wallet">Coins.</param>
        /// <param name="texts">Badge title ("LEVEL" or "TRICKY").</param>
        public HudViewModel(LevelSession session, IWalletService wallet, ILocalizationService texts)
        {
            _session = session;
            _texts = texts;
            Coins = wallet.Coins;
            Disposables.Add(_badgeTitle);
            Disposables.Add(_levelNumber);
            Disposables.Add(_isTricky);
            Disposables.Add(_progressText);
            Disposables.Add(_progress);
            Disposables.Add(_settingsRequested);
            session.Changed.Subscribe(_ => Refresh()).AddTo(Disposables);
            Refresh();
        }

        /// <summary>
        /// Called by the view when the gear is tapped.
        /// </summary>
        public void OpenSettings() => _settingsRequested.OnNext(Unit.Default);

        private void Refresh()
        {
            var st = _session.State;
            if (st == null)
            {
                return;
            }

            _isTricky.Value = _session.IsTricky;
            _badgeTitle.Value = _texts.Get(_session.IsTricky ? GameplayTexts.BadgeTricky : GameplayTexts.BadgeLevel);
            _levelNumber.Value = st.LevelNumber.ToString();
            _progressText.Value = $"{st.Collected} / {st.Total}";
            _progress.Value = st.Total > 0 ? st.Collected / (float)st.Total : 0f;
        }
    }
}
