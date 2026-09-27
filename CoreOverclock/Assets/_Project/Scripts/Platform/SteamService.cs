using System.Collections.Generic;
using UnityEngine;
#if STEAMWORKS_NET
using Steamworks;
#endif

namespace CoreOverclock
{
    /// <summary>Build flavor switches. The demo build defines CORE_DEMO (see ProjectSetup.BuildDemo).</summary>
    public static class BuildFlavor
    {
        // static readonly (not const) so non-demo code paths don't trigger unreachable-code warnings.
#if CORE_DEMO
        public static readonly bool IsDemo = true;
#else
        public static readonly bool IsDemo = false;
#endif
        /// <summary>Next Fest demo ends after this wave.</summary>
        public const int DemoLastWave = 10;
        public static string Version => Application.version;
    }

    /// <summary>Achievement API names (must match the Steamworks partner site).</summary>
    public static class Achievements
    {
        public const string FirstWave = "ACH_FIRST_WAVE";
        public const string Juggernaut = "ACH_BEAT_JUGGERNAUT";
        public const string Overseer = "ACH_BEAT_OVERSEER";
        public const string Escape = "ACH_ESCAPE";
        public const string DemoClear = "ACH_DEMO_CLEAR";
        public const string FullArsenal = "ACH_FULL_ARSENAL";
        public const string FirstMeltdown = "ACH_FIRST_MELTDOWN";
        public const string FirstFusion = "ACH_FIRST_FUSION";
        public const string FusionCodex = "ACH_FUSION_CODEX";

        public static readonly (string id, string name)[] All =
        {
            (FirstWave, "첫 출격"), (Juggernaut, "저거너트 격파"), (Overseer, "오버시어 격파"), (Escape, "탈출 성공"),
            (DemoClear, "데모 클리어"), (FullArsenal, "완전 무장"), (FirstMeltdown, "노심 용융"),
            (FirstFusion, "첫 퓨전"), (FusionCodex, "퓨전 도감 완성"),
        };
    }

    /// <summary>
    /// Thin Steam layer (기획서 Phase 4: Steam SDK 연동). Compiles without the SDK: achievements are
    /// always recorded locally, and forwarded to Steam when Steamworks.NET is installed and the
    /// scripting define STEAMWORKS_NET is set. The dev/demo builds ship steam_appid.txt = 480 (Spacewar test app).
    /// </summary>
    public static class SteamService
    {
        public const uint TestAppId = 480;
        const string LocalPrefix = "ach_";

        static bool initialized;
        public static bool Available { get; private set; }

        public static void Init()
        {
            if (initialized) return;
            initialized = true;
#if STEAMWORKS_NET
            try
            {
                Available = SteamAPI.Init();
                if (Available) SteamUserStats.RequestCurrentStats();
            }
            catch (System.DllNotFoundException e)
            {
                Debug.LogWarning($"[Steam] steam_api64.dll missing: {e.Message}");
                Available = false;
            }
#endif
            Debug.Log($"[Steam] available={Available} demo={BuildFlavor.IsDemo} version={BuildFlavor.Version}");
        }

        public static void Tick()
        {
#if STEAMWORKS_NET
            if (Available) SteamAPI.RunCallbacks();
#endif
        }

        public static void Shutdown()
        {
#if STEAMWORKS_NET
            if (Available) SteamAPI.Shutdown();
#endif
            Available = false;
            initialized = false;
        }

        public static bool IsUnlocked(string id) => PlayerPrefs.GetInt(LocalPrefix + id, 0) == 1;

        public static int UnlockedCount()
        {
            int n = 0;
            foreach (var (id, _) in Achievements.All) if (IsUnlocked(id)) n++;
            return n;
        }

        /// <returns>true when this call newly unlocked the achievement.</returns>
        public static bool Unlock(string id)
        {
            if (IsUnlocked(id)) return false;
            if (DevCommandLine.Enabled)
            {
                // Automated test runs must not award real achievements.
                Debug.Log($"[Steam] (dev run, not saved) Achievement: {id}");
                return false;
            }
            PlayerPrefs.SetInt(LocalPrefix + id, 1);
            PlayerPrefs.Save();
#if STEAMWORKS_NET
            if (Available)
            {
                SteamUserStats.SetAchievement(id);
                SteamUserStats.StoreStats();
            }
#endif
            Debug.Log($"[Steam] Achievement unlocked: {id}");
            return true;
        }

        public static string DisplayName(string id)
        {
            foreach (var (aid, name) in Achievements.All) if (aid == id) return name;
            return id;
        }
    }

    /// <summary>Pumps Steam callbacks and shuts the API down on quit.</summary>
    public class SteamRunner : MonoBehaviour
    {
        void Awake() => SteamService.Init();
        void Update() => SteamService.Tick();
        void OnApplicationQuit() => SteamService.Shutdown();
    }
}
