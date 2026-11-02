using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Bootstrap.Composition;
using Game.Features.Gameplay.Model;
using Game.Features.Gameplay.Session;
using Game.Services.Content;
using Game.Services.Levels;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Game.Bootstrap.Editor.Addressables
{
    /// <summary>
    /// Splits the item sprites into Addressables groups by level: <c>Items-Level-NN</c> holds the items that first
    /// appear in level NN of the default curve (played as a fresh profile, so the generator's "prefer unfound items"
    /// picks match a new player), <c>Items-Rest</c> holds the items no level of that curve uses. Also points the
    /// catalog's sprite references at the sprites.
    /// </summary>
    /// <remarks>
    /// Run it after changing the default curve, the catalog or the level generator. The grouping only decides which
    /// bundle an item downloads with; the game loads exactly the items a level generated, wherever they live.
    /// </remarks>
    public static class LevelItemGroups
    {
        private const string MenuPath = "Tools/Addressables/Rebuild Level Item Groups";
        private const string GroupPrefix = "Items-";
        private const string RestGroup = "Items-Rest";
        private const string TemplateGroup = "AlwaysLoaded";
        private const string AddressPrefix = "item/";
        private const string SpritesFolder = "Assets/Game/Content/Visual/Items";
        private const string ScopePrefab = "Assets/Game/Content/Prefabs/ProjectLifetimeScope.prefab";
        private const string CurvesField = "_curves";
        private const int IdDigits = 3;

        /// <summary>
        /// Recomputes the level item groups and the catalog sprite references.
        /// </summary>
        [MenuItem(MenuPath)]
        public static void Rebuild()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var catalog = LoadSingle<ItemCatalog>();
            var sprites = FindSprites();
            var firstLevel = FirstAppearances(catalog, LoadDefaultCurve());
            var groups = new Dictionary<string, AddressableAssetGroup>();
            foreach (var item in catalog.Items)
            {
                var name = firstLevel.TryGetValue(item.Id, out var level) ? $"{GroupPrefix}Level-{level:00}" : RestGroup;
                if (!groups.TryGetValue(name, out var group))
                {
                    group = groups[name] = GetOrCreateGroup(settings, name);
                }

                var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(sprites[item.Id]));
                var entry = settings.CreateOrMoveEntry(guid, group, false, false);
                entry.SetAddress(AddressPrefix + sprites[item.Id].name, false);
            }

            RemoveStaleGroups(settings, groups.Keys);
            SetCatalogReferences(catalog, sprites);
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.BatchModification, null, true, true);
            AssetDatabase.SaveAssets();
            Debug.Log($"[LevelItemGroups] {catalog.Items.Count} items in {groups.Count} groups " +
                $"({groups.Count(g => g.Key != RestGroup)} level groups).");
        }

        private static Dictionary<int, int> FirstAppearances(ItemCatalog catalog, LevelCurveConfig curve)
        {
            var generator = new LevelGenerator(catalog.Items, catalog.Families);
            var found = new HashSet<int>();
            var firstLevel = new Dictionary<int, int>();
            for (var n = 1; n <= curve.Levels.Count; n++)
            {
                var state = generator.Generate(n, curve.Levels[n - 1], new HashSet<int>(found));
                foreach (var type in LevelItemTypes.Of(state).Where(t => !firstLevel.ContainsKey(t)))
                {
                    firstLevel[type] = n;
                }

                // Completing a level ships, and so finds, every item it holds.
                found.UnionWith(LevelItemTypes.Of(state));
            }

            return firstLevel;
        }

        private static LevelCurveConfig LoadDefaultCurve()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<ProjectLifetimeScope>(ScopePrefab);
            var curves = new SerializedObject(prefab).FindProperty(CurvesField);
            if (curves == null || curves.arraySize == 0)
            {
                throw new InvalidDataException($"{ScopePrefab} has no level curves.");
            }

            return (LevelCurveConfig)curves.GetArrayElementAtIndex(0).objectReferenceValue;
        }

        private static Dictionary<int, Sprite> FindSprites()
        {
            var sprites = new Dictionary<int, Sprite>();
            foreach (var guid in AssetDatabase.FindAssets("t:Sprite", new[] { SpritesFolder }))
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guid));
                if (sprite == null || !int.TryParse(sprite.name.Substring(0, IdDigits), out var id))
                {
                    throw new InvalidDataException($"Item sprite '{sprite?.name}' doesn't start with a {IdDigits}-digit id.");
                }

                sprites.Add(id, sprite);
            }

            return sprites;
        }

        private static AddressableAssetGroup GetOrCreateGroup(AddressableAssetSettings settings, string name)
        {
            var group = settings.FindGroup(name);
            if (group != null)
            {
                return group;
            }

            var template = settings.FindGroup(TemplateGroup);
            return settings.CreateGroup(name, false, false, false, template.Schemas, template.SchemaTypes.ToArray());
        }

        private static void RemoveStaleGroups(AddressableAssetSettings settings, ICollection<string> used)
        {
            var stale = settings.groups.Where(g => g != null && g.Name.StartsWith(GroupPrefix) && !used.Contains(g.Name));
            foreach (var group in stale.ToList())
            {
                settings.RemoveGroup(group);
            }
        }

        private static void SetCatalogReferences(ItemCatalog catalog, Dictionary<int, Sprite> sprites)
        {
            var items = catalog.Items
                .Select(i => new ItemDefinition(i.Id, i.Name, i.Family, i.Shape, i.Silhouette, i.ColorBucket,
                    SpriteReference(sprites[i.Id])))
                .ToList();
            catalog.SetContent(items, catalog.Families.ToList());
            EditorUtility.SetDirty(catalog);
        }

        private static AssetReferenceSprite SpriteReference(Sprite sprite)
        {
            var reference = new AssetReferenceSprite(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(sprite)));
            reference.SetEditorSubObject(sprite);
            return reference;
        }

        private static T LoadSingle<T>() where T : Object
        {
            var guids = AssetDatabase.FindAssets("t:" + typeof(T).Name);
            if (guids.Length != 1)
            {
                throw new InvalidDataException($"Expected one {typeof(T).Name}, found {guids.Length}.");
            }

            return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }
    }
}
