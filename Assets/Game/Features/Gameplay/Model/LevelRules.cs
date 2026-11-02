using System.Collections.Generic;
using System.Linq;

namespace Game.Features.Gameplay.Model
{
    /// <summary>
    /// The rules of a level as pure functions over <see cref="LevelState"/>: taking items, ordering baskets,
    /// shipping, winning, losing and the in-level boosters. A port of the prototype's rule functions.
    /// </summary>
    /// <remarks>
    /// Called by <c>LevelSession</c> only; views never mutate the state. Every method that changes the state lists
    /// what happened in the <c>events</c> argument so the board can animate it.
    /// </remarks>
    public static class LevelRules
    {
        /// <summary>
        /// Locates a shelf by id.
        /// </summary>
        /// <param name="st">Level state.</param>
        /// <param name="blockId">Shelf id.</param>
        /// <param name="column">Column index of the shelf, or -1.</param>
        /// <returns>The shelf, or null when it is gone.</returns>
        public static Block FindBlock(LevelState st, int blockId, out int column)
        {
            for (var c = 0; c < LevelState.ColumnCount; c++)
            {
                foreach (var b in st.Columns[c].Blocks)
                {
                    if (b.Id != blockId)
                    {
                        continue;
                    }

                    column = c;
                    return b;
                }
            }

            column = -1;
            return null;
        }

