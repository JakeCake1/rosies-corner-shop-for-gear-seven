using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Bootstrap.Composition;
using Game.Features.Gameplay;
using Game.Services.Content;
using Game.Services.Levels;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using VContainer.Unity;

namespace Game.Bootstrap.Editor.Builder
{
    /// <summary>
    /// Generates every asset that is normally assembled by hand in the editor: sprite import settings, TMP font
    /// assets, configs from the prototype's JSON data, prefabs with wired references, scenes, VContainer settings
    /// and Addressables groups. Re-running it rebuilds the generated assets in place.
    /// </summary>
    /// <remarks>
    /// Run from <c>Tools/Rosie/Build Project</c> or by the agent bridge (<c>invoke
    /// Game.Bootstrap.Editor.Builder.ProjectBuilder.BuildAll</c>). TMP Essential Resources must be imported first
    /// (<see cref="ImportTmpEssentials"/>) because importing a package finishes on the next editor tick.
    /// </remarks>
    public static class ProjectBuilder
    {
        public const string Root = "Assets/Game";
        public const string Generated = "Assets/Game/Bootstrap/Generated";
        public const string ScenesFolder = "Assets/Game/Bootstrap/Scenes";
        private const string ItemsFolder = "Assets/Game/Services/Content/Items";
        private const string KitFolder = "Assets/Game/Services/UI/Art/Kit";
        private const string FontsFolder = "Assets/Game/Services/UI/Fonts";
        private const string BootScene = ScenesFolder + "/Boot.unity";
        private const string HomeScene = ScenesFolder + "/Home.unity";
        private const string GameplayScene = ScenesFolder + "/Gameplay.unity";

        [MenuItem("Tools/Rosie/1. Import TMP Essentials")]
        public static string ImportTmpEssentials()
        {
            if (AssetDatabase.IsValidFolder("Assets/TextMesh Pro")) return "already imported";

            var package = Directory.GetFiles(Path.GetFullPath("Packages/com.unity.ugui"), "TMP Essential Resources.unitypackage",
                SearchOption.AllDirectories).FirstOrDefault();
            if (package == null) return "TMP Essential Resources.unitypackage not found";

            AssetDatabase.ImportPackage(package, false);
            return "importing " + package;
        }

        [MenuItem("Tools/Rosie/2. Build Project")]
        public static string BuildAll()
        {
            EnsureFolder(Generated);
            EnsureFolder(ScenesFolder);
            ConfigureImports();
            CreateFonts();
            CreateCatalog();
            var curves = CreateCurves();
            var art = CreateGameplayArt();
            AssetDatabase.SaveAssets();

            var prefabs = PrefabBuilder.BuildAll();
            var scope = CreateProjectScope(prefabs.UIRoot, curves);
            SetupVContainer(scope);
            BuildScenes(prefabs, art);
            SetupAddressables();
            ConfigurePlayer();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return "built";
        }

        /// <summary>
        /// Sprite import settings: kit sprites at 2x with 9-slice borders from slices.json, art at 4x.
        /// </summary>
        public static void ConfigureImports()
        {
            var slices = new Dictionary<string, int[]>();
            var slicesPath = KitFolder + "/slices.json";
            if (File.Exists(slicesPath))
            {
                foreach (var kv in JObject.Parse(File.ReadAllText(slicesPath)))
                {
                    slices[kv.Key] = kv.Value.ToObject<int[]>();
                }
            }

            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { Root });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) continue;

