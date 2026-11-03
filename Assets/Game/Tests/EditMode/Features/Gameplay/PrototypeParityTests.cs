using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Features.Gameplay.Model;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Game.Tests.EditMode.Features.Gameplay
{
    /// <summary>
    /// Replays every level of both curves with a fixed tap policy (plus one Quick Pick and one Tidy Up) and
    /// compares the state after each step with a recording of the original HTML prototype's rule code.
    /// </summary>
    /// <remarks>
    /// The fixture comes from <c>Fixtures/prototype_playthrough.js.txt</c> run in Node on the prototype's own
    /// functions; any difference in generation, randomness or rules shows up as the first differing step.
    /// </remarks>
    public sealed class PrototypeParityTests
    {
        private JObject _fixture;

        private static IEnumerable<TestCaseData> Levels()
        {
            foreach (var (curve, file, count) in new[] { ("new", "levels_new_20.json", 20), ("orig", "levels_original_10.json", 10) })
            {
                for (var i = 0; i < count; i++)
                {
                    yield return new TestCaseData(curve, file, i).SetName($"Playthrough_{curve}_L{i + 1}_MatchesPrototype");
                }
            }
        }

        [TestCaseSource(nameof(Levels))]
        public void Playthrough_MatchesPrototype(string curve, string file, int index)
        {
            var expected = _fixture[curve][index].ToObject<List<string>>();
            var def = TestContent.Curve(file)[index];
            var generator = new LevelGenerator(TestContent.Items, TestContent.Families);

            var st = generator.Generate(0, def, new HashSet<int>());
            var actual = PrototypePlaythrough.Play(st);

            for (var k = 0; k < expected.Count; k++)
            {
                Assert.That(k, Is.LessThan(actual.Count), $"{curve} L{index + 1}: run ended early at step {k}");
                Assert.That(actual[k], Is.EqualTo(expected[k]), $"{curve} L{index + 1}: first difference at step {k}");
            }

            Assert.That(actual.Count, Is.EqualTo(expected.Count), $"{curve} L{index + 1}: step count");
        }

        [OneTimeSetUp]
        public void LoadFixture() =>
            _fixture = JObject.Parse(File.ReadAllText("Assets/Game/Tests/EditMode/Features/Gameplay/Fixtures/prototype_playthrough.json"));
    }
}
