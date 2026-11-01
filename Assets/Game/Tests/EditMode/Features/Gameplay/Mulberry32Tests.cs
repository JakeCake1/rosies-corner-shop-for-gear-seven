using Game.Features.Gameplay.Model;
using NUnit.Framework;

namespace Game.Tests.EditMode.Features.Gameplay
{
    /// <summary>
    /// The PRNG must reproduce the prototype's JavaScript values exactly (reference values taken from Node).
    /// </summary>
    public sealed class Mulberry32Tests
    {
        [Test]
        public void Next_WithLevelOneSeed_MatchesJavaScript()
        {
            var r = new Mulberry32(51013);

            var a = r.Next();
            var b = r.Next();
            var c = r.Next();

            Assert.That(a, Is.EqualTo(0.364691943628713).Within(1e-12));
            Assert.That(b, Is.EqualTo(0.067226348910481).Within(1e-12));
            Assert.That(c, Is.EqualTo(0.741352862445638).Within(1e-12));
        }

        [Test]
        public void NextRandom_InLevelStream_MatchesJavaScriptRnd()
        {
            var st = new LevelState { Seed = 51013 };

            var a = st.NextRandom();
            var b = st.NextRandom();

            Assert.That(a, Is.EqualTo(0.227599600795656).Within(1e-12));
            Assert.That(b, Is.EqualTo(0.575173042714596).Within(1e-12));
            Assert.That(st.RandomCounter, Is.EqualTo(2));
        }
    }
}