        /// <summary>
        /// Lists front items the player could see, tools included (prototype <c>visibleJars</c>).
        /// </summary>
        /// <param name="st">Level state.</param>
        /// <param name="includeClosed">Also list tied, frozen and nailed shelves (Quick Pick reaches into them).</param>
        /// <returns>Items in column, shelf (bottom first) and cell order.</returns>
        public static List<JarLocation> VisibleJars(LevelState st, bool includeClosed)
        {
            var result = new List<JarLocation>();
            for (var c = 0; c < LevelState.ColumnCount; c++)
            {
                foreach (var b in st.Columns[c].Blocks)
                {
                    if (!includeClosed && !b.IsOpen)
                    {
                        continue;
                    }

                    for (var i = 0; i < b.Slots.Length; i++)
                    {
                        if (b.Slots[i] != null)
                        {
                            result.Add(new JarLocation(b, c, i, b.Slots[i]));
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Picks the next basket order: what already waits on the counter first, then the level's order list
        /// (prototype <c>pickTarget</c>). Removes the chosen order from the list.
        /// </summary>
        /// <param name="st">Level state.</param>
        /// <returns>The item type to order, or null when no type has three items left.</returns>
        public static int? PickTarget(LevelState st)
        {
            var active = new HashSet<int>(st.Baskets.Where(b => b != null).Select(b => b.Type));
            var totals = TypeTotals(st);
            int Total(int t) => totals.TryGetValue(t, out var n) ? n : 0;

            var onCounter = new SortedDictionary<int, int>();
            foreach (var j in st.Counter)
            {
                onCounter[j.Type] = (onCounter.TryGetValue(j.Type, out var n) ? n : 0) + 1;
            }

            int? best = null;
            var bestCount = 0;
            foreach (var kv in onCounter)
            {
                if (!active.Contains(kv.Key) && Total(kv.Key) >= Basket.Capacity && kv.Value > bestCount)
                {
                    bestCount = kv.Value;
                    best = kv.Key;
                }
            }

            if (best.HasValue)
            {
                st.OrderSequence.Remove(best.Value);
                return best;
            }

            for (var k = 0; k < st.OrderSequence.Count; k++)
            {
                var t = st.OrderSequence[k];
                if (active.Contains(t) || Total(t) < Basket.Capacity)
                {
                    continue;
                }

                st.OrderSequence.RemoveAt(k);
                return t;
            }

            foreach (var t in totals.Keys.OrderBy(t => t))
            {
                if (!active.Contains(t) && Total(t) >= Basket.Capacity)
                {
                    return t;
                }
            }

            return null;
        }

        /// <summary>
        /// Fills empty open basket slots with new orders; a new basket immediately takes up to three matching items
        /// from the counter (prototype <c>refillBaskets</c>).
        /// </summary>
        /// <param name="st">Level state.</param>
        /// <param name="events">Receives <see cref="LevelEventType.BasketArrived"/>; may be null.</param>
        public static void RefillBaskets(LevelState st, List<LevelEvent> events)
        {
            for (var i = 0; i < st.UnlockedBaskets; i++)
            {
                if (st.Baskets[i] != null)
                {
                    continue;
                }

                var t = PickTarget(st);
                if (!t.HasValue)
                {
                    continue;
                }

                var basket = new Basket { Id = st.NextId++, Type = t.Value };
                st.Baskets[i] = basket;
                for (var k = 0; k < st.Counter.Count && basket.Jars.Count < Basket.Capacity;)
                {
                    if (st.Counter[k].Type == t.Value)
                    {
                        basket.Jars.Add(st.Counter[k]);
                        st.Counter.RemoveAt(k);
                    }
                    else
                    {
                        k++;
                    }
                }

                if (basket.Jars.Count == Basket.Capacity)
                {
                    basket.IsDone = true;
                }
                events?.Add(LevelEvent.Of(LevelEventType.BasketArrived, basket.Id, i));
            }
        }

        /// <summary>
        /// Taps an item (prototype <c>take</c>): the hammer opens the nailed shelf; an item goes to a waiting basket,
        /// otherwise to the counter; a full counter refuses the move.
        /// </summary>
        /// <param name="st">Level state.</param>
        /// <param name="blockId">Shelf of the tapped item.</param>
        /// <param name="slot">Cell index on that shelf.</param>
        /// <returns>The outcome with the events to animate.</returns>
        public static TakeResult Take(LevelState st, int blockId, int slot)
        {
            var block = FindBlock(st, blockId, out var column);
            if (block == null || !block.IsOpen)
            {
                return TakeResult.Rejected(false);
            }

            var jar = block.Slots[slot];
            if (jar == null)
            {
                return TakeResult.Rejected(false);
            }

            var events = new List<LevelEvent>();
            if (jar.IsTool)
            {
                block.Slots[slot] = jar.Back;
                st.Moves++;
                var target = FindBlock(st, jar.TargetBlockId, out _);
                if (target != null && target.IsBoarded)
                {
                    target.IsBoarded = false;
                    events.Add(LevelEvent.Of(LevelEventType.ShelfUnboarded, target.Id, jar.Id));
                }

                FoldIfEmpty(st, block, column, events);
                return new TakeResult(true, false, jar, null, true, events);
            }

            var basket = MatchBasket(st, jar.Type);
            if (basket == null && st.Counter.Count >= st.Capacity)
            {
                return TakeResult.Rejected(true);
            }

            block.Slots[slot] = jar.Back;
            var item = new Jar { Id = jar.Id, Type = jar.Type };
            st.Moves++;
            if (basket != null)
            {
                basket.Jars.Add(item);
                if (basket.Jars.Count == Basket.Capacity)
                {
                    basket.IsDone = true;
                }
            }
            else
            {
                st.Counter.Add(item);
                if (st.Counter.Count > st.Peak)
                {
                    st.Peak = st.Counter.Count;
                }
            }

            UnwrapRow(st, block, slot, events);
            FoldIfEmpty(st, block, column, events);
            return new TakeResult(true, false, item, basket, false, events);
        }

        /// <summary>
        /// Ships a full basket: progress +3, every frozen shelf melts by one, a new order arrives
        /// (prototype <c>completeBasket</c>).
        /// </summary>
        /// <param name="st">Level state.</param>
        /// <param name="slot">Basket slot.</param>
        /// <param name="events">Receives thaw and basket events.</param>
        /// <returns>The shipped item type, or null when the slot holds no full basket.</returns>
        public static int? CompleteBasket(LevelState st, int slot, List<LevelEvent> events)
        {
            var basket = st.Baskets[slot];
            if (basket == null || !basket.IsDone)
            {
                return null;
            }

            st.Collected += Basket.Capacity;
            st.Shipped.Add(basket.Type);
            st.Baskets[slot] = null;
            foreach (var col in st.Columns)
            {
                foreach (var b in col.Blocks)
                {
                    if (b.Frozen <= 0)
                    {
                        continue;
                    }

                    b.Frozen--;
                    if (b.Frozen == 0)
                    {
                        events.Add(LevelEvent.Of(LevelEventType.ShelfThawed, b.Id));
                    }
                }
            }

            RefillBaskets(st, events);
            return basket.Type;
        }

        /// <summary>
        /// True once every item of the level is packed and shipped.
        /// </summary>
        /// <param name="st">Level state.</param>
        /// <returns>Whether the level is won.</returns>
        public static bool IsWon(LevelState st) => st.Collected >= st.Total;

        /// <summary>
        /// True when no move can help any more (prototype <c>isLost</c>); never while a full basket waits to ship.
        /// </summary>
        /// <param name="st">Level state.</param>
        /// <returns>Whether the level is lost.</returns>
        public static bool IsLost(LevelState st)
        {
            if (st.Baskets.Any(b => b != null && b.IsDone))
            {
                return false;
            }
            if (st.Collected >= st.Total)
            {
                return false;
            }

            var visible = VisibleJars(st, false);
            if (st.Counter.Count < st.Capacity)
            {
                return visible.Count == 0;
            }

            return !visible.Any(v => v.Jar.IsTool || MatchBasket(st, v.Jar.Type) != null);
        }

        /// <summary>
        /// Quick Pick booster: fills the basket closest to done from the counter, then any shelf (even closed ones),
        /// then the stockrooms (prototype <c>bQuickPick</c>).
        /// </summary>
        /// <param name="st">Level state.</param>
        /// <param name="events">Receives fold, unlock and arrival events.</param>
        /// <param name="fromStock">Receives jar id → column for items pulled out of a stockroom.</param>
        /// <returns>The filled basket slot, or -1 when no basket is waiting.</returns>
        public static int QuickPick(LevelState st, List<LevelEvent> events, Dictionary<int, int> fromStock)
        {
            var slot = -1;
            var bestCount = -1;
            for (var i = 0; i < LevelState.MaxBaskets; i++)
            {
                var b = st.Baskets[i];
                if (b != null && !b.IsDone && b.Jars.Count > bestCount)
                {
                    bestCount = b.Jars.Count;
                    slot = i;
                }
            }

            if (slot < 0)
            {
                return -1;
            }

            var basket = st.Baskets[slot];
            for (var k = 0; k < st.Counter.Count && basket.Jars.Count < Basket.Capacity;)
            {
                if (st.Counter[k].Type == basket.Type)
                {
                    basket.Jars.Add(st.Counter[k]);
                    st.Counter.RemoveAt(k);
                }
                else
                {
                    k++;
                }
            }

            foreach (var v in VisibleJars(st, true))
            {
                if (basket.Jars.Count >= Basket.Capacity)
                {
                    break;
                }
                if (v.Jar.IsTool || v.Jar.Type != basket.Type || v.Block.Slots[v.Slot] != v.Jar)
                {
                    continue;
                }

                v.Block.Slots[v.Slot] = v.Jar.Back;
                basket.Jars.Add(new Jar { Id = v.Jar.Id, Type = v.Jar.Type });
                if (v.Block.IsEmpty())
                {
                    RemoveBlock(st, v.Column, v.Block, events);
                }
            }

            for (var c = 0; c < LevelState.ColumnCount && basket.Jars.Count < Basket.Capacity; c++)
            {
                var q = st.Columns[c].Queue;
                for (var k = 0; k < q.Count && basket.Jars.Count < Basket.Capacity;)
                {
                    if (!q[k].IsTool && q[k].Type == basket.Type)
                    {
                        var j = q[k];
                        q.RemoveAt(k);
                        basket.Jars.Add(j);
                        fromStock[j.Id] = c;
                    }
                    else
                    {
                        k++;
                    }
                }
            }

            LevelGenerator.FillColumns(st, events);
            if (basket.Jars.Count == Basket.Capacity)
            {
                basket.IsDone = true;
            }
            st.Moves++;
            return slot;
        }

        /// <summary>
        /// Tidy Up booster: returns up to three of the oldest counter items to the stockroom of the column with the
        /// shortest stock (prototype <c>bTidy</c>).
        /// </summary>
        /// <param name="st">Level state.</param>
        /// <param name="events">Receives arrival events for shelves cut right away.</param>
        /// <param name="column">Column whose stockroom received the items.</param>
        /// <param name="backInStock">Ids of returned items still in the stockroom (they fly into the label).</param>
        /// <returns>Number of items moved; 0 when the counter is empty.</returns>
        public static int Tidy(LevelState st, List<LevelEvent> events, out int column, List<int> backInStock)
        {
            column = 0;
            var n = System.Math.Min(3, st.Counter.Count);
            if (n == 0)
            {
                return 0;
            }

            var jars = st.Counter.GetRange(0, n);
            st.Counter.RemoveRange(0, n);
            for (var i = 0; i < LevelState.ColumnCount; i++)
            {
                if (st.Columns[i].Queue.Count < st.Columns[column].Queue.Count)
                {
                    column = i;
                }
            }

            st.Columns[column].Queue.InsertRange(0, jars);
            LevelGenerator.FillColumns(st, events);
            st.Moves++;
            var queue = st.Columns[column].Queue;
            backInStock.AddRange(jars.Where(queue.Contains).Select(j => j.Id));
            return n;
        }

        /// <summary>
        /// Shuffle booster: mixes the items on open shelves (tools stay put). Only offered on the original curve.
        /// </summary>
        /// <param name="st">Level state.</param>
        /// <param name="random">Time-seeded stream, so repeated shuffles differ.</param>
        /// <returns>False when fewer than two items can move.</returns>
        public static bool Shuffle(LevelState st, Mulberry32 random)
        {
            var positions = new List<(Block Block, int Slot)>();
            foreach (var col in st.Columns)
            {
                foreach (var b in col.Blocks)
                {
                    if (!b.IsOpen)
                    {
                        continue;
                    }

                    for (var i = 0; i < b.Slots.Length; i++)
                    {
                        if (b.Slots[i] != null && !b.Slots[i].IsTool)
                        {
                            positions.Add((b, i));
                        }
                    }
                }
            }

            if (positions.Count < 2)
            {
                return false;
            }

            var jars = positions.Select(p => p.Block.Slots[p.Slot]).ToList();
            var before = string.Join(",", jars.Select(j => j.Id));
            random.Shuffle(jars);
            if (string.Join(",", jars.Select(j => j.Id)) == before)
            {
                jars.Add(jars[0]);
                jars.RemoveAt(0);
            }

            for (var k = 0; k < positions.Count; k++)
            {
                positions[k].Block.Slots[positions[k].Slot] = jars[k];
            }

            st.Moves++;
            return true;
        }

        /// <summary>
        /// Finds the move the hidden hint highlights: an item an open basket wants, from the emptiest shelf;
        /// otherwise the hammer (prototype <c>bestHint</c>).
        /// </summary>
        /// <param name="st">Level state.</param>
        /// <returns>The jar to highlight, or null.</returns>
        public static Jar BestHint(LevelState st)
        {
            var visible = VisibleJars(st, false);
            var wanted = new HashSet<int>(st.Baskets.Where(b => b != null && !b.IsDone).Select(b => b.Type));
            var matches = visible.Where(v => !v.Jar.IsTool && !v.Jar.IsWrapped && wanted.Contains(v.Jar.Type))
                .OrderBy(v => v.Block.Slots.Count(s => s != null)).ToList();
            if (matches.Count > 0)
            {
                return matches[0].Jar;
            }

            return visible.FirstOrDefault(v => v.Jar.IsTool).Jar;
        }

        /// <summary>
        /// Returns the open, not-yet-full basket that wants <paramref name="type"/>.
        /// </summary>
        /// <param name="st">Level state.</param>
        /// <param name="type">Item type.</param>
        /// <returns>The basket, or null.</returns>
        public static Basket MatchBasket(LevelState st, int type) =>
            st.Baskets.FirstOrDefault(b => b != null && !b.IsDone && b.Type == type && b.Jars.Count < Basket.Capacity);

        private static Dictionary<int, int> TypeTotals(LevelState st)
        {
            var totals = new Dictionary<int, int>();
            void Add(int t) => totals[t] = (totals.TryGetValue(t, out var n) ? n : 0) + 1;

            foreach (var j in st.Counter)
            {
                Add(j.Type);
            }

            foreach (var col in st.Columns)
            {
                foreach (var b in col.Blocks)
                {
                    foreach (var j in b.Slots)
                    {
                        if (j == null || j.IsTool)
                        {
                            continue;
                        }

                        Add(j.Type);
                        if (j.Back != null)
                        {
                            Add(j.Back.Type);
                        }
                    }
                }

                foreach (var j in col.Queue)
                {
                    if (!j.IsTool)
                    {
                        Add(j.Type);
                    }
                }
            }

            return totals;
        }

        private static void UnwrapRow(LevelState st, Block block, int slot, List<LevelEvent> events)
        {
            var row = slot / st.JarsPerRow;
            for (var k = row * st.JarsPerRow; k < (row + 1) * st.JarsPerRow; k++)
            {
                var s = block.Slots[k];
                if (s == null || !s.IsWrapped)
                {
                    continue;
                }

                var open = s.CloneShallow();
                open.IsWrapped = false;
                block.Slots[k] = open;
                events.Add(LevelEvent.Of(LevelEventType.JarUnwrapped, s.Id));
            }
        }

        private static void FoldIfEmpty(LevelState st, Block block, int column, List<LevelEvent> events)
        {
            if (!block.IsEmpty())
            {
                return;
            }

            RemoveBlock(st, column, block, events);
            LevelGenerator.FillColumns(st, events);
        }

        private static void RemoveBlock(LevelState st, int column, Block block, List<LevelEvent> events)
        {
            st.Columns[column].Blocks.Remove(block);
            events.Add(LevelEvent.Of(LevelEventType.ShelfFolded, block.Id, column));
            if (block.KeyTargetId == 0)
            {
                return;
            }

            var target = FindBlock(st, block.KeyTargetId, out _);
            if (target != null && target.IsLocked)
            {
                target.IsLocked = false;
                events.Add(LevelEvent.Of(LevelEventType.ShelfUnlocked, target.Id, block.Id));
            }
        }
    }
}
