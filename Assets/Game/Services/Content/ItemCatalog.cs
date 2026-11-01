using System.Collections.Generic;
using UnityEngine;

namespace Game.Services.Content
{
    /// <summary>
    /// The full grocery catalog and the department order; loaded once at bootstrap through Addressables and
    /// shared by the level generator, the shelves, the home showcase and the album.
    /// </summary>
    /// <remarks>
    /// Item sprites are referenced directly (not via <c>AssetReference</c>): the catalog is one addressable asset
    /// whose sprites always load together, so per-item handles would add bookkeeping without saving memory.
    /// </remarks>
    [CreateAssetMenu(menuName = "Game/Item Catalog", fileName = "ItemCatalog")]
    public sealed class ItemCatalog : ScriptableObject, IValidatableConfig
    {
        /// <summary>
        /// Addressables key of the catalog asset.
        /// </summary>
        public const string AddressKey = "config/item-catalog";

        [SerializeField] private List<ItemDefinition> _items = new List<ItemDefinition>();
        [Tooltip("Departments in album order; the generator also relies on this order.")]
        [SerializeField] private List<string> _families = new List<string>();
        [SerializeField] private List<string> _familyNames = new List<string>();
        [Tooltip("Items shown on the home showcase before the player has found seven items.")]
        [SerializeField] private List<int> _defaultShowcase = new List<int> { 0, 4, 10, 16, 22, 26, 32 };

        /// <summary>
        /// All items ordered by id; index equals <see cref="ItemDefinition.Id"/>.
        /// </summary>
        public IReadOnlyList<ItemDefinition> Items => _items;

        /// <summary>
        /// Department ids in album order (dairy, bakery, drink, pantry, sweet, fresh, home).
        /// </summary>
        public IReadOnlyList<string> Families => _families;

        /// <summary>
        /// Items shown on the home showcase until the player has found at least seven.
        /// </summary>
        public IReadOnlyList<int> DefaultShowcase => _defaultShowcase;

        /// <summary>
        /// Returns the item with the given id.
        /// </summary>
        /// <param name="id">Catalog id; must be valid.</param>
        /// <returns>The item definition.</returns>
        public ItemDefinition Get(int id) => _items[id];

        /// <summary>
        /// Returns the display name of a department ("Fresh produce" for "fresh").
        /// </summary>
        /// <param name="family">Department id.</param>
        /// <returns>The display name, or the id itself when unknown.</returns>
        public string FamilyName(string family)
        {
            var i = _families.IndexOf(family);
            return i >= 0 && i < _familyNames.Count ? _familyNames[i] : family;
        }

        /// <summary>
        /// Replaces the catalog content; called by the editor importer only.
        /// </summary>
        /// <param name="items">Items ordered by id.</param>
        /// <param name="families">Department ids in album order.</param>
        /// <param name="familyNames">Display names matching <paramref name="families"/>.</param>
        public void SetContent(List<ItemDefinition> items, List<string> families, List<string> familyNames)
        {
            _items = items;
            _families = families;
            _familyNames = familyNames;
        }

        /// <inheritdoc/>
        public void Validate(List<string> errors)
        {
            if (_items.Count == 0) errors.Add($"{name}: no items.");
            for (var i = 0; i < _items.Count; i++)
            {
                var it = _items[i];
                if (it.Id != i) errors.Add($"{name}: item at index {i} has id {it.Id}.");
                if (it.Sprite == null) errors.Add($"{name}: item {it.Id} '{it.Name}' has no sprite.");
                if (!_families.Contains(it.Family)) errors.Add($"{name}: item {it.Id} has unknown family '{it.Family}'.");
                if (string.IsNullOrEmpty(it.Shape) || string.IsNullOrEmpty(it.ColorBucket))
                {
                    errors.Add($"{name}: item {it.Id} is missing shape or color group.");
                }
            }

            if (_familyNames.Count != _families.Count) errors.Add($"{name}: family names don't match families.");
            foreach (var id in _defaultShowcase)
            {
                if (id < 0 || id >= _items.Count) errors.Add($"{name}: showcase id {id} is out of range.");
            }
        }
    }
}
