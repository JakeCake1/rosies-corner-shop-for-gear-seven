using Game.Services.Audio;
using NUnit.Framework;

namespace Game.Tests.EditMode.Services.Audio
{
    public sealed class TapSoundsTests
    {
        [TestCase("box", SoundId.TapBox)]
        [TestCase("can", SoundId.TapCan)]
        [TestCase("bottle", SoundId.TapBottle)]
        [TestCase("stick", SoundId.TapStick)]
        [TestCase("round", SoundId.TapRound)]
        public void For_WhenKnownSilhouette_ReturnsItsSound(string silhouette, SoundId expected)
        {
            var sound = TapSounds.For(silhouette);

            Assert.That(sound, Is.EqualTo(expected));
        }

        [TestCase("")]
        [TestCase(null)]
        [TestCase("jar")]
        public void For_WhenUnknownSilhouette_ReturnsRoundThud(string silhouette)
        {
            var sound = TapSounds.For(silhouette);

            Assert.That(sound, Is.EqualTo(SoundId.TapRound));
        }
    }
}
