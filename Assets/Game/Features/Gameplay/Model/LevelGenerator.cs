using System;
using System.Collections.Generic;
using System.Linq;
using Game.Services.Levels;
using Game.Services.Content;

namespace Game.Features.Gameplay.Model
{
    /// <summary>
    /// Builds the opening state of a level from its definition: picks item types, fills the stockrooms, cuts the
    /// first shelves and places keys, the nailed shelf and the hammer. A port of the prototype's <c>makeLevel</c>.
    /// </summary>
    /// <remarks>
    /// The order of random draws is part of the contract: changing it changes every level layout. Keep it aligned
    /// with the prototype source (<c>_inputs/elements/source/index.html</c>).
    /// </remarks>
    public sealed class LevelGenerator
    {
        private const double FrozenChance = 0.34;

        private static readonly string[][] Lookalikes =
        {
            new[] { "Red Apple", "Tomato" }, new[] { "Cherry Soda", "Tomato Soup" }, new[] { "Lime Soda", "Pea Soup" },
            new[] { "Orange Soda", "Orange Drink" }, new[] { "Lemonade", "Mustard" }, new[] { "Milk", "Glass Milk" },
            new[] { "Strawberry Yogurt", "Strawberry Milk" }, new[] { "Cheese", "Butter" },
            new[] { "Ketchup", "Red Vinegar" }, new[] { "Orange", "Peach" }, new[] { "Lemon", "Yellow Pepper" },
            new[] { "Grapes", "Blueberries" }, new[] { "Cherries", "Strawberry" },
            new[] { "Pink Cupcake", "Strawberry Ice Cream" }, new[] { "Cherry Popsicle", "Ketchup" },
            new[] { "Grape Juice", "Grape Soda" }, new[] { "Green Apple", "Pear" },
            new[] { "Chocolate Muffin", "Chocolate Cupcake" }, new[] { "Pink Donut", "Pink Iced Cookie" },
            new[] { "Corn", "Pineapple" },
        };

        private readonly IReadOnlyList<ItemDefinition> _items;
        private readonly IReadOnlyList<string> _familyOrder;

        /// <summary>
        /// Creates a generator over the item catalog.
        /// </summary>
        /// <param name="items">All catalog items ordered by id.</param>
        /// <param name="familyOrder">Department ids in catalog order; the shuffle of departments starts from it.</param>
        public LevelGenerator(IReadOnlyList<ItemDefinition> items, IReadOnlyList<string> familyOrder)
        {
            _items = items;
            _familyOrder = familyOrder;
        }

        /// <summary>
        /// Generates the opening state of a level.
        /// </summary>
        /// <param name="levelNumber">1-based number in the active curve (used for rules that depend on it).</param>
        /// <param name="def">Level parameters.</param>
        /// <param name="foundItems">Items the player already has in the album; unseen ones are preferred.</param>
        /// <returns>A ready-to-play state with baskets ordered.</returns>
        public LevelState Generate(int levelNumber, LevelDefinition def, ISet<int> foundItems)
        {
            var seed = (uint)def.Seed;
            var r = new Mulberry32(seed);
            var nid = 1;
            var types = PickTypes(def.Types, new Mulberry32(unchecked(seed * 31u + 7u)), def.Look, def.Family,
                foundItems);

            var jars = new List<Jar>();
            foreach (var t in types)
            {
                for (var i = 0; i < def.PerType; i++)
                {
                    jars.Add(new Jar { Id = nid++, Type = t });
                }
            }

            r.Shuffle(jars);
            var st = new LevelState
            {
                LevelNumber = levelNumber, JarsPerRow = def.JarsPerRow, VisibleRows = def.VisibleRows,
                Sizes = def.Sizes, Seed = seed, Capacity = def.Capacity, UnlockedBaskets = def.Baskets,
                Total = jars.Count, NextId = nid,
                Mechanics = new LevelMechanics
                {
                    Wrap = def.Wrap, Deep = def.Deep, Frozen = def.Frozen, Boarded = def.Boarded,
                    Thaw = def.Thaw > 0 ? def.Thaw : 3, HammerMode = def.HammerMode,
                },
            };
            for (var i = 0; i < jars.Count; i++)
            {
                st.Columns[i % LevelState.ColumnCount].Queue.Add(jars[i]);
            }

            foreach (var t in types)
            {
                for (var k = 0; k < def.PerType / Basket.Capacity; k++)
                {
                    st.OrderSequence.Add(t);
                }
            }

            r.Shuffle(st.OrderSequence);
            FillColumns(st, null);
            PlaceLocks(st, def.Locks, r);
            PlaceBoard(st);
            LevelRules.RefillBaskets(st, null);
            return st;
        }

