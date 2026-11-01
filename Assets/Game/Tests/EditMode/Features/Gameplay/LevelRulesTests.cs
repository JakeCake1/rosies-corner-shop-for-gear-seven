using System.Collections.Generic;
using System.Linq;
using Game.Features.Gameplay.Model;
using Game.Features.Gameplay.Session;
using NUnit.Framework;

namespace Game.Tests.EditMode.Features.Gameplay
{
    /// <summary>
    /// Focused rule checks on generated levels (the parity test covers whole playthroughs).
    /// </summary>
    public sealed class LevelRulesTests
    {
        private LevelState _state;

        [SetUp]
        public void SetUp()
        {
            var generator = new LevelGenerator(TestContent.Items, TestContent.Families);
            _state = generator.Generate(1, TestContent.Curve("levels_new_20.json")[0], new HashSet<int>());
        }

        [Test]
        public void Generate_LevelOne_OpensTwoBasketsWithDifferentOrders()
        {
            var baskets = _state.Baskets.Where(b => b != null).ToList();

            Assert.That(baskets, Has.Count.EqualTo(2));
            Assert.That(baskets[0].Type, Is.Not.EqualTo(baskets[1].Type));
            Assert.That(_state.Total, Is.EqualTo(18));
        }

        [Test]
        public void Take_WhenBasketWantsItem_PutsItInBasket()
        {
            var basket = _state.Baskets[0];
            var target = LevelRules.VisibleJars(_state, false).First(v => v.Jar.Type == basket.Type);

            var result = LevelRules.Take(_state, target.Block.Id, target.Slot);

            Assert.That(result.IsOk, Is.True);
            Assert.That(result.Basket, Is.SameAs(basket));
            Assert.That(basket.Jars, Has.Count.EqualTo(1));
            Assert.That(_state.Counter, Is.Empty);
        }

        [Test]
        public void Take_WhenCounterFullAndNoBasketWantsItem_IsRefusedWithoutChanges()
        {
            var unwanted = LevelRules.VisibleJars(_state, false).First(v => LevelRules.MatchBasket(_state, v.Jar.Type) == null);
            for (var i = 0; i < _state.Capacity; i++)
            {
                _state.Counter.Add(new Jar { Id = 1000 + i, Type = unwanted.Jar.Type });
            }

            var moves = _state.Moves;

            var result = LevelRules.Take(_state, unwanted.Block.Id, unwanted.Slot);

            Assert.That(result.IsOk, Is.False);
            Assert.That(result.IsCounterFull, Is.True);
            Assert.That(_state.Moves, Is.EqualTo(moves));
            Assert.That(unwanted.Block.Slots[unwanted.Slot], Is.SameAs(unwanted.Jar));
        }

        [Test]
        public void CompleteBasket_WhenFull_AddsThreeToProgressAndOrdersAgain()
        {
            var basket = _state.Baskets[0];
            for (var i = 0; i < Basket.Capacity; i++)
            {
                basket.Jars.Add(new Jar { Id = 2000 + i, Type = basket.Type });
            }

            basket.IsDone = true;

            var type = LevelRules.CompleteBasket(_state, 0, new List<LevelEvent>());

            Assert.That(type, Is.EqualTo(basket.Type));
            Assert.That(_state.Collected, Is.EqualTo(3));
            Assert.That(_state.Shipped, Is.EqualTo(new[] { basket.Type }));
        }

        [Test]
        public void Clone_AfterChanges_LeavesSnapshotUntouched()
        {
            var snapshot = _state.Clone();
            var target = LevelRules.VisibleJars(_state, false).First();

            LevelRules.Take(_state, target.Block.Id, target.Slot);

            Assert.That(snapshot.Moves, Is.EqualTo(0));
            Assert.That(LevelRules.VisibleJars(snapshot, false).Count, Is.EqualTo(LevelRules.VisibleJars(_state, false).Count + 1));
        }

        [TestCase(0, 1, 3)]
        [TestCase(2, 1, 3)]
        [TestCase(3, 1, 2)]
        [TestCase(4, 1, 1)]
        [TestCase(3, 5, 3)]
        [TestCase(4, 5, 2)]
        [TestCase(5, 5, 1)]
        public void StarsFor_ByCounterPeak_MatchesPrototype(int peak, int level, int stars)
        {
            Assert.That(GameplayBalance.StarsFor(peak, level), Is.EqualTo(stars));
        }
    }
}
