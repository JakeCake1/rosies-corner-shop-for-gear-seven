using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Services.Content;
using Game.Services.Levels;
using Newtonsoft.Json.Linq;

namespace Game.Tests.EditMode.Features.Gameplay
{
    /// <summary>
    /// Loads the catalog and level tables straight from the source JSON (no sprites, no assets), so rule tests
    /// don't depend on the generated assets.
    /// </summary>
    internal static class TestContent
    {
        public static readonly string[] Families = { "dairy", "bakery", "drink", "pantry", "sweet", "fresh", "home" };

        // A fresh list per access: tests may mutate it and must not share state.
        public static List<ItemDefinition> Items => LoadItems();

        public static List<LevelDefinition> Curve(string file)
        {
            var levels = new List<LevelDefinition>();
            foreach (var l in JArray.Parse(File.ReadAllText("Assets/Game/Content/Data/Levels/" + file)))
            {
                levels.Add(new LevelDefinition(
                    (long)l["seed"], (int)l["jps"], (int)l["vis"], l["sizes"].ToObject<int[]>(), (int)l["types"], (int)l["per"],
                    (int?)l["locks"] ?? 0, (int?)l["frz"] ?? 0, (int?)l["thaw"] ?? 3, (int?)l["brd"] ?? 0, (int?)l["hm"] ?? 0,
                    (float?)l["wrap"] ?? 0f, (float?)l["deep"] ?? 0f, (string)l["fam"] ?? string.Empty, (int?)l["look"] ?? 0,
                    (bool?)l["hard"] ?? false, (string)l["tip"] ?? string.Empty, (int?)l["cap"] ?? 0, (int?)l["bk"] ?? 0));
            }

            return levels;
        }

        private static List<ItemDefinition> LoadItems() =>
            JArray.Parse(File.ReadAllText("Assets/Game/Content/Data/Items/items.json"))
                .Select(it => new ItemDefinition((int)it["id"], (string)it["name"], (string)it["family"], (string)it["shape"],
                    (string)it["silhouette"], (string)it["colorBucket"], null))
                .OrderBy(i => i.Id)
                .ToList();
    }
}
