using System;
using System.IO;
using UnityEngine;

namespace CoreOverclock
{
    /// <summary>
    /// Command-line switches for automated smoke tests of builds:
    ///   -autopilot            player circles the arena
    ///   -god                  player takes no damage
    ///   -wavetime N           override wave duration
    ///   -startwave N          begin at wave N
    ///   -shots DIR            save screenshots to DIR at the times in -shottimes (default "5,12")
    ///   -quitafter N          quit after N real seconds
    /// </summary>
    public class DevCommandLine : MonoBehaviour
    {
        public static bool Enabled, Autopilot, God;
        public static float WaveTimeOverride = -1f;
        public static int StartWave = 1;
        static string shotDir;
        static float[] shotTimes = { 5f, 12f };
        static float quitAfter = -1f;

        int nextShot;
        float intermissionTime;

        public static void Parse()
        {
            Enabled = Autopilot = God = false;
            WaveTimeOverride = -1f;
            StartWave = 1;
            shotDir = null;
            quitAfter = -1f;

            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                string next = i + 1 < args.Length ? args[i + 1] : null;
                switch (args[i])
                {
                    case "-autopilot": Autopilot = Enabled = true; break;
                    case "-god": God = true; break;
                    case "-wavetime": WaveTimeOverride = ParseFloat(next, -1f); Enabled = true; break;
                    case "-startwave": StartWave = (int)ParseFloat(next, 1f); break;
                    case "-shots": shotDir = next; Enabled = true; break;
                    case "-shottimes":
                        if (next != null) shotTimes = Array.ConvertAll(next.Split(','), s => ParseFloat(s, 0f));
                        break;
                    case "-quitafter": quitAfter = ParseFloat(next, -1f); Enabled = true; break;
                }
            }
        }

        static float ParseFloat(string s, float fallback) =>
            float.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : fallback;

        public static Vector2 AutopilotMove()
        {
            var gm = GameManager.Instance;
            if (!gm || !gm.Player) return Vector2.zero;
            float a = Time.time * 0.5f;
            var target = new Vector2(Mathf.Cos(a) * 7f, Mathf.Sin(a) * 4f);
            var to = target - gm.Player.Position;
            return to.sqrMagnitude > 0.04f ? to.normalized : Vector2.zero;
        }

        void Update()
        {
            float t = Time.realtimeSinceStartup;
            if (shotDir != null && nextShot < shotTimes.Length && t >= shotTimes[nextShot])
            {
                Directory.CreateDirectory(shotDir);
                ScreenCapture.CaptureScreenshot(Path.Combine(shotDir, $"shot_{nextShot}.png"));
                Debug.Log($"[Dev] Screenshot {nextShot} at {t:F1}s, state={GameManager.Instance.State}, enemies={Enemy.Active.Count}, scrap={GameManager.Instance.Scrap}");
                nextShot++;
            }

            // Autopilot presses "next wave" by itself.
            var gm = GameManager.Instance;
            intermissionTime = gm.State == GameState.Intermission ? intermissionTime + Time.unscaledDeltaTime : 0f;
            if (Autopilot && intermissionTime > 3f) gm.NextWave();

            if (quitAfter > 0f && t >= quitAfter)
            {
                Debug.Log($"[Dev] Quit. wave={gm.Wave} state={gm.State} kills={gm.TotalKills} scrap={gm.Scrap} hp={gm.Player.HP}");
                Application.Quit();
            }
        }
    }
}
