using Game.Features.Gameplay.Model;

namespace Game.Features.Gameplay
{
    /// <summary>
    /// Shows the level banner when a level starts: the title ("Level 7" or "Tricky level") and a tip that tells where
    /// this level's hammer is hidden, or the level's own tip.
    /// </summary>
    public sealed class LevelBannerPresenter
    {
        private static readonly string[] HammerStockTips =
        {
            GameplayTexts.HammerStockLeft, GameplayTexts.HammerStockMiddle, GameplayTexts.HammerStockRight,
        };
        private static readonly string[] HammerTiedTips =
        {
            GameplayTexts.HammerTiedLeft, GameplayTexts.HammerTiedMiddle, GameplayTexts.HammerTiedRight,
        };

        private readonly GameplayContext _context;

        /// <summary>
        /// Creates the presenter.
        /// </summary>
        /// <param name="context">Shared gameplay state.</param>
        public LevelBannerPresenter(GameplayContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Shows the banner of the level that just started.
        /// </summary>
        public void Show()
        {
            var texts = _context.Services.Texts;
            var isTricky = _context.Session.IsTricky;
            var title = isTricky
                ? texts.Get(GameplayTexts.BannerTricky)
                : texts.Format(GameplayTexts.BannerLevel, _context.Level);
            var tipKey = TipKey();
            var tip = string.IsNullOrEmpty(tipKey) ? string.Empty : texts.Get(tipKey);
            _context.Screen.Banner.Show(title, tip, isTricky, _context.Board.Counter);
        }

        private string TipKey()
        {
            var state = _context.Session.State;
            switch (state.HammerPlace)
            {
                case HammerPlace.Stock:
                    return HammerStockTips[state.HammerColumn];
                case HammerPlace.Tied:
                    return HammerTiedTips[state.HammerColumn];
                case HammerPlace.Shelf:
                    return GameplayTexts.HammerShelf;
                default:
                    return _context.Session.Definition.TipKey;
            }
        }
    }
}
