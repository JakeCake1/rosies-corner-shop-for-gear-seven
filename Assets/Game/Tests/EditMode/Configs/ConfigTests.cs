using System.Collections.Generic;
using System.Linq;
using Game.Services.Content;
using Game.Services.Levels;
using NUnit.Framework;
using UnityEditor;

namespace Game.Tests.EditMode.Configs
{
    /// <summary>
    /// Runs <see cref="IValidatableConfig.Validate"/> over every config asset of each type.
    /// </summary>
    public sealed class ConfigTests
    {
        private static IEnumerable<TestCaseData> Assets<T>() where T : UnityEngine.Object =>
            AssetDatabase.FindAssets("t:" + typeof(T).Name)
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(p => new TestCaseData(p).SetName($"{typeof(T).Name}_{System.IO.Path.GetFileNameWithoutExtension(p)}_IsValid"));

        private static IEnumerable<TestCaseData> Catalogs() => Assets<ItemCatalog>();

        private static IEnumerable<TestCaseData> Curves() => Assets<LevelCurveConfig>();

        [TestCaseSource(nameof(Catalogs))]
        public void ItemCatalog_IsValid(string path) => AssertValid(path);

        [TestCaseSource(nameof(Curves))]
        public void LevelCurve_IsValid(string path) => AssertValid(path);

        [Test]
        public void Configs_Exist()
        {
            Assert.That(AssetDatabase.FindAssets("t:ItemCatalog"), Is.Not.Empty, "no ItemCatalog asset; run Tools/Rosie/Build Project");
            Assert.That(AssetDatabase.FindAssets("t:LevelCurveConfig").Length, Is.EqualTo(2), "expected the New 20 and Original 10 curves");
        }

        [Test]
        public void ItemCatalog_HasAllPrototypeItems()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ItemCatalog>(
                AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("t:ItemCatalog").First()));

            Assert.That(catalog.Items, Has.Count.EqualTo(162));
            Assert.That(catalog.Families, Has.Count.EqualTo(7));
        }

        private static void AssertValid(string path)
        {
            var asset = (IValidatableConfig)AssetDatabase.LoadMainAssetAtPath(path);
            var errors = new List<string>();

            asset.Validate(errors);

            Assert.That(errors, Is.Empty, string.Join("\n", errors));
        }
    }
}
