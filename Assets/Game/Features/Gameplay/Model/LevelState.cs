using System.Collections.Generic;
using System.Linq;

namespace Game.Features.Gameplay.Model
{
    /// <summary>
    /// Complete state of one level in play: columns, counter, baskets and progress (the prototype's <c>S</c>).
    /// </summary>
    /// <remarks>
    /// Mutated only by <see cref="LevelRules"/> and <see cref="LevelGenerator"/>. All randomness is derived from
    /// <see cref="Seed"/> and <see cref="RandomCounter"/>, so <see cref="Clone"/> replays identically after undo.
    /// </remarks>
    public sealed class LevelState
    {
        /// <summary>
        /// Number of cabinet columns.
        /// </summary>
        public const int ColumnCount = 3;

        /// <summary>
        /// Maximum number of basket slots (two open by default, the third is bought or offered).
        /// </summary>
        public const int MaxBaskets = 3;

        /// <summary>
        /// Columns left to right.
        /// </summary>
        public Column[] Columns { get; } = { new Column(), new Column(), new Column() };

        /// <summary>
        /// Remaining basket orders in shuffled order; each type appears <c>per / 3</c> times.
        /// </summary>
        public List<int> OrderSequence { get; } = new List<int>();

        /// <summary>
        /// Items waiting on the counter, oldest first.
        /// </summary>
        public List<Jar> Counter { get; } = new List<Jar>();

        /// <summary>
        /// Basket slots; null where no customer is waiting.
        /// </summary>
        public Basket[] Baskets { get; } = new Basket[MaxBaskets];

        /// <summary>
        /// Shipped basket types in order; feeds the album.
        /// </summary>
        public List<int> Shipped { get; } = new List<int>();

        /// <summary>
        /// Remaining mechanic budgets.
        /// </summary>
        public LevelMechanics Mechanics { get; set; } = new LevelMechanics();

        /// <summary>
        /// 1-based level number in the active curve.
        /// </summary>
        public int LevelNumber { get; set; }

        /// <summary>
        /// Items per shelf row (2 or 3).
        /// </summary>
        public int JarsPerRow { get; set; }

        /// <summary>
        /// Visible column height in rows.
        /// </summary>
        public int VisibleRows { get; set; }

        /// <summary>
        /// Allowed heights (rows) of new shelves; duplicates weight the random choice.
        /// </summary>
        public int[] Sizes { get; set; }

        /// <summary>
        /// Level seed; with <see cref="RandomCounter"/> it drives all in-level randomness.
        /// </summary>
        public uint Seed { get; set; }

        /// <summary>
        /// Counter capacity: 5, or 6 after the "+1 counter spot" offer.
        /// </summary>
        public int Capacity { get; set; }

        /// <summary>
        /// Open basket slots (2 or 3).
        /// </summary>
        public int UnlockedBaskets { get; set; }

        /// <summary>
        /// Items in the level; the level is won when <see cref="Collected"/> reaches it.
        /// </summary>
        public int Total { get; set; }

        /// <summary>
        /// Items in shipped baskets.
        /// </summary>
        public int Collected { get; set; }

        /// <summary>
        /// Next free id for jars, shelves and baskets.
        /// </summary>
        public int NextId { get; set; }

        /// <summary>
        /// Most items ever on the counter during the level; decides the stars.
        /// </summary>
        public int Peak { get; set; }

        /// <summary>
        /// Moves made, including hammer taps and boosters.
        /// </summary>
        public int Moves { get; set; }

        /// <summary>
        /// Draws made from the in-level random stream (the prototype's <c>rc</c>).
        /// </summary>
        public int RandomCounter { get; set; }

        /// <summary>
        /// Where the hammer was placed at generation.
        /// </summary>
        public HammerPlace HammerPlace { get; set; }

        /// <summary>
        /// Column of the hammer (or of the shelf holding it) at generation; -1 without a hammer.
        /// </summary>
        public int HammerColumn { get; set; } = -1;

        /// <summary>
        /// Returns the next in-level random value; deterministic for a given seed and draw count.
        /// </summary>
        /// <returns>A value in [0, 1).</returns>
        public double NextRandom()
        {
            unchecked
            {
                RandomCounter++;
                var seed = Seed ^ ((uint)RandomCounter * 2654435761u);
                return Mulberry32.First(seed);
            }
        }

        /// <summary>
        /// Deep-copies the state for undo; jar identity inside the copy is preserved (a jar shared by two places
        /// stays shared).
        /// </summary>
        /// <returns>An independent copy.</returns>
        public LevelState Clone()
        {
            var map = new Dictionary<Jar, Jar>();
            Jar Copy(Jar j)
            {
                if (j == null) return null;
                if (map.TryGetValue(j, out var c)) return c;
                c = new Jar
                {
                    Id = j.Id, Type = j.Type, IsTool = j.IsTool, TargetBlockId = j.TargetBlockId,
                    IsWrapped = j.IsWrapped,
                };
                map[j] = c;
                c.Back = Copy(j.Back);
                return c;
            }

            var s = new LevelState
            {
                Mechanics = Mechanics.Clone(), LevelNumber = LevelNumber, JarsPerRow = JarsPerRow,
                VisibleRows = VisibleRows, Sizes = Sizes, Seed = Seed, Capacity = Capacity,
                UnlockedBaskets = UnlockedBaskets, Total = Total, Collected = Collected, NextId = NextId, Peak = Peak,
                Moves = Moves, RandomCounter = RandomCounter, HammerPlace = HammerPlace, HammerColumn = HammerColumn,
            };
            for (var c = 0; c < ColumnCount; c++)
            {
                var src = Columns[c];
                var dst = s.Columns[c];
                dst.SpawnCount = src.SpawnCount;
                dst.Queue.AddRange(src.Queue.Select(Copy));
                foreach (var b in src.Blocks)
                {
                    dst.Blocks.Add(new Block
                    {
                        Id = b.Id, Height = b.Height, Slots = b.Slots.Select(Copy).ToArray(), IsLocked = b.IsLocked,
                        KeyTargetId = b.KeyTargetId, Frozen = b.Frozen, IsBoarded = b.IsBoarded,
                    });
                }
            }

            s.OrderSequence.AddRange(OrderSequence);
            s.Counter.AddRange(Counter.Select(Copy));
            s.Shipped.AddRange(Shipped);
            for (var i = 0; i < MaxBaskets; i++)
            {
                var b = Baskets[i];
                if (b == null) continue;

                var copy = new Basket { Id = b.Id, Type = b.Type, IsDone = b.IsDone };
                copy.Jars.AddRange(b.Jars.Select(Copy));
                s.Baskets[i] = copy;
            }

            return s;
        }
    }
}
