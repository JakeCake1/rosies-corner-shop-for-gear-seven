using Game.Services.Onboarding;
using NUnit.Framework;
using UniRx;

namespace Game.Tests.EditMode.Services.Onboarding
{
    public sealed class OnboardingServiceTests
    {
        private OnboardingService _onboarding;
        private int _changedCount;

        [SetUp]
        public void SetUp()
        {
            _onboarding = new OnboardingService();
            _changedCount = 0;
            _onboarding.Changed.Subscribe(_ => _changedCount++);
        }

        [TearDown]
        public void TearDown()
        {
            _onboarding.Dispose();
        }

        [Test]
        public void Key_Always_IsOnboarding()
        {
            Assert.That(_onboarding.Key, Is.EqualTo("onboarding"));
        }

        [Test]
        public void HasFlag_ForNewPlayer_IsFalse()
        {
            Assert.That(_onboarding.HasFlag(Flags.AlbumIntro), Is.False);
        }

        [Test]
        public void SetFlag_WhenNotSet_SetsOnlyThatFlagAndReportsChange()
        {
            _onboarding.SetFlag(Flags.AlbumIntro);

            Assert.That(_onboarding.HasFlag(Flags.AlbumIntro), Is.True);
            Assert.That(_onboarding.HasFlag(Flags.AlbumOpened), Is.False);
            Assert.That(_changedCount, Is.EqualTo(1));
        }

        [Test]
        public void SetFlag_WhenAlreadySet_DoesNotReportChangeAgain()
        {
            _onboarding.SetFlag(Flags.NameAsked);

            _onboarding.SetFlag(Flags.NameAsked);

            Assert.That(_changedCount, Is.EqualTo(1));
        }

        [Test]
        public void MarkTipSeen_WhenNotSeen_MarksTipAndReportsChange()
        {
            _onboarding.MarkTipSeen("ship");

            Assert.That(_onboarding.HasSeenTip("ship"), Is.True);
            Assert.That(_onboarding.HasSeenTip("stock"), Is.False);
            Assert.That(_changedCount, Is.EqualTo(1));
        }

        [Test]
        public void MarkTipSeen_WhenAlreadySeen_DoesNotReportChangeAgain()
        {
            _onboarding.MarkTipSeen("counter");

            _onboarding.MarkTipSeen("counter");

            Assert.That(_changedCount, Is.EqualTo(1));
        }

        [Test]
        public void FlagsAndTips_AreIndependent()
        {
            _onboarding.SetFlag("shared");

            Assert.That(_onboarding.HasSeenTip("shared"), Is.False);
        }

        [Test]
        public void Restore_WithSavedData_ExposesSavedFlagsAndTips()
        {
            var data = new OnboardingData();
            data.Flags.Add(Flags.FreeBasket);
            data.SeenTips.Add("counter");

            _onboarding.Restore(data);

            Assert.That(_onboarding.HasFlag(Flags.FreeBasket), Is.True);
            Assert.That(_onboarding.HasSeenTip("counter"), Is.True);
        }

        [Test]
        public void Restore_WithNull_ClearsEverything()
        {
            _onboarding.SetFlag(Flags.FreeBasket);
            _onboarding.MarkTipSeen("counter");

            _onboarding.Restore(null);

            Assert.That(_onboarding.HasFlag(Flags.FreeBasket), Is.False);
            Assert.That(_onboarding.HasSeenTip("counter"), Is.False);
        }
    }
}
