using System.Collections.Generic;
using Game.Features.Gameplay.Model;

namespace Game.Features.Gameplay.Session
{
    /// <summary>
    /// Lists the catalog items a generated level uses, so their sprites can load before the level is shown.
    /// </summary>
    public static class LevelItemTypes
    {
        /// <summary>
        /// Returns the item type of every jar on the shelves, behind them and in the stock queues; the hammer is not an
        /// item.
        /// </summary>
        /// <param name="state">A freshly generated level.</param>
        /// <returns>Distinct catalog ids.</returns>
        public static HashSet<int> Of(LevelState state)
        {
            var types = new HashSet<int>();
            foreach (var column in state.Columns)
            {
                AddAll(types, column.Queue);
                foreach (var block in column.Blocks)
                {
                    foreach (var jar in block.Slots)
                    {
                        Add(types, jar);
                        Add(types, jar?.Back);
                    }
                }
            }

            return types;
        }

        private static void AddAll(HashSet<int> types, IEnumerable<Jar> jars)
        {
            foreach (var jar in jars)
            {
                Add(types, jar);
            }
        }

        private static void Add(HashSet<int> types, Jar jar)
        {
            if (jar != null && !jar.IsTool)
            {
                types.Add(jar.Type);
            }
        }
    }
}
