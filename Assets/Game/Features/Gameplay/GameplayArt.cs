using UnityEngine;

namespace Game.Features.Gameplay
{
    /// <summary>
    /// Sprites the gameplay presenter needs for dialogs and the booster bar.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Gameplay Art", fileName = "GameplayArt")]
    public sealed class GameplayArt : ScriptableObject
    {
        [SerializeField] private Sprite _undo;
        [SerializeField] private Sprite _pick;
        [SerializeField] private Sprite _tidy;
        [SerializeField] private Sprite _shuffle;
        [SerializeField] private Sprite _basket;

        /// <summary>Undo icon.</summary>
        public Sprite Undo => _undo;

        /// <summary>Quick Pick icon.</summary>
        public Sprite Pick => _pick;

        /// <summary>Tidy Up icon.</summary>
        public Sprite Tidy => _tidy;

        /// <summary>Shuffle icon.</summary>
        public Sprite Shuffle => _shuffle;

        /// <summary>Full basket picture for the extra-basket offer and start boosters.</summary>
        public Sprite Basket => _basket;

        /// <summary>
        /// Returns the icon of a booster id.
        /// </summary>
        /// <param name="id">Booster id.</param>
        /// <returns>The icon, or null.</returns>
        public Sprite IconOf(string id)
        {
            switch (id)
            {
                case "undo": return _undo;
                case "pick": return _pick;
                case "tidy": return _tidy;
                case "shuffle": return _shuffle;
                default: return null;
            }
        }

        /// <summary>
        /// Assigns the sprites; called by the editor builder only.
        /// </summary>
        /// <param name="undo">Undo icon.</param>
        /// <param name="pick">Quick Pick icon.</param>
        /// <param name="tidy">Tidy Up icon.</param>
        /// <param name="shuffle">Shuffle icon.</param>
        /// <param name="basket">Basket picture.</param>
        public void SetSprites(Sprite undo, Sprite pick, Sprite tidy, Sprite shuffle, Sprite basket)
        {
            _undo = undo;
            _pick = pick;
            _tidy = tidy;
            _shuffle = shuffle;
            _basket = basket;
        }
    }
}