        /// <summary>
        /// Cuts new shelves from each column's stockroom while there is room at the top (prototype <c>fillCols</c>).
        /// </summary>
        /// <param name="st">Level state to mutate.</param>
        /// <param name="events">Receives a <see cref="LevelEventType.ShelfArrived"/> per new shelf; may be null.</param>
        public static void FillColumns(LevelState st, List<LevelEvent> events)
        {
            for (var c = 0; c < LevelState.ColumnCount; c++)
            {
                var col = st.Columns[c];
                var used = ColumnUsed(col);
                while (col.Queue.Count > 0 && used < st.VisibleRows)
                {
                    var free = st.VisibleRows - used;
                    var height = SpawnHeight(st, c, free);
                    var slots = new List<Jar>();
                    for (var k = 0; k < height * st.JarsPerRow; k++)
                    {
                        if (col.Queue.Count == 0)
                        {
                            slots.Add(null);
                            continue;
                        }

                        slots.Add(TakeFromStock(st, col));
                    }

                    var last = slots.FindLastIndex(x => x != null);
                    height = Math.Max(1, (int)Math.Ceiling((last + 1) / (double)st.JarsPerRow));
                    var cells = new Jar[height * st.JarsPerRow];
                    for (var k = 0; k < cells.Length && k < slots.Count; k++)
                    {
                        cells[k] = slots[k];
                    }

                    var block = new Block { Id = st.NextId++, Height = height, Slots = cells };
                    var isFirst = col.Blocks.Count == 0;
                    col.Blocks.Add(block);
                    col.SpawnCount++;
                    used += height;
                    if (!isFirst)
                    {
                        ApplyMechanics(st, block);
                    }
                    events?.Add(LevelEvent.Of(LevelEventType.ShelfArrived, block.Id, c));
                }
            }
        }

        private static Jar TakeFromStock(LevelState st, Column col)
        {
            var jar = col.Queue[0];
            col.Queue.RemoveAt(0);
            if (jar.IsTool)
            {
                return jar;
            }

            var m = st.Mechanics;
            if (m.Wrap > 0 && st.NextRandom() < m.Wrap)
            {
                jar = jar.CloneShallow();
                jar.IsWrapped = true;
            }

            if (m.Deep > 0 && col.Queue.Count > 0 && !col.Queue[0].IsTool && st.NextRandom() < m.Deep)
            {
                jar = jar.CloneShallow();
                jar.Back = col.Queue[0];
                col.Queue.RemoveAt(0);
            }

            return jar;
        }

        private static int SpawnHeight(LevelState st, int c, int free)
        {
            var col = st.Columns[c];
            var seed = (uint)((st.Seed * 31L + c * 7919L + col.SpawnCount * 104729L) & 0xFFFFFFFFL);
            var rr = new Mulberry32(seed);
            rr.Next();
            var options = st.Sizes.Where(s => s <= free).ToArray();
            return options.Length > 0 ? options[rr.NextIndex(options.Length)] : free;
        }

        private static int ColumnUsed(Column col) => col.Blocks.Sum(b => b.Height);

        private static void ApplyMechanics(LevelState st, Block block)
        {
            var m = st.Mechanics;
            if (m.Frozen > 0 && st.NextRandom() < FrozenChance)
            {
                block.Frozen = m.Thaw;
                m.Frozen--;
            }
        }

