using Game.Services.Save;
using Game.Services.Settings;
using Game.Services.Wallet;
using NUnit.Framework;
using VContainer;

namespace Game.Tests.EditMode.Services.Save
{
    /// <summary>
    /// Chunk owners registered with <c>AsImplementedInterfaces()</c> reach <see cref="ProfileService"/> as one
    /// collection, the way <c>ProjectLifetimeScope.RegisterProfileChunks</c> wires them.
    /// </summary>
    public sealed class ProfileCompositionTests
    {
        [Test]
        public void Build_WithRegisteredOwners_ProfileServiceSavesEveryChunk()
        {
            var save = new RecordingSaveService();
            var builder = new ContainerBuilder();
            builder.RegisterInstance<ISaveService>(save);
            builder.Register<ProfileService>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<WalletService>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<SettingsService>(Lifetime.Singleton).AsImplementedInterfaces();
            var container = builder.Build();

            var profile = container.Resolve<IProfileService>();
            profile.Load();
            profile.SaveAll();
            container.Resolve<IWalletService>().Add(1);

            Assert.That(save.Saved, Is.EquivalentTo(new[] { "wallet", "settings", "wallet" }));
            container.Dispose();
        }
    }
}
