using Game.Features.Gameplay.Board;
using Game.Features.Gameplay.Hud;
using UnityEngine;

namespace Game.Features.Gameplay
{
    /// <summary>
    /// Scene component that groups the gameplay scene's views so the scope can register them with one reference.
    /// </summary>
    public sealed class GameplayViews : MonoBehaviour
    {
        [SerializeField] private HudView _hud;
        [SerializeField] private BoosterButtonView[] _boosters = new BoosterButtonView[4];
        [SerializeField] private LevelBannerView _banner;

        /// <summary>The HUD row.</summary>
        public HudView Hud => _hud;

        /// <summary>Booster bar buttons, left to right (unused ones are hidden).</summary>
        public BoosterButtonView[] Boosters => _boosters;

        /// <summary>The level start banner.</summary>
        public LevelBannerView Banner => _banner;
    }
}
