using System;
using System.Collections.Generic;
using Game.Features.Home;
using Game.Services.Album;
using Game.Services.Levels;
using Game.Services.Onboarding;
using Game.Services.Save;
using Game.Services.Wallet;
using NUnit.Framework;
using UniRx;

namespace Game.Tests.EditMode.Features.Home
{
    public sealed class HomeProgressTests
    {
        [Test]
        public void Constructor_WithDependencies_ExposesEachOneUnchanged()
        {
            var profile = new StubProfileService();
            var wallet = new WalletService();
            var levels = new StubLevelProgressService();
            var album = new StubAlbumService();
            var onboarding = new OnboardingService();
            var shop = new ShopProfile();

            var progress = new HomeProgress(profile, wallet, levels, album, onboarding, shop);

            Assert.That(progress.Profile, Is.SameAs(profile));
            Assert.That(progress.Wallet, Is.SameAs(wallet));
            Assert.That(progress.Levels, Is.SameAs(levels));
            Assert.That(progress.Album, Is.SameAs(album));
            Assert.That(progress.Onboarding, Is.SameAs(onboarding));
            Assert.That(progress.Shop, Is.SameAs(shop));

            wallet.Dispose();
            onboarding.Dispose();
            shop.Dispose();
        }

        [Test]
        public void Constructor_WithNullDependencies_KeepsThemNull()
        {
            var progress = new HomeProgress(null, null, null, null, null, null);

            Assert.That(progress.Profile, Is.Null);
            Assert.That(progress.Shop, Is.Null);
        }

        private sealed class StubProfileService : IProfileService
        {
            public IObservable<Unit> Changed => Observable.Never<Unit>();

            public void Load()
            {
            }

            public void SaveAll()
            {
            }

            public void ResetProgress()
            {
            }
        }

        private sealed class StubLevelProgressService : ILevelProgressService
        {
            public IReadOnlyDictionary<int, int> Stars { get; } = new Dictionary<int, int>();

            public bool IsCompleted(int level) => false;

            public void Complete(int level, int stars)
            {
            }

            public bool HasFailed(int level) => false;

            public void MarkFailed(int level)
            {
            }

            public void ClearStars()
            {
            }
        }

        private sealed class StubAlbumService : IAlbumService
        {
            public IReadOnlyList<int> Found { get; } = new List<int>();

            public int NewCount => 0;

            public bool IsFamilyCompleted(string family) => false;

            public string RegisterFound(int itemId) => null;

            public void MarkSeen()
            {
            }
        }
    }
}
