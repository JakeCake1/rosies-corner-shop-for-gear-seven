using System.Collections.Generic;
using Game.Features.Gameplay.Session;

namespace Game.Features.Gameplay.Hud
{
    /// <summary>
    /// Localization keys of each booster: its name (button label, offer title) and Rosie's gift tip.
    /// </summary>
    public static class BoosterTexts
    {
        private static readonly Dictionary<string, string> Names = new Dictionary<string, string>
        {
            [BoosterIds.Undo] = GameplayTexts.BoosterUndo,
            [BoosterIds.Pick] = GameplayTexts.BoosterPick,
            [BoosterIds.Tidy] = GameplayTexts.BoosterTidy,
            [BoosterIds.Shuffle] = GameplayTexts.BoosterShuffle,
        };
        private static readonly Dictionary<string, string> Gifts = new Dictionary<string, string>
        {
            [BoosterIds.Undo] = GameplayTexts.GiftUndo,
            [BoosterIds.Tidy] = GameplayTexts.GiftTidy,
            [BoosterIds.Pick] = GameplayTexts.GiftPick,
        };

        /// <summary>
        /// Returns the key of the booster's name.
        /// </summary>
        /// <param name="boosterId">One of <see cref="BoosterIds"/>.</param>
        /// <returns>A key of the <c>UI</c> string table.</returns>
        public static string NameOf(string boosterId) => Names[boosterId];

        /// <summary>
        /// Returns the key of the coach tip shown when Rosie gifts the booster; Shuffle is never gifted.
        /// </summary>
        /// <param name="boosterId">Undo, Tidy or Pick.</param>
        /// <returns>A key of the <c>UI</c> string table.</returns>
        public static string GiftOf(string boosterId) => Gifts[boosterId];

        /// <summary>
        /// Returns the key of the text that explains the booster in its purchase offer.
        /// </summary>
        /// <param name="boosterId">Pick, Tidy or Shuffle; Undo has its own offer text.</param>
        /// <returns>A key of the <c>UI</c> string table.</returns>
        public static string InfoOf(string boosterId)
        {
            if (boosterId == BoosterIds.Pick)
            {
                return GameplayTexts.PickInfo;
            }

            return boosterId == BoosterIds.Tidy ? GameplayTexts.TidyInfo : GameplayTexts.ShuffleInfo;
        }
    }
}
