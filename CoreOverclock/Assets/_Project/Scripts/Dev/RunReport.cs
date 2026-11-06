using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CoreOverclock
{
    /// <summary>
    /// -report DIR: writes one JSON summary per automated run (balance sweeps, see Tools/balance_run.ps1).
    /// Only game numbers go in the file — no paths, user names or hardware info — so it can be shared
    /// (e.g. copied into the wiki) while the raw Player.log stays on the machine.
    /// </summary>
    public static class RunReport
    {
        [Serializable]
        public class WaveRecord
        {
            public int wave;
            public float seconds;       // simulated wave length
            public int kills;
            public int scrapGained;     // pickups + clear bonus
            public float damageTaken;   // after armor, counted even in -god runs
            public float hpLeft, maxHp;
            public int weapons, chips;
            public bool boss, bossKilled;
            public int meltdowns;
        }

        [Serializable]
        public class WeaponRecord
        {
            public string id;
            public int grade;
        }

        [Serializable]
        public class Data
        {
            public int schema = 1;
            public string version, build, core, startWeapon;
            public int seed;
            public float timeScale;
            public bool god, autobuy;
            public int metaLevel;
            public string result;       // victory | death | quit
            public int waveReached, totalWaves, totalKills, bossKills, fragments, finalScrap;
            public int merges, upgrades, marketBuys, meltdowns;
            public float realSeconds;
            public List<WaveRecord> waves = new();
            public List<WeaponRecord> finalWeapons = new();
            public List<string> chips = new(), fusions = new(), market = new();
        }

        static Data data;
        static int waveMeltdowns;
        static float startRealtime;
        static bool finished;

        public static bool Active => DevCommandLine.ReportDir != null && data != null && !finished;

        public static void Begin(GameManager gm)
        {
            if (DevCommandLine.ReportDir == null || data != null) return;
            startRealtime = Time.realtimeSinceStartup;
            data = new Data
            {
                version = BuildFlavor.Version,
                build = BuildFlavor.IsDemo ? "demo" : "full",
                core = Cores.Current.Id.ToString(),
                startWeapon = gm.Loadout.Weapons.Count > 0 ? gm.Loadout.Weapons[0].Data.id : "",
                seed = DevCommandLine.Seed,
                timeScale = DevCommandLine.TimeScale,
                god = DevCommandLine.God,
                autobuy = DevCommandLine.AutoBuy,
                metaLevel = DevCommandLine.MetaLevel,
                totalWaves = gm.TotalWaves,
            };
        }

        public static void Count(string what, string detail = null)
        {
            if (!Active) return;
            switch (what)
            {
                case "merge": data.merges++; break;
                case "upgrade": data.upgrades++; break;
                case "market": data.marketBuys++; if (detail != null) data.market.Add(detail); break;
                case "meltdown": data.meltdowns++; waveMeltdowns++; break;
            }
        }

        public static void Wave(GameManager gm, int kills, int scrapGained, float seconds, bool died)
        {
            if (!Active) return;
            var def = gm.CurrentWave;
            data.waves.Add(new WaveRecord
            {
                wave = gm.Wave,
                seconds = seconds,
                kills = kills,
                scrapGained = scrapGained,
                damageTaken = gm.Player.DamageTakenThisWave,
                hpLeft = died ? 0f : gm.Player.HP,
                maxHp = gm.Player.MaxHP,
                weapons = gm.Loadout.Weapons.Count,
                chips = gm.Loadout.Chips.Count,
                boss = def != null && def.bosses.Count > 0,
                bossKilled = def != null && def.bosses.Count > 0 && gm.Boss == null,
                meltdowns = waveMeltdowns,
            });
            waveMeltdowns = 0;
        }

        public static void Finish(GameManager gm, string result, int bossKills, int fragments)
        {
            if (!Active) return;
            finished = true;
            data.result = result;
            data.waveReached = gm.Wave;
            data.totalKills = gm.TotalKills;
            data.bossKills = bossKills;
            data.fragments = fragments;
            data.finalScrap = gm.Scrap;
            data.realSeconds = Time.realtimeSinceStartup - startRealtime;
            foreach (var w in gm.Loadout.Weapons) data.finalWeapons.Add(new WeaponRecord { id = w.Data.id, grade = w.Grade });
            foreach (var c in gm.Loadout.Chips) data.chips.Add(c.id);
            foreach (var f in gm.Loadout.ActiveFusions) data.fusions.Add(f.ToString());

            try
            {
                Directory.CreateDirectory(DevCommandLine.ReportDir);
                string name = $"run_{DateTime.Now:yyyyMMdd_HHmmss}_{data.core}_{Guid.NewGuid().ToString("N").Substring(0, 6)}.json";
                string path = Path.Combine(DevCommandLine.ReportDir, name);
                File.WriteAllText(path, JsonUtility.ToJson(data, true));
                Debug.Log($"[Dev] Report written: {name} result={result} wave={data.waveReached}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Dev] Report failed: {e.Message}");
            }
        }
    }
}
