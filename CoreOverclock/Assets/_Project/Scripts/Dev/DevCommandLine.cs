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
    ///   -scrap N              start with N scrap
    ///   -autobuy              autopilot buys whatever it can afford in the shop
    ///   -novent               autopilot never uses Vent Out
    ///   -title                stay on the title screen (for screenshots)
    ///   -timescale N          run the simulation N times faster (balance sweeps)
    /// </summary>
    public class DevCommandLine : MonoBehaviour
    {
        public static bool Enabled, Autopilot, God;
        public static float WaveTimeOverride = -1f;
        public static int StartWave = 1, StartScrap;
        public static bool AutoBuy, NoVent, ShowTitle;
        public static float TimeScale = 1f;
        static string shotDir;
        static float[] shotTimes = { 5f, 12f };
        static float quitAfter = -1f;

        int nextShot;
        float intermissionTime;

        public static void Parse()
        {
            Enabled = Autopilot = God = AutoBuy = NoVent = ShowTitle = false;
            StartScrap = 0;
            TimeScale = 1f;
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
                    case "-scrap": StartScrap = (int)ParseFloat(next, 0f); break;
                    case "-autobuy": AutoBuy = true; Enabled = true; break;
                    case "-novent": NoVent = true; break;
                    case "-title": ShowTitle = true; break;
                    case "-timescale": TimeScale = Mathf.Clamp(ParseFloat(next, 1f), 0.1f, 8f); break;
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

        /// <summary>
        /// Crude "human-like" kiting for balance sweeps: orbit the arena while being pushed away
        /// from nearby enemies and pulled away from the walls.
        /// </summary>
        public static Vector2 AutopilotMove()
        {
            var gm = GameManager.Instance;
            if (!gm || !gm.Player) return Vector2.zero;
            Vector2 pos = gm.Player.Position;
            float a = Time.time * 0.5f;
            var orbit = new Vector2(Mathf.Cos(a) * 7f, Mathf.Sin(a) * 4f) - pos;
            Vector2 steer = orbit.sqrMagnitude > 0.04f ? orbit.normalized * 0.6f : Vector2.zero;

            foreach (var e in Enemy.Active)
            {
                if (!e.IsAlive) continue;
                Vector2 away = pos - e.Position;
                float d = away.magnitude;
                float danger = 3.5f + e.Data.scale;
                if (d < danger && d > 0.01f) steer += away / d * ((danger - d) / danger) * 2.5f;
            }

            var h = Arena.HalfSize;
            steer.x -= Mathf.Clamp01((pos.x - (h.x - 2f)) / 2f) * 3f - Mathf.Clamp01((-h.x + 2f - pos.x) / 2f) * 3f;
            steer.y -= Mathf.Clamp01((pos.y - (h.y - 2f)) / 2f) * 3f - Mathf.Clamp01((-h.y + 2f - pos.y) / 2f) * 3f;
            return steer.sqrMagnitude > 0.01f ? steer.normalized : Vector2.zero;
        }

        void Update()
        {
            float t = Time.realtimeSinceStartup;
            if (shotDir != null && nextShot < shotTimes.Length && t >= shotTimes[nextShot])
            {
                Directory.CreateDirectory(shotDir);
                ScreenCapture.CaptureScreenshot(Path.Combine(shotDir, $"shot_{nextShot}.png"));
                var g = GameManager.Instance;
                Debug.Log($"[Dev] Screenshot {nextShot} at {t:F1}s, wave={g.Wave} state={g.State}, enemies={Enemy.Active.Count}, scrap={g.Scrap}, heat={g.Heat.Value:F0} ({g.Heat.State}), weapons={g.Loadout.Weapons.Count}, heatMul={g.Loadout.HeatGenMultiplier:F2}");
                nextShot++;
            }

            // Autopilot presses "next wave" by itself.
            var gm = GameManager.Instance;
            float prev = intermissionTime;
            intermissionTime = gm.State == GameState.Intermission ? intermissionTime + Time.unscaledDeltaTime : 0f;
            if (AutoBuy && gm.State == GameState.Intermission && Mathf.Floor(prev * 4f) != Mathf.Floor(intermissionTime * 4f))
            {
                // Weapons first until the slots are full, then chips; reroll while it can afford to.
                bool bought = false;
                for (int pass = 0; pass < 2 && !bought; pass++)
                for (int i = 0; i < Shop.SlotCount && !bought; i++)
                    if ((pass == 1 || gm.Shop.Offers[i].IsWeapon) && gm.Shop.Buy(i)) bought = true;
                if (!bought && gm.Scrap > gm.Shop.RerollCost * 4) gm.Shop.Reroll();
            }
            if (Autopilot && intermissionTime > 3f) gm.NextWave();

            if (quitAfter > 0f && t >= quitAfter)
            {
                Debug.Log($"[Dev] Quit. wave={gm.Wave} state={gm.State} kills={gm.TotalKills} scrap={gm.Scrap} hp={gm.Player.HP}");
                Application.Quit();
            }
        }
    }
}
