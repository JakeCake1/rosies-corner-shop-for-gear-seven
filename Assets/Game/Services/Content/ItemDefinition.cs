using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Game.Services.Content
{
    /// <summary>
    /// One grocery item of the 162-item catalog: identity, the attributes the level generator uses to keep similar
    /// items apart, and the Addressables reference of its sprite.
    /// </summary>
    [Serializable]
    public sealed class ItemDefinition
    {
        [Tooltip("Item art; lives in the item group of the level where it first appears (Items-Level-NN or Items-Rest).")]
        [SerializeField] private AssetReferenceSprite _sprite;
        [SerializeField] private int _id;
        [SerializeField] private string _name;
        [Tooltip("Department id: dairy, bakery, drink, pantry, sweet, fresh, home.")]
        [SerializeField] private string _family;
        [Tooltip("Product shape; a level never holds two items of the same shape.")]
        [SerializeField] private string _shape;
        [Tooltip("Outline class (box, can, bottle, stick, round); also picks the tap sound.")]
        [SerializeField] private string _silhouette;
        [Tooltip("Dominant color group; each level uses a color group at most once.")]
        [SerializeField] private string _colorBucket;

        /// <summary>
        /// Addressables reference of the item art (the 40:64 frame includes the shadow the item stands on). Load it
        /// through <see cref="IContentService.LoadItemSpritesAsync"/>, read it with <see cref="IContentService.GetItemSprite"/>.
        /// </summary>
        public AssetReferenceSprite Sprite => _sprite;

        /// <summary>Stable catalog id; saves (album) and the level generator refer to it.</summary>
        public int Id => _id;

        /// <summary>
        /// English name the level generator uses to find its lookalike pairs; not shown to the player. The
        /// display name is the <c>item.&lt;id&gt;</c> entry of the <c>Items</c> string table.
        /// </summary>
        public string Name => _name;

        /// <summary>Department id (dairy, bakery, drink, pantry, sweet, fresh, home).</summary>
        public string Family => _family;

        /// <summary>Product shape; the generator never puts two items of one shape in a level.</summary>
        public string Shape => _shape;

        /// <summary>Outline class (box, can, bottle, stick, round); also selects the tap sound timbre.</summary>
        public string Silhouette => _silhouette;

        /// <summary>Dominant color group used to keep lookalikes apart.</summary>
        public string ColorBucket => _colorBucket;

        /// <summary>
        /// Creates an item definition; used by the catalog importer and tests.
        /// </summary>
        /// <param name="id">Stable catalog id (0..161); saves and level layouts refer to it.</param>
        /// <param name="name">English name that identifies the item in the generator's lookalike pairs.</param>
        /// <param name="family">Department id.</param>
        /// <param name="shape">Product shape.</param>
        /// <param name="silhouette">Outline class.</param>
        /// <param name="colorBucket">Dominant color group.</param>
        /// <param name="sprite">Reference of the item art (40:64 aspect); may be null in tests.</param>
        public ItemDefinition(int id, string name, string family, string shape, string silhouette, string colorBucket,
            AssetReferenceSprite sprite)
        {
            _id = id;
            _name = name;
            _family = family;
            _shape = shape;
            _silhouette = silhouette;
            _colorBucket = colorBucket;
            _sprite = sprite;
        }
    }
}
