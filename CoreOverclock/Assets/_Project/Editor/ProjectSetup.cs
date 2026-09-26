using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CoreOverclock.EditorTools
{
    /// <summary>
    /// One-click (or batchmode) setup: layers, data assets, the Arena scene and build settings.
    /// Safe to re-run; existing data assets are kept so designer tweaks survive.
    /// </summary>
    public static class ProjectSetup
    {
        const string Root = "Assets/_Project";
        const string ScenePath = Root + "/Scenes/Arena.unity";
        const string BuildPath = "Builds/Windows/CoreOverclock.exe";
        const string DemoBuildPath = "Builds/Demo/CoreOverclockDemo.exe";
        const string Version = "0.4.0";

        [MenuItem("Core Overclock/Setup Project")]
        public static void Setup()
        {
            // Opening a new Single scene unloads unreferenced assets, so do it before loading any data.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SetupLayers();
            foreach (var dir in new[] { "Data/Weapons", "Data/Chips", "Data/Enemies", "Data/Waves", "Data/Player", "Data/Shop", "Materials", "Scenes" })
                Directory.CreateDirectory(Path.Combine(Root, dir));
            AssetDatabase.Refresh();

            var material = CreateMaterial();
            var player = LoadOrCreate<PlayerData>($"{Root}/Data/Player/PlayerData.asset", _ => { });
            var weapons = DefaultContent.Weapons(Root);
            var blaster = weapons[0];
            var shopDb = LoadOrCreate<ShopDatabase>($"{Root}/Data/Shop/ShopDatabase.asset", _ => { });
            shopDb.weapons = weapons;
            shopDb.chips = DefaultContent.Chips(Root);
            EditorUtility.SetDirty(shopDb);

            var enemies = DefaultContent.Enemies(Root);
            var waves = DefaultContent.Upsert<WaveTable>($"{Root}/Data/Waves/WaveTable.asset", t => DefaultContent.FillWaves(t, enemies));

            PopulateScene(scene, material, player, blaster, waves, shopDb);
            ConfigurePlayer();
            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] Core Overclock setup complete.");
        }

        [MenuItem("Core Overclock/Build Windows")]
        public static void BuildWindows() => Build(BuildPath, null);

        /// <summary>Next Fest demo: CORE_DEMO limits the run to waves 1-10 and shows a wishlist ending.</summary>
        [MenuItem("Core Overclock/Build Demo (Windows)")]
        public static void BuildDemo() => Build(DemoBuildPath, new[] { "CORE_DEMO" });

        static void Build(string path, string[] defines)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = path,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
                extraScriptingDefines = defines,
            });
            // Lets Steamworks initialise outside the Steam client (480 = Valve's Spacewar test app).
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(path), "steam_appid.txt"), "480");
            Debug.Log($"[Build] {path} {report.summary.result} errors={report.summary.totalErrors} size={report.summary.totalSize}");
            if (Application.isBatchMode && report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                EditorApplication.Exit(1);
        }

        /// <summary>Batchmode entry: setup then build.</summary>
        public static void SetupAndBuild()
        {
            Setup();
            BuildWindows();
            BuildDemo();
        }

        static void SetupLayers()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tagManager.FindProperty("layers");
            string[] names = { "Player", "Enemy", "Wall", "Obstacle" };
            for (int i = 0; i < names.Length; i++) layers.GetArrayElementAtIndex(6 + i).stringValue = names[i];
            tagManager.ApplyModifiedPropertiesWithoutUndo();
        }

        static Material CreateMaterial()
        {
            string path = $"{Root}/Materials/M_SpriteUnlit.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat) return mat;
            var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            mat = new Material(shader) { name = "M_SpriteUnlit" };
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        internal static T LoadOrCreate<T>(string path, System.Action<T> init) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            init(asset);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static void PopulateScene(Scene scene, Material material, PlayerData player, WeaponData weapon, WaveTable waves, ShopDatabase shopDb)
        {

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 9f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Palette.Background;
            camGo.transform.position = new Vector3(0f, 0f, -10f);
            camGo.AddComponent<CameraShake>();

            var gmGo = new GameObject("GameManager");
            var gm = gmGo.AddComponent<GameManager>();
            var so = new SerializedObject(gm);
            so.FindProperty("spriteMaterial").objectReferenceValue = material;
            so.FindProperty("playerData").objectReferenceValue = player;
            so.FindProperty("startingWeapon").objectReferenceValue = weapon;
            so.FindProperty("waveTable").objectReferenceValue = waves;
            so.FindProperty("shopDatabase").objectReferenceValue = shopDb;
            so.ApplyModifiedPropertiesWithoutUndo();
            if (!player || !weapon || !waves || so.FindProperty("playerData").objectReferenceValue == null)
                Debug.LogError("[Setup] GameManager data references are missing - re-run setup.");

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "CoreOverclock";
            PlayerSettings.productName = "Core Overclock";
            PlayerSettings.bundleVersion = Version;
            // Release default is borderless fullscreen; smoke tests pass -screen-fullscreen 0.
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
        }
    }
}