        private static void PlaceLocks(LevelState st, int locks, Mulberry32 r)
        {
            for (var l = 0; l < locks; l++)
            {
                var candidates = new List<(Block Block, int Column)>();
                for (var c = 0; c < LevelState.ColumnCount; c++)
                {
                    foreach (var b in st.Columns[c].Blocks)
                    {
                        if (b.IsOpen && b.KeyTargetId == 0 && !b.Slots.Any(j => j != null && j.IsTool))
                        {
                            candidates.Add((b, c));
                        }
                    }
                }

                if (candidates.Count < 2)
                {
                    break;
                }

                r.Shuffle(candidates);
                var target = candidates.FirstOrDefault(x => x.Block.Height >= 2);
                if (target.Block == null)
                {
                    target = candidates[0];
                }
                var keyShelf = candidates.FirstOrDefault(x => x.Block != target.Block && x.Column != target.Column);
                if (keyShelf.Block == null)
                {
                    keyShelf = candidates.FirstOrDefault(x => x.Block != target.Block);
                }
                if (keyShelf.Block == null)
                {
                    break;
                }

                target.Block.IsLocked = true;
                keyShelf.Block.KeyTargetId = target.Block.Id;
            }
        }

        /// <summary>
        /// Places the single nailed shelf and its hammer in another column (prototype <c>placeBoard</c>).
        /// </summary>
        private static void PlaceBoard(LevelState st)
        {
            if (st.Mechanics.Boarded <= 0)
            {
                return;
            }

            var candidates = new List<(Block Block, int Column)>();
            for (var c = 0; c < LevelState.ColumnCount; c++)
            {
                var col = st.Columns[c];
                foreach (var b in col.Blocks)
                {
                    if (b.IsOpen && b.KeyTargetId == 0 && !b.Slots.Any(j => j != null && j.IsTool) && col.Blocks.Count >= 2)
                    {
                        candidates.Add((b, c));
                    }
                }
            }

            if (candidates.Count == 0)
            {
                return;
            }

            var target = candidates[(int)(st.NextRandom() * candidates.Count)];
            target.Block.IsBoarded = true;
            var hammer = new Jar { Id = st.NextId++, Type = Jar.HammerType, IsTool = true, TargetBlockId = target.Block.Id };
            var others = Enumerable.Range(0, LevelState.ColumnCount).Where(i => i != target.Column).ToList();
            var mode = st.Mechanics.HammerMode;

            (Column Col, Block Block, int Slot, int ColIndex)? spot = null;
            if (mode == 2)
            {
                spot = ShelfSpot(st, others, b => b.IsLocked);
            }
            if (mode == 0)
            {
                spot = ShelfSpot(st, others, b => b.IsOpen && b.KeyTargetId == 0);
            }
            if (spot.HasValue)
            {
                var s = spot.Value;
                var old = s.Block.Slots[s.Slot];
                s.Block.Slots[s.Slot] = hammer;
                s.Col.Queue.Insert(0, new Jar { Id = old.Id, Type = old.Type });
                st.HammerPlace = mode == 2 ? HammerPlace.Tied : HammerPlace.Shelf;
                st.HammerColumn = s.ColIndex;
                return;
            }

            var withStock = others.Where(i => st.Columns[i].Queue.Count > 0).ToList();
            var ci = withStock.Count > 0 ? withStock[(int)(st.NextRandom() * withStock.Count)] : others[0];
            var queue = st.Columns[ci].Queue;
            var at = (int)(st.NextRandom() * Math.Min(st.JarsPerRow, queue.Count + 1));
            queue.Insert(at, hammer);
            st.HammerPlace = HammerPlace.Stock;
            st.HammerColumn = ci;
        }

        private static (Column, Block, int, int)? ShelfSpot(LevelState st, List<int> columns, Func<Block, bool> filter)
        {
            var spots = new List<(Column, Block, int, int)>();
            foreach (var ci in columns)
            {
                var col = st.Columns[ci];
                foreach (var b in col.Blocks)
                {
                    if (!filter(b))
                    {
                        continue;
                    }

                    for (var i = 0; i < b.Slots.Length; i++)
                    {
                        var j = b.Slots[i];
                        if (j != null && !j.IsTool && j.Back == null)
                        {
                            spots.Add((col, b, i, ci));
                        }
                    }
                }
            }

            if (spots.Count == 0)
            {
                return null;
            }

            return spots[(int)(st.NextRandom() * spots.Count)];
        }

