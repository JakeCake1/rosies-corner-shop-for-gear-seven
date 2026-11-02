using UnityEngine;

namespace Game.Features.Gameplay
{
    /// <summary>
    /// Sprites the gameplay presenter needs for dialogs and the booster bar.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Gameplay Art", fileName = "GameplayArt")]
    public sealed class GameplayArt : ScriptableObject
    {
        /// <summary>Addressables address; the start-boosters prompt loads it before the gameplay scene.</summary>
        public const string AddressKey = "config/gameplay-art";

        [SerializeField] private Sprite _undo;
        [SerializeField] private Sprite _pick;
        [SerializeField] private Sprite _tidy;
        [SerializeField] private Sprite _shuffle;
        [SerializeField] private Sprite _basket;
        [SerializeField] private Sprite _hammer;
        [SerializeField] private Sprite _paperBag;

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

        /// <summary>Hammer tool item.</summary>
        public Sprite Hammer => _hammer;

        /// <summary>Paper bag over wrapped items.</summary>
        public Sprite PaperBag => _paperBag;

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
    }
}
