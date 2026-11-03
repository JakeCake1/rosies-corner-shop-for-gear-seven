using System;
using System.Collections.Generic;
using Game.Features.Gameplay.Boosters;
using UniRx;

namespace Game.Tests.EditMode.Features.Gameplay.Session
{
    internal sealed class FakeBoosterInventory : IBoosterInventory
    {
        private readonly Dictionary<string, int> _owned = new Dictionary<string, int>();

        public IObservable<Unit> Changed => Observable.Empty<Unit>();

        public int Get(string id) => _owned.TryGetValue(id, out var count) ? count : 0;

        public void Add(string id, int delta) => _owned[id] = Get(id) + delta;

        public int GetStart(string id) => 0;

        public void AddStart(string id, int delta)
        {
        }

        public bool IsGifted(string id) => false;

        public void MarkGifted(string id)
        {
        }
    }
}