        /// <summary>
        /// Picks the item types of a level: one per shape, at most one per color group, spread over departments,
        /// preferring items the player has not found yet (prototype <c>pickTypes</c>).
        /// </summary>
        /// <param name="count">Number of types.</param>
        /// <param name="r">Dedicated random stream so catalog changes never reshuffle shelves.</param>
        /// <param name="look">Lookalike strictness 0..2 (0 keeps similar items apart).</param>
        /// <param name="family">Department of the day, or empty.</param>
        /// <param name="found">Items the player has already found.</param>
        /// <returns>Chosen item ids in pick order.</returns>
        public List<int> PickTypes(int count, Mulberry32 r, int look, string family, ISet<int> found)
        {
            var byFamily = new Dictionary<string, List<ItemDefinition>>();
            var familyKeys = new List<string>();
            foreach (var it in _items)
            {
                if (!byFamily.TryGetValue(it.Family, out var list))
                {
                    list = new List<ItemDefinition>();
                    byFamily[it.Family] = list;
                    familyKeys.Add(it.Family);
                }

                list.Add(it);
            }

            foreach (var k in familyKeys)
            {
                r.Shuffle(byFamily[k]);
            }

            var families = r.Shuffle(_familyOrder.ToList());
            var result = new List<int>();
            var shapes = new HashSet<string>();
            var looks = new HashSet<string>();
            var colorCounts = new Dictionary<string, int>();
            var maxPerColor = look + 1;

            bool IsAllowed(ItemDefinition it) =>
                !shapes.Contains(it.Shape) &&
                (string.IsNullOrEmpty(it.ColorBucket) ||
                 (!looks.Contains(it.Silhouette + "|" + it.ColorBucket) &&
                  colorCounts.GetValueOrDefault(it.ColorBucket) < maxPerColor));

            void Take(ItemDefinition it)
            {
                shapes.Add(it.Shape);
                looks.Add(it.Silhouette + "|" + it.ColorBucket);
                colorCounts[it.ColorBucket] = colorCounts.GetValueOrDefault(it.ColorBucket) + 1;
                result.Add(it.Id);
                foreach (var list in byFamily.Values)
                {
                    list.Remove(it);
                }
            }

            if (look == 2 && count >= 4)
            {
                var byName = _items.GroupBy(i => i.Name).ToDictionary(g => g.Key, g => g.First());
                foreach (var pair in r.Shuffle(Lookalikes.ToList()))
                {
                    if (byName.TryGetValue(pair[0], out var a) && byName.TryGetValue(pair[1], out var b))
                    {
                        Take(a);
                        Take(b);
                        break;
                    }
                }
            }

            if (!string.IsNullOrEmpty(family) && byFamily.TryGetValue(family, out var famList))
            {
                var got = 0;
                foreach (var it in famList.ToList())
                {
                    if (got >= 3 || result.Count >= count)
                    {
                        break;
                    }
                    if (!IsAllowed(it))
                    {
                        continue;
                    }

                    Take(it);
                    got++;
                }
            }

            var index = 0;
            for (var guard = 0; result.Count < count && guard < 5000; guard++)
            {
                if (!byFamily.TryGetValue(families[index % families.Count], out var list) || list.Count == 0)
                {
                    index++;
                    continue;
                }

                index++;
                var k = list.FindIndex(it => IsAllowed(it) && !found.Contains(it.Id));
                if (k < 0)
                {
                    k = list.FindIndex(IsAllowed);
                }
                if (k < 0)
                {
                    list.Clear();
                    continue;
                }

                Take(list[k]);
            }

            // Safety net: if the look rules ran the catalog dry, fill up on shape alone.
            foreach (var it in _items)
            {
                if (result.Count >= count)
                {
                    break;
                }
                if (result.Contains(it.Id) || shapes.Contains(it.Shape))
                {
                    continue;
                }

                shapes.Add(it.Shape);
                result.Add(it.Id);
            }

            return result;
        }
    }
}
