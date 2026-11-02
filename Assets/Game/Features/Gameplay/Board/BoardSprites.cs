using Game.Features.Gameplay.Model;
using Game.Services.Content;
using UnityEngine;

namespace Game.Features.Gameplay.Board
{
    /// <summary>
    /// Picks the picture of an item on the board: the catalog sprite, the hammer or the paper bag of a wrapped item.
    /// </summary>
    public sealed class BoardSprites
    {
        private readonly ItemCatalog _catalog;
        private readonly GameplayArt _art;

        /// <summary>
        /// Creates the lookup.
        /// </summary>
        /// <param name="catalog">Item catalog; its sprites must be loaded.</param>
        /// <param name="art">Hammer and paper-bag pictures.</param>
        public BoardSprites(ItemCatalog catalog, GameplayArt art)
        {
            _catalog = catalog;
            _art = art;
        }

        /// <summary>
        /// Returns the picture of an item type, ignoring wrapping.
        /// </summary>
        /// <param name="type">Catalog id, or <see cref="Jar.HammerType"/>.</param>
        /// <returns>The sprite.</returns>
        public Sprite OfType(int type) => type == Jar.HammerType ? _art.Hammer : _catalog.Get(type).Sprite;

        /// <summary>
        /// Returns what the player sees of a jar: the hammer, a paper bag while wrapped, otherwise the item.
        /// </summary>
        /// <param name="jar">The jar on the board.</param>
        /// <returns>The sprite.</returns>
        public Sprite OfJar(Jar jar)
        {
            if (jar.IsTool)
            {
                return _art.Hammer;
            }

            return jar.IsWrapped ? _art.PaperBag : OfType(jar.Type);
        }
    }
}
