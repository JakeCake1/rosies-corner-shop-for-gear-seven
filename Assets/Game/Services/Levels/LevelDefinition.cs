using System;
using UnityEngine;

namespace Game.Services.Levels
{
    /// <summary>
    /// Parameters of one level, ported 1:1 from the prototype's <c>LEVELS</c> table. Lives inside a
    /// <see cref="LevelCurveConfig"/> asset; the JSON tables next to it are the import source.
    /// </summary>
    [Serializable]
    public sealed class LevelDefinition
    {
        [SerializeField] private int[] _sizes = { 1 };
        [SerializeField] private long _seed;
        [Tooltip("Items per shelf row (prototype jps).")]
        [SerializeField] private int _jarsPerRow = 3;
        [Tooltip("Visible column height in rows (prototype vis).")]
        [SerializeField] private int _visibleRows = 5;
        [SerializeField] private int _types = 3;
        [Tooltip("Copies of each type: 6 or 9, i.e. 2 or 3 baskets per type.")]
        [SerializeField] private int _perType = 6;
        [SerializeField] private int _locks;
        [SerializeField] private int _frozen;
        [SerializeField] private int _thaw = 3;
        [SerializeField] private int _boarded;
        [Tooltip("0: hammer on an open shelf, 1: in another column's stock, 2: inside a tied shelf.")]
        [SerializeField] private int _hammerMode;
        [SerializeField] private float _wrap;
        [SerializeField] private float _deep;
        [Tooltip("Lookalike strictness; 0 keeps similar items apart.")]
        [SerializeField] private int _look;
        [Tooltip("Counter capacity override; 0 means the default of 5.")]
        [SerializeField] private int _capacity;
        [Tooltip("Open baskets at start; 0 means the default of 2.")]
        [SerializeField] private int _baskets;
        [SerializeField] private bool _isHard;
        [Tooltip("Department of the day: up to three item types come from this family. Empty for none.")]
        [SerializeField] private string _family = string.Empty;
        [SerializeField] private string _tip = string.Empty;

        /// <summary>
        /// Creates a level definition; used by the JSON importer and by tests.
        /// </summary>
        /// <param name="seed">Seed of the deterministic layout.</param>
        /// <param name="jarsPerRow">Items per shelf row (2 or 3).</param>
        /// <param name="visibleRows">Visible column height in rows.</param>
        /// <param name="sizes">Possible heights of new shelves; repeats weight the choice.</param>
        /// <param name="types">Distinct item types.</param>
        /// <param name="perType">Copies of each type (6 or 9).</param>
        /// <param name="locks">Tied shelves.</param>
        /// <param name="frozen">Frozen shelves to sprinkle over the level.</param>
        /// <param name="thaw">Baskets that thaw a frozen shelf.</param>
        /// <param name="boarded">Nailed shelves (0 or 1).</param>
        /// <param name="hammerMode">0 open shelf, 1 stockroom, 2 inside a tied shelf.</param>
        /// <param name="wrap">Paper bag chance (0..1).</param>
        /// <param name="deep">Second-row chance (0..1).</param>
        /// <param name="family">Department of the day, or empty.</param>
        /// <param name="look">Lookalike strictness (0..2).</param>
        /// <param name="isHard">Tricky level with double rewards.</param>
        /// <param name="tip">Banner tip at level start.</param>
        /// <param name="capacity">Counter capacity override, 0 for default.</param>
        /// <param name="baskets">Open baskets at start, 0 for default.</param>
        public LevelDefinition(long seed, int jarsPerRow, int visibleRows, int[] sizes, int types, int perType,
            int locks = 0, int frozen = 0, int thaw = 3, int boarded = 0, int hammerMode = 0, float wrap = 0f,
            float deep = 0f, string family = "", int look = 0, bool isHard = false, string tip = "", int capacity = 0,
            int baskets = 0)
        {
            _seed = seed;
            _jarsPerRow = jarsPerRow;
            _visibleRows = visibleRows;
            _sizes = sizes;
            _types = types;
            _perType = perType;
            _locks = locks;
            _frozen = frozen;
            _thaw = thaw;
            _boarded = boarded;
            _hammerMode = hammerMode;
            _wrap = wrap;
            _deep = deep;
            _family = family ?? string.Empty;
            _look = look;
            _isHard = isHard;
            _tip = tip ?? string.Empty;
            _capacity = capacity;
            _baskets = baskets;
        }

        /// <summary>Possible heights of new shelves in rows; repeating a value makes it more likely.</summary>
        public int[] Sizes => _sizes;

        /// <summary>Seed of the deterministic layout; reduced to 32 bits by the generator.</summary>
        public long Seed => _seed;

        /// <summary>Items per shelf row (2 or 3).</summary>
        public int JarsPerRow => _jarsPerRow;

        /// <summary>Visible column height in rows; together with the screen height it sets the shelf size.</summary>
        public int VisibleRows => _visibleRows;

        /// <summary>Distinct item types in the level.</summary>
        public int Types => _types;

        /// <summary>Copies of each type: 6 or 9, i.e. 2 or 3 baskets per type.</summary>
        public int PerType => _perType;

        /// <summary>Tied shelves, each opened by a brass key on a shelf in another column.</summary>
        public int Locks => _locks;

        /// <summary>Frozen shelves sprinkled over newly arriving shelves (34% chance each).</summary>
        public int Frozen => _frozen;

        /// <summary>Shipped baskets needed to thaw a frozen shelf.</summary>
        public int Thaw => _thaw;

        /// <summary>Nailed shelves; the generator supports exactly one.</summary>
        public int Boarded => _boarded;

        /// <summary>Hammer placement: 0 open shelf, 1 stockroom, 2 inside a tied shelf.</summary>
        public int HammerMode => _hammerMode;

        /// <summary>Chance (0..1) that a new item is hidden in a paper bag.</summary>
        public float Wrap => _wrap;

        /// <summary>Chance (0..1) that a new item gets a second-row item behind it.</summary>
        public float Deep => _deep;

        /// <summary>Lookalike strictness; every shipped level uses 0 (similar items kept apart).</summary>
        public int Look => _look;

        /// <summary>Counter capacity; 5 unless overridden.</summary>
        public int Capacity => _capacity > 0 ? _capacity : 5;

        /// <summary>Open baskets at start; 2 unless overridden.</summary>
        public int Baskets => _baskets > 0 ? _baskets : 2;

        /// <summary>Tricky level: rewards ×2, red badge, red background and "Tricky" labels.</summary>
        public bool IsHard => _isHard;

        /// <summary>Department of the day (catalog family id), or empty.</summary>
        public string Family => _family;

        /// <summary>Banner tip shown when the level starts; empty when the level has none.</summary>
        public string Tip => _tip;

        /// <summary>Total items in the level; the progress bar's denominator.</summary>
        public int TotalItems => _types * _perType;
    }
}
