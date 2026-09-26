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

        [MenuItem("Core Overclock/Setup Project (Phase 1)")]
        public static void Setup()
        {
            // Opening a new Single scene unloads unreferenced assets, so do it before loading any data.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SetupLayers();
            foreach (var dir in new[] { "Data/Weapons", "Data/Enemies", "Data/Waves", "Data/Player", "Materials", "Scenes" })
                Directory.CreateDirectory(Path.Combine(Root, dir));
            AssetDatabase.Refresh();

            var material = CreateMaterial();
            var player = LoadOrCreate<PlayerData>($"{Root}/Data/Player/PlayerData.asset", _ => { });
            var blaster = LoadOrCreate<WeaponData>($"{Root}/Data/Weapons/W_Blaster.asset", w =>
            {
                w.id = "blaster";
                w.displayName = "펄스 블래스터";
                w.description = "기본 탄도 무기. 가장 가까운 적에게 단발 탄환을 발사한다.";
                w.tag = WeaponTag.Ballistic;
                w.price = 15;
                w.damage = 7f;
                w.fireInterval = 0.5f;
                w.range = 7.5f;
                w.projectileSpeed = 17f;
                w.projectileRadius = 0.13f;
                w.knockback = 3f;
                w.critChance = 0.1f;
                w.critMultiplier = 2f;
                w.projectileColor = new Color(1f, 0.85f, 0.35f);
            });
            var scrapBit = LoadOrCreate<EnemyData>($"{Root}/Data/Enemies/E_ScrapBit.asset", e =>
            {
                e.id = "scrap_bit";
                e.displayName = "스크랩 비트";
                e.shape = EnemyShape.Triangle;
                e.behaviour = EnemyBehaviour.Chaser;
                e.color = new Color(0.92f, 0.95f, 1f);
                e.scale = 0.7f;
                e.maxHP = 10f;
                e.moveSpeed = 2.6f;
                e.contactDamage = 3f;
                e.scrapDrop = 1;
            });
            var waves = LoadOrCreate<WaveTable>($"{Root}/Data/Waves/WaveTable.asset", t => FillWaves(t, scrapBit));

            PopulateScene(scene, material, player, blaster, waves);
            ConfigurePlayer();
            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] Core Overclock Phase 1 setup complete.");
        }

        [MenuItem("Core Overclock/Build Windows")]
        public static void BuildWindows()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(BuildPath));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = BuildPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            });
            Debug.Log($"[Build] {report.summary.result} errors={report.summary.totalErrors} size={report.summary.totalSize}");
            if (Application.isBatchMode && report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                EditorApplication.Exit(1);
        }

        /// <summary>Batchmode entry: setup then build.</summary>
        public static void SetupAndBuild()
        {
            Setup();
            BuildWindows();
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

        static T LoadOrCreate<T>(string path, System.Action<T> init) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            init(asset);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        /// <summary>Wave durations follow 기획서 4.3; spawn pressure ramps linearly (tuned in Phase 3).</summary>
        static void FillWaves(WaveTable table, EnemyData enemy)
        {
            table.waves.Clear();
            for (int w = 1; w <= 20; w++)
            {
                float duration = w switch
                {
                    <= 5 => 30f,
                    15 => 60f,
                    20 => 90f,
                    _ => 45f,
                };
                table.waves.Add(new WaveDefinition
                {
                    duration = duration,
                    spawnInterval = Mathf.Max(0.3f, 1f - 0.035f * (w - 1)),
                    spawnPerTick = 1 + w / 3,
                    hpMultiplier = 1f + 0.25f * (w - 1),
                    speedMultiplier = 1f + 0.02f * (w - 1),
                    enemies = { new WaveSpawnEntry { enemy = enemy, weight = 1f } },
                });
            }
        }

        static void PopulateScene(Scene scene, Material material, PlayerData player, WeaponData weapon, WaveTable waves)
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
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
        }
    }
}
