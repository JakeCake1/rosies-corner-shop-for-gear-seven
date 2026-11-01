using System;
using System.Collections.Generic;
using Game.Services.Save;

namespace Game.Features.Gameplay.Boosters
{
    /// <summary>
    /// Default <see cref="IBoosterInventory"/>.
    /// </summary>
    public sealed class BoosterInventory : ProfileChunk<BoosterData>, IBoosterInventory
    {
        /// <inheritdoc/>
        public override string Key => "boosters";

        /// <inheritdoc/>
        public int Get(string id) => Count(Data.Owned, id);

        /// <inheritdoc/>
        public void Add(string id, int delta) => Change(Data.Owned, id, delta);

        /// <inheritdoc/>
        public int GetStart(string id) => Count(Data.StartOwned, id);

        /// <inheritdoc/>
        public void AddStart(string id, int delta) => Change(Data.StartOwned, id, delta);

        /// <inheritdoc/>
        public bool IsGifted(string id) => Data.Gifted.Contains(id);

        /// <inheritdoc/>
        public void MarkGifted(string id)
        {
            if (Data.Gifted.Add(id)) MarkChanged();
        }

        private static int Count(Dictionary<string, int> counts, string id) => counts.TryGetValue(id, out var n) ? n : 0;

        private void Change(Dictionary<string, int> counts, string id, int delta)
        {
            counts[id] = Math.Max(0, Count(counts, id) + delta);
            MarkChanged();
        }
    }
}
