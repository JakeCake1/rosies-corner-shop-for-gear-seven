using System.Collections.Generic;
using System.Linq;
using Game.Features.Gameplay.Model;

namespace Game.Tests.EditMode.Features.Gameplay
{
    /// <summary>
    /// Replays a level with the fixed tap policy of the prototype recording (plus one Quick Pick and one Tidy Up)
    /// and returns the compact state after every step. Kept out of the tests so they stay free of branching.
    /// </summary>
    internal static class PrototypePlaythrough
    {
        private const int MaxSteps = 400;
        private const int QuickPickStep = 6;
        private const int TidyStep = 11;

        public static List<string> Play(LevelState st)
        {
            var steps = new List<string> { Compact(st) };
            for (var k = 0; k < MaxSteps && Step(st, k, steps); k++)
            {
            }

            return steps;
        }

        // Records one step; false when the run is over.
        private static bool Step(LevelState st, int k, List<string> steps)
        {
            if (ShipDoneBaskets(st, steps))
            {
                return true;
            }

            if (LevelRules.IsWon(st))
            {
                steps.Add("WON");
                return false;
            }

            if (LevelRules.IsLost(st))
            {
                steps.Add("LOST");
                return false;
            }

            if (k == QuickPickStep)
            {
                var slot = LevelRules.QuickPick(st, new List<LevelEvent>(), new Dictionary<int, int>());
                steps.Add("pick" + slot + " " + Compact(st));
                return true;
            }

            if (k == TidyStep)
            {
                var n = LevelRules.Tidy(st, new List<LevelEvent>(), out var column, new List<int>());
                steps.Add("tidy" + (n > 0 ? column : -1) + " " + Compact(st));
                return true;
            }

            return Take(st, steps);
        }

        private static bool ShipDoneBaskets(LevelState st, List<string> steps)
        {
            var shipped = false;
            for (var s = 0; s < LevelState.MaxBaskets; s++)
            {
                if (st.Baskets[s] == null || !st.Baskets[s].IsDone)
                {
                    continue;
                }

                LevelRules.CompleteBasket(st, s, new List<LevelEvent>());
                steps.Add("ship" + s + " " + Compact(st));
                shipped = true;
            }

            return shipped;
        }

        private static bool Take(LevelState st, List<string> steps)
        {
            var visible = LevelRules.VisibleJars(st, false);
            var pick = visible.Where(v => !v.Jar.IsTool && LevelRules.MatchBasket(st, v.Jar.Type) != null)
                .Concat(visible.Where(v => v.Jar.IsTool))
                .Concat(visible.Where(v => !v.Jar.IsWrapped))
                .Concat(visible)
                .FirstOrDefault();
            if (pick.Jar == null)
            {
                steps.Add("NOMOVE");
                return false;
            }

            var r = LevelRules.Take(st, pick.Block.Id, pick.Slot);
            steps.Add("take" + pick.Block.Id + "." + pick.Slot + (r.IsOk ? string.Empty : "X") + " " + Compact(st));
            if (r.IsOk)
            {
                return true;
            }

            steps.Add("STUCK");
            return false;
        }

        private static string Jar(Jar j)
        {
            if (j == null)
            {
                return ".";
            }
            if (j.IsTool)
            {
                return "H";
            }

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
