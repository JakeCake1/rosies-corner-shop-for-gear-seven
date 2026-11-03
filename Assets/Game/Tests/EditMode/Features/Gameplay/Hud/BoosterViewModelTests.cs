using System.Collections.Generic;
using Game.Features.Gameplay.Hud;
using NUnit.Framework;

namespace Game.Tests.EditMode.Features.Gameplay.Hud
{
    public sealed class BoosterViewModelTests
    {
        private readonly List<string> _used = new List<string>();
        private BoosterViewModel _viewModel;

        [SetUp]
        public void SetUp()
        {
            _used.Clear();
            _viewModel = new BoosterViewModel("pick", "Quick Pick", _used.Add);
        }

        [TearDown]
        public void TearDown() => _viewModel.Dispose();

        [Test]
        public void Constructor_ShowsPlusBadge()
        {
            Assert.That(_viewModel.Id, Is.EqualTo("pick"));
            Assert.That(_viewModel.Title, Is.EqualTo("Quick Pick"));
            Assert.That(_viewModel.Count.Value, Is.EqualTo("+"));
            Assert.That(_viewModel.IsPlus.Value, Is.True);
            Assert.That(_viewModel.IsDimmed.Value, Is.False);
            Assert.That(_viewModel.IsPulsing.Value, Is.False);
            Assert.That(_viewModel.IsGift.Value, Is.False);
        }

        [Test]
        public void Set_WhenCountPositive_ShowsCount()
        {
            _viewModel.Set(3, true, true);

            Assert.That(_viewModel.Count.Value, Is.EqualTo("3"));
            Assert.That(_viewModel.IsPlus.Value, Is.False);
            Assert.That(_viewModel.IsDimmed.Value, Is.True);
            Assert.That(_viewModel.IsPulsing.Value, Is.True);
        }

        [TestCase(0)]
        [TestCase(-2)]
        public void Set_WhenCountNotPositive_ShowsPlus(int count)
        {
            _viewModel.Set(5, false, false);
            _viewModel.Set(count, false, false);

            Assert.That(_viewModel.Count.Value, Is.EqualTo("+"));
            Assert.That(_viewModel.IsPlus.Value, Is.True);
        }

        [Test]
        public void Use_CallsCallbackWithIdAndClearsGift()
        {
            _viewModel.SetGift(true);

            _viewModel.Use();

            Assert.That(_used, Is.EqualTo(new[] { "pick" }));
            Assert.That(_viewModel.IsGift.Value, Is.False);
        }

        [Test]
        public void SetGift_TurnsPulseOnAndOff()
        {
            _viewModel.SetGift(true);
            Assert.That(_viewModel.IsGift.Value, Is.True);

            _viewModel.SetGift(false);
            Assert.That(_viewModel.IsGift.Value, Is.False);
        }
    }
}
