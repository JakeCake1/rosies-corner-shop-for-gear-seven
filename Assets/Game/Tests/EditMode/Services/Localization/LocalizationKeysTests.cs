using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Game.Features.Home;
using Game.Services.Content;
using Game.Services.Levels;
using Game.Services.Localization;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace Game.Tests.EditMode.Services.Localization
{
    /// <summary>
    /// Checks that every text key used by code and configs has an English entry, and that the tables hold no
    /// entries nothing uses. Code keys are the constants of the <c>*Texts</c> classes in the game assemblies.
    /// </summary>
    public sealed class LocalizationKeysTests
    {
        private const string English = "en";
        private const string TextsSuffix = "Texts";
        private const int MaxPlaceholders = 5;

        private static readonly Assembly[] GameAssemblies =
        {
            typeof(LocalizationService).Assembly, typeof(HomeViewModel).Assembly,
        };

        private static IEnumerable<TestCaseData> CodeKeys() =>
            CodeKeyFields().Select(f => new TestCaseData((string)f.GetRawConstantValue())
                .SetName($"CodeKey_{f.DeclaringType.Name}_{f.Name}_HasEnglishText"));

        private static IEnumerable<TestCaseData> UIEntries() =>
            Table(LocalizationService.UITable).Values.Select(e => new TestCaseData(e.Key)
                .SetName($"UIEntry_{e.Key}_FormatsWithArguments"));

        [Test]
        public void TextsClasses_Exist()
        {
            var classes = CodeKeyFields().Select(f => f.DeclaringType.Name).Distinct();

            Assert.That(classes, Is.SupersetOf(new[] { "AlbumTexts", "GameplayTexts", "HomeTexts", "DialogTexts" }));
        }

        [TestCaseSource(nameof(CodeKeys))]
        public void CodeKey_HasEnglishText(string key)
        {
            AssertHasText(LocalizationService.UITable, key);
        }

        [Test]
        public void CurveKeys_HaveEnglishText()
        {
            var missing = ConfigKeys().Where(k => !HasText(LocalizationService.UITable, k)).ToList();

            Assert.That(missing, Is.Empty, "level curve keys without English text: " + string.Join(", ", missing));
        }

        [Test]
        public void CatalogItemsAndDepartments_HaveEnglishNames()
        {
            var catalog = Catalog();
            var keys = catalog.Items.Select(i => LocalizationService.ItemKeyPrefix + i.Id)
                .Concat(catalog.Families.Select(f => LocalizationService.FamilyKeyPrefix + f));

            var missing = keys.Where(k => !HasText(LocalizationService.ItemsTable, k)).ToList();

            Assert.That(missing, Is.Empty, "Items table entries missing: " + string.Join(", ", missing));
        }

        [Test]
        public void UITable_HasOnlyUsedEntries()
        {
            var used = new HashSet<string>(CodeKeyFields().Select(f => (string)f.GetRawConstantValue())
                .Concat(ConfigKeys()));

            var unused = Keys(LocalizationService.UITable).Where(k => !used.Contains(k)).ToList();

            Assert.That(unused, Is.Empty, "UI table entries no code or config uses: " + string.Join(", ", unused));
        }

        [Test]
        public void ItemsTable_HasOnlyCatalogEntries()
        {
            var catalog = Catalog();
            var used = new HashSet<string>(catalog.Items.Select(i => LocalizationService.ItemKeyPrefix + i.Id)
                .Concat(catalog.Families.Select(f => LocalizationService.FamilyKeyPrefix + f)));

            var unused = Keys(LocalizationService.ItemsTable).Where(k => !used.Contains(k)).ToList();

            Assert.That(unused, Is.Empty, "Items table entries no catalog item uses: " + string.Join(", ", unused));
        }

        [TestCaseSource(nameof(UIEntries))]
        public void UIEntry_FormatsWithArguments(string key)
        {
            // Format entries are plain String.Format strings (not Smart Strings); stray braces would throw at runtime.
            var entry = Table(LocalizationService.UITable).GetEntry(key);
            var args = Enumerable.Range(0, MaxPlaceholders).Select(i => (object)i).ToArray();

            Assert.That(entry.IsSmart, Is.False, $"{key} is a Smart String; the service formats with String.Format.");
            Assert.That(() => string.Format(entry.Value, args), Throws.Nothing, key);
        }

        private static IEnumerable<FieldInfo> CodeKeyFields() =>
            GameAssemblies.SelectMany(a => a.GetTypes())
                .Where(t => t.IsAbstract && t.IsSealed && t.Name.EndsWith(TextsSuffix, StringComparison.Ordinal))
                .SelectMany(t => t.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                .Where(f => f.IsLiteral && f.FieldType == typeof(string));

        private static IEnumerable<string> ConfigKeys() =>
            AssetDatabase.FindAssets("t:" + nameof(LevelCurveConfig))
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<LevelCurveConfig>)
                .SelectMany(c => new[] { c.LabelKey, c.SubtitleKey }.Concat(c.Levels.Select(l => l.TipKey)))
                .Where(k => !string.IsNullOrEmpty(k))
                .Distinct();

        private static ItemCatalog Catalog() =>
            AssetDatabase.LoadAssetAtPath<ItemCatalog>(
                AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("t:" + nameof(ItemCatalog)).Single()));

        private static StringTable Table(string collection)
        {
            var tables = LocalizationEditorSettings.GetStringTableCollection(collection);
            Assert.That(tables, Is.Not.Null, $"no string table collection '{collection}'");
            var table = tables.GetTable(new LocaleIdentifier(English)) as StringTable;
            Assert.That(table, Is.Not.Null, $"'{collection}' has no English table");
            return table;
        }

        private static IEnumerable<string> Keys(string collection) => Table(collection).Values.Select(e => e.Key);

        private static bool HasText(string collection, string key)
        {
            var entry = Table(collection).GetEntry(key);
            return entry != null && !string.IsNullOrWhiteSpace(entry.Value);
        }

        private static void AssertHasText(string collection, string key) =>
            Assert.That(HasText(collection, key), Is.True, $"'{key}' has no English text in '{collection}'.");
    }
}
