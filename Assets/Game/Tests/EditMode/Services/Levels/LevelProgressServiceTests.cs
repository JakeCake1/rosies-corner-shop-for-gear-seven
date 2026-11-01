using Game.Services.Levels;
using NUnit.Framework;

namespace Game.Tests.EditMode.Services.Levels
{
    public sealed class LevelProgressServiceTests
    {
        private LevelProgressService _levels;

        [SetUp]
        public void SetUp() => _levels = new LevelProgressService();

        [TearDown]
        public void TearDown() => _levels.Dispose();

        [Test]
        public void Complete_KeepsBestStars()
        {
            _levels.Complete(4, 3);
            _levels.Complete(4, 1);

            Assert.That(_levels.Stars[4], Is.EqualTo(3));
        }

        [Test]
        public void ClearStars_KeepsFailedLevels()
        {
            _levels.Complete(2, 2);
            _levels.MarkFailed(3);

            _levels.ClearStars();

            Assert.That(_levels.IsCompleted(2), Is.False);
            Assert.That(_levels.HasFailed(3), Is.True);
        }
    }
}
