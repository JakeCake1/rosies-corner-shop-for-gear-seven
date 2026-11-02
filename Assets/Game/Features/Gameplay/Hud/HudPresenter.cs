using System;
using Cysharp.Threading.Tasks;
using UniRx;

namespace Game.Features.Gameplay.Hud
{
    /// <summary>
    /// Binds the HUD (level badge, progress, coins, settings button) to a fresh view model for every level and opens
    /// the settings screen from it.
    /// </summary>
    public sealed class HudPresenter : IDisposable
    {
        private readonly GameplayContext _context;
        private readonly Action _restart;
        private readonly CompositeDisposable _bindings = new CompositeDisposable();
        private HudViewModel _model;

        /// <summary>
        /// Creates the presenter.
        /// </summary>
        /// <param name="context">Shared gameplay state.</param>
        /// <param name="restart">Restarts the level in play; the settings screen's "Restart level" calls it.</param>
        public HudPresenter(GameplayContext context, Action restart)
        {
            _context = context;
            _restart = restart;
        }

        /// <summary>
        /// Rebinds the HUD to the level that just started.
        /// </summary>
        public void Bind()
        {
            Unbind();
            var services = _context.Services;
            _model = new HudViewModel(_context.Session, services.Wallet, services.Texts);
            _model.SettingsRequested.Subscribe(_ => OpenSettings()).AddTo(_bindings);
            _context.Screen.Hud.Bind(_model);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Unbind();
            _bindings.Dispose();
        }

        private void Unbind()
        {
            if (_model == null)
            {
                return;
            }

            _bindings.Clear();
            _context.Screen.Hud.Unbind();
            _model.Dispose();
            _model = null;
        }

        private void OpenSettings()
        {
            _context.Services.Settings.OpenAsync(_restart, _context.LevelToken).Forget();
        }
    }
}
