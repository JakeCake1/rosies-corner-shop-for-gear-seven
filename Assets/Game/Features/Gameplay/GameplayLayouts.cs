using UnityEngine.UIElements;

namespace Game.Features.Gameplay
{
    /// <summary>
    /// UXML layouts of the gameplay screen, set on the gameplay scene's lifetime scope.
    /// </summary>
    public sealed class GameplayLayouts
    {
        /// <summary>Board, HUD and booster bar.</summary>
        public VisualTreeAsset Screen { get; }

        /// <summary>One shelf block.</summary>
        public VisualTreeAsset Shelf { get; }

        /// <summary>One basket.</summary>
        public VisualTreeAsset Basket { get; }

        /// <summary>One booster button.</summary>
        public VisualTreeAsset Booster { get; }

        /// <summary>Level banner.</summary>
        public VisualTreeAsset Banner { get; }

        /// <summary>
        /// Creates the set.
        /// </summary>
        /// <param name="screen">Board, HUD and booster bar.</param>
        /// <param name="shelf">One shelf block.</param>
        /// <param name="basket">One basket.</param>
        /// <param name="booster">One booster button.</param>
        /// <param name="banner">Level banner.</param>
        public GameplayLayouts(VisualTreeAsset screen, VisualTreeAsset shelf, VisualTreeAsset basket, VisualTreeAsset booster,
            VisualTreeAsset banner)
        {
            Screen = screen;
            Shelf = shelf;
            Basket = basket;
            Booster = booster;
            Banner = banner;
        }
    }
}