                var isKit = path.StartsWith(KitFolder);
                var name = Path.GetFileName(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.spritePixelsPerUnit = isKit ? 200f : 400f;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.wrapMode = name == "wallpaper.png" || name == "awning.png" || name == "wainscot.png" || name == "ledge.png"
                    ? TextureWrapMode.Repeat
                    : TextureWrapMode.Clamp;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
                importer.spriteBorder = isKit && slices.TryGetValue(name, out var b)
                    ? new Vector4(b[0], b[1], b[2], b[3])
                    : Vector4.zero;
                importer.SaveAndReimport();
            }
        }

        private static void CreateFonts()
        {
            UiFactory.Body = CreateFont(FontsFolder + "/Fredoka-SemiBold.ttf", Generated + "/Fredoka SDF.asset");
            UiFactory.Display = CreateFont(FontsFolder + "/YesevaOne-Regular.ttf", Generated + "/Yeseva SDF.asset");
        }

        private static TMP_FontAsset CreateFont(string ttf, string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (existing != null) return existing;

            var font = AssetDatabase.LoadAssetAtPath<Font>(ttf);
            var asset = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024,
                AtlasPopulationMode.Dynamic, true);
            AssetDatabase.CreateAsset(asset, path);
            asset.atlasTextures[0].name = asset.name + " Atlas";
            AssetDatabase.AddObjectToAsset(asset.atlasTextures[0], asset);
            asset.material.name = asset.name + " Material";
            AssetDatabase.AddObjectToAsset(asset.material, asset);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return asset;
        }

        private static ItemCatalog CreateCatalog()
        {
            var json = JArray.Parse(File.ReadAllText("Assets/Game/Services/Content/items.json"));
            var items = new List<ItemDefinition>();
            foreach (var it in json)
            {
                var file = (string)it["file"];
                var spritePath = ItemsFolder + "/" + (string)it["family"] + "/" + Path.GetFileNameWithoutExtension(file) + ".png";
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                if (sprite == null) Debug.LogError("[Builder] missing item sprite " + spritePath);
                items.Add(new ItemDefinition((int)it["id"], (string)it["name"], (string)it["family"], (string)it["shape"],
                    (string)it["silhouette"], (string)it["colorBucket"], sprite));
            }

            var catalog = LoadOrCreate<ItemCatalog>(Generated + "/ItemCatalog.asset");
            catalog.SetContent(items.OrderBy(i => i.Id).ToList(),
                new List<string> { "dairy", "bakery", "drink", "pantry", "sweet", "fresh", "home" },
                new List<string> { "Dairy", "Bakery", "Drinks", "Pantry", "Sweets", "Fresh produce", "Home goods" });
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static List<LevelCurveConfig> CreateCurves()
        {
            return new List<LevelCurveConfig>
            {
                CreateCurve("levels_new_20.json", "LevelCurve_New20", "new", "New · 20",
                    "Prototype · 20 levels · placeholder art", false),
                CreateCurve("levels_original_10.json", "LevelCurve_Original10", "orig", "Original · 10",
                    "Original prototype · 10 levels", true),
            };
        }

        private static LevelCurveConfig CreateCurve(string json, string asset, string id, string label, string subtitle, bool isOriginal)
        {
            var levels = new List<LevelDefinition>();
            foreach (var l in JArray.Parse(File.ReadAllText("Assets/Game/Services/Levels/" + json)))
            {
                levels.Add(new LevelDefinition(
                    (long)l["seed"], (int)l["jps"], (int)l["vis"], l["sizes"].ToObject<int[]>(), (int)l["types"], (int)l["per"],
                    (int?)l["locks"] ?? 0, (int?)l["frz"] ?? 0, (int?)l["thaw"] ?? 3, (int?)l["brd"] ?? 0, (int?)l["hm"] ?? 0,
                    (float?)l["wrap"] ?? 0f, (float?)l["deep"] ?? 0f, (string)l["fam"] ?? string.Empty, (int?)l["look"] ?? 0,
                    (bool?)l["hard"] ?? false, (string)l["tip"] ?? string.Empty, (int?)l["cap"] ?? 0, (int?)l["bk"] ?? 0));
            }

            var curve = LoadOrCreate<LevelCurveConfig>(Generated + "/" + asset + ".asset");
            curve.SetContent(id, label, subtitle, isOriginal, levels);
            EditorUtility.SetDirty(curve);
            return curve;
        }

        private static GameplayArt CreateGameplayArt()
        {
            var art = LoadOrCreate<GameplayArt>(Generated + "/GameplayArt.asset");
            art.SetSprites(UiFactory.Art("boosters/undo"), UiFactory.Art("boosters/pick"), UiFactory.Art("boosters/tidy"),
                UiFactory.Art("boosters/shuffle"), UiFactory.Art("basket/basket_full"));
            EditorUtility.SetDirty(art);
            return art;
        }

        private static ProjectLifetimeScope CreateProjectScope(GameObject uiRoot, List<LevelCurveConfig> curves)
        {
            var path = Generated + "/ProjectLifetimeScope.prefab";
            var go = new GameObject("ProjectLifetimeScope");
            var scope = go.AddComponent<ProjectLifetimeScope>();
            UiFactory.Wire(scope, ("_uiRootPrefab", uiRoot.GetComponent<Game.Services.UI.UIRoot>()));
            var so = new SerializedObject(scope);
            var list = so.FindProperty("_curves");
            list.arraySize = curves.Count;
            for (var i = 0; i < curves.Count; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = curves[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab.GetComponent<ProjectLifetimeScope>();
        }

        private static void SetupVContainer(ProjectLifetimeScope scope)
        {
            var path = Generated + "/VContainerSettings.asset";
            var settings = LoadOrCreate<VContainerSettings>(path);
            settings.RootLifetimeScope = scope;
            // VContainer otherwise rewrites any newly imported *LifetimeScope.cs with its empty template.
            settings.DisableScriptModifier = true;
            EditorUtility.SetDirty(settings);
            var preloaded = PlayerSettings.GetPreloadedAssets().Where(a => a != null && !(a is VContainerSettings)).ToList();
            preloaded.Add(settings);
            PlayerSettings.SetPreloadedAssets(preloaded.ToArray());
        }

        private static void BuildScenes(PrefabBuilder.Result prefabs, GameplayArt art)
        {
            var boot = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera();
            EditorSceneManager.SaveScene(boot, BootScene);

            var home = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera();
            EditorSceneManager.SaveScene(home, HomeScene);

            var gameplay = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera();
            // NewScene(Single) unloads unused assets, which can invalidate the instance loaded earlier.
            if (art == null) art = AssetDatabase.LoadAssetAtPath<GameplayArt>(Generated + "/GameplayArt.asset");
            BoardSceneBuilder.Build(prefabs, art);
            EditorSceneManager.SaveScene(gameplay, GameplayScene);

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(BootScene, true) };
            EditorSceneManager.OpenScene(BootScene, OpenSceneMode.Single);
        }

        private static void CreateCamera()
        {
            var cam = new GameObject("Main Camera", typeof(Camera)).GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = UiFactory.Hex("#2B1B0E");
            cam.orthographic = true;
            cam.tag = "MainCamera";
        }

        private static void SetupAddressables()
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            var always = GetGroup(settings, "AlwaysLoaded");
            var scenes = GetGroup(settings, "Scenes");
            Add(settings, always, Generated + "/ItemCatalog.asset", ItemCatalog.AddressKey);
            Add(settings, always, PrefabBuilder.Folder + "/DialogWindow.prefab", "ui/DialogWindow");
            Add(settings, always, PrefabBuilder.Folder + "/HomeWindow.prefab", "ui/HomeWindow");
            Add(settings, always, PrefabBuilder.Folder + "/AlbumWindow.prefab", "ui/AlbumWindow");
            Add(settings, scenes, HomeScene, "scene/Home");
            Add(settings, scenes, GameplayScene, "scene/Gameplay");
            EditorUtility.SetDirty(settings);
        }

        private static AddressableAssetGroup GetGroup(AddressableAssetSettings settings, string name)
        {
            var group = settings.FindGroup(name);
            if (group != null) return group;

            return settings.CreateGroup(name, false, false, true, null, typeof(BundledAssetGroupSchema),
                typeof(ContentUpdateGroupSchema));
        }

        private static void Add(AddressableAssetSettings settings, AddressableAssetGroup group, string path, string address)
        {
            var guid = AssetDatabase.AssetPathToGUID(path);
            var entry = string.IsNullOrEmpty(guid) ? null : settings.CreateOrMoveEntry(guid, group, false, false);
            if (entry == null)
            {
                Debug.LogError($"[Builder] can't make {path} addressable (guid '{guid}')");
                return;
            }

            entry.address = address;
        }

        private static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "Seven Gears";
            PlayerSettings.productName = "Rosie's Corner Shop";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.defaultScreenWidth = 390;
            PlayerSettings.defaultScreenHeight = 844;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
