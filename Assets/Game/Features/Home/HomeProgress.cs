using Game.Services.Album;
using Game.Services.Levels;
using Game.Services.Onboarding;
using Game.Services.Save;
using Game.Services.Wallet;

namespace Game.Features.Home
{
    /// <summary>
    /// Parameter object bundling the profile chunks the home screen shows, to keep its constructors short.
    /// </summary>
    public sealed class HomeProgress
    {
        /// <summary>
        /// Creates the bundle; resolved by VContainer.
        /// </summary>
        /// <param name="profile">Emits after any chunk changed.</param>
        /// <param name="wallet">Coin chip.</param>
        /// <param name="levels">Next level and milestones.</param>
        /// <param name="album">Album badge and showcase.</param>
        /// <param name="onboarding">Milestone popups and the album button pulse.</param>
        /// <param name="shop">Name on the sign.</param>
        public HomeProgress(IProfileService profile, IWalletService wallet, ILevelProgressService levels,
            IAlbumService album, IOnboardingService onboarding, IShopProfile shop)
        {
            Profile = profile;
            Wallet = wallet;
            Levels = levels;
            Album = album;
            Onboarding = onboarding;
            Shop = shop;
        }

        /// <summary>Emits after any chunk changed.</summary>
        public IProfileService Profile { get; }

        /// <summary>Coin chip.</summary>
        public IWalletService Wallet { get; }

        /// <summary>Next level and milestones.</summary>
        public ILevelProgressService Levels { get; }

        /// <summary>Album badge and showcase.</summary>
        public IAlbumService Album { get; }

        /// <summary>Milestone popups and the album button pulse.</summary>
        public IOnboardingService Onboarding { get; }

        /// <summary>Name on the sign.</summary>
        public IShopProfile Shop { get; }
    }
}
