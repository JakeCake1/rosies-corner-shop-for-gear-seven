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
        private const int MaxSteps = 400;
        private const int QuickPickStep = 6;
        private const int TidyStep = 11;

        private static JObject _fixture;

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
            var expected = Fixture()[curve][index].ToObject<List<string>>();
            var def = TestContent.Curve(file)[index];
            var generator = new LevelGenerator(TestContent.Items, TestContent.Families);

            var st = generator.Generate(0, def, new HashSet<int>());
            var actual = Play(st);

            for (var k = 0; k < expected.Count; k++)
            {
                Assert.That(k, Is.LessThan(actual.Count), $"{curve} L{index + 1}: run ended early at step {k}");
                Assert.That(actual[k], Is.EqualTo(expected[k]), $"{curve} L{index + 1}: first difference at step {k}");
            }

            Assert.That(actual.Count, Is.EqualTo(expected.Count), $"{curve} L{index + 1}: step count");
        }

        private static JObject Fixture()
        {
            if (_fixture != null) return _fixture;

            var path = "Assets/Game/Tests/EditMode/Features/Gameplay/Fixtures/prototype_playthrough.json";
            _fixture = JObject.Parse(File.ReadAllText(path));
            return _fixture;
        }

        private static List<string> Play(LevelState st)
        {
            var steps = new List<string> { Compact(st) };
            for (var k = 0; k < MaxSteps; k++)
            {
                var shipped = false;
                for (var s = 0; s < LevelState.MaxBaskets; s++)
                {
                    if (st.Baskets[s] == null || !st.Baskets[s].IsDone) continue;

                    LevelRules.CompleteBasket(st, s, new List<LevelEvent>());
                    steps.Add("ship" + s + " " + Compact(st));
                    shipped = true;
                }

                if (shipped) continue;
                if (LevelRules.IsWon(st))
                {
                    steps.Add("WON");
                    break;
                }

                if (LevelRules.IsLost(st))
                {
                    steps.Add("LOST");
                    break;
                }

                if (k == QuickPickStep)
                {
                    var slot = LevelRules.QuickPick(st, new List<LevelEvent>(), new Dictionary<int, int>());
                    steps.Add("pick" + slot + " " + Compact(st));
                    continue;
                }

                if (k == TidyStep)
                {
                    var n = LevelRules.Tidy(st, new List<LevelEvent>(), out var column, new List<int>());
                    steps.Add("tidy" + (n > 0 ? column : -1) + " " + Compact(st));
                    continue;
                }

                var visible = LevelRules.VisibleJars(st, false);
                var pick = visible.Where(v => !v.Jar.IsTool && LevelRules.MatchBasket(st, v.Jar.Type) != null)
                    .Concat(visible.Where(v => v.Jar.IsTool))
                    .Concat(visible.Where(v => !v.Jar.IsWrapped))
                    .Concat(visible)
                    .FirstOrDefault();
                if (pick.Jar == null)
                {
                    steps.Add("NOMOVE");
                    break;
                }

                var r = LevelRules.Take(st, pick.Block.Id, pick.Slot);
                steps.Add("take" + pick.Block.Id + "." + pick.Slot + (r.IsOk ? string.Empty : "X") + " " + Compact(st));
                if (!r.IsOk)
                {
                    steps.Add("STUCK");
                    break;
                }
            }

            return steps;
        }

        private static string Jar(Jar j)
        {
            if (j == null) return ".";
            if (j.IsTool) return "H";

            return (j.IsWrapped ? "w" : string.Empty) + j.Type + (j.Back != null ? "/" + j.Back.Type : string.Empty);
        }

        private static string Compact(LevelState st)
        {
            var cols = st.Columns.Select(c =>
                string.Join("|", c.Blocks.Select(b =>
                    b.Id + ":" + (b.IsLocked ? "L" : string.Empty) + (b.KeyTargetId != 0 ? "K" : string.Empty) +
                    (b.Frozen > 0 ? "F" + b.Frozen : string.Empty) + (b.IsBoarded ? "B" : string.Empty) +
                    "[" + string.Join(",", b.Slots.Select(Jar)) + "]")) +
                "#" + string.Join(",", c.Queue.Select(Jar)));
            return string.Join(" / ", cols) +
                   " C[" + string.Join(",", st.Counter.Select(j => j.Type)) + "]" +
                   " K[" + string.Join(",", st.Baskets.Select(b => b != null ? b.Type + ":" + b.Jars.Count : "-")) + "]" +
                   $" {st.Collected}/{st.Total} p{st.Peak} m{st.Moves} cap{st.Capacity} u{st.UnlockedBaskets}";
        }
    }
}
