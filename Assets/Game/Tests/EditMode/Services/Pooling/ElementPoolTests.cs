using System;
using Game.Services.Pooling;
using Game.Services.UI.Elements;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace Game.Tests.EditMode.Services.Pooling
{
    public sealed class ElementPoolTests
    {
        private ElementPool<TestView> _pool;
        private int _createdCount;

        [SetUp]
        public void SetUp()
        {
            _createdCount = 0;
            _pool = new ElementPool<TestView>(Create, 2, 2);
        }

        [TearDown]
        public void TearDown()
        {
            _pool.Dispose();
        }

        [Test]
        public void Get_WhenPoolEmpty_CreatesViewAndSpawnsIt()
        {
            var view = _pool.Get();

            Assert.That(_createdCount, Is.EqualTo(1));
            Assert.That(view.SpawnedCount, Is.EqualTo(1));
            Assert.That(view.DespawnedCount, Is.Zero);
        }

        [Test]
        public void Release_WhenViewAttached_DespawnsAndDetachesIt()
        {
            var view = _pool.Get();
            var parent = new VisualElement();
            parent.Add(view.Root);

            _pool.Release(view);

            Assert.That(view.DespawnedCount, Is.EqualTo(1));
            Assert.That(view.Root.parent, Is.Null);
            Assert.That(parent.childCount, Is.Zero);
        }

        [Test]
        public void Get_AfterRelease_ReusesViewAndSpawnsItAgain()
        {
            var first = _pool.Get();
            _pool.Release(first);

            var second = _pool.Get();

            Assert.That(second, Is.SameAs(first));
            Assert.That(_createdCount, Is.EqualTo(1));
            Assert.That(second.SpawnedCount, Is.EqualTo(2));
        }

        [Test]
        public void Release_WhenPoolIsFull_DropsExtraViewInsteadOfKeepingIt()
        {
            var a = _pool.Get();
            var b = _pool.Get();
            var c = _pool.Get();

            _pool.Release(a);
            _pool.Release(b);
            _pool.Release(c);
            var reusedFirst = _pool.Get();
            var reusedSecond = _pool.Get();
            var fresh = _pool.Get();

            Assert.That(new[] { reusedFirst, reusedSecond }, Is.EquivalentTo(new[] { a, b }));
            Assert.That(fresh, Is.Not.SameAs(c));
            Assert.That(_createdCount, Is.EqualTo(4));
        }

        [Test]
        public void Release_WhenViewAlreadyReleased_Throws()
        {
            var view = _pool.Get();
            _pool.Release(view);

            Assert.Throws<InvalidOperationException>(() => _pool.Release(view));
        }

        [Test]
        public void Dispose_WithKeptViews_ForgetsThemSoGetCreatesNewOnes()
        {
            var view = _pool.Get();
            _pool.Release(view);

            _pool.Dispose();
            var next = _pool.Get();

            Assert.That(next, Is.Not.SameAs(view));
            Assert.That(_createdCount, Is.EqualTo(2));
        }

        private TestView Create()
        {
            _createdCount++;
            var view = new TestView();
            view.Attach(new VisualElement());
            return view;
        }

        private sealed class TestView : ElementView, IPoolable
        {
            public int SpawnedCount { get; private set; }

            public int DespawnedCount { get; private set; }

            public void OnSpawned()
            {
                SpawnedCount++;
            }

            public void OnDespawned()
            {
                DespawnedCount++;
            }

            protected override void OnAttached()
            {
            }
        }
    }
}
