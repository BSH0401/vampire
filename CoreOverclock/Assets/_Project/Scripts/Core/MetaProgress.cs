using System;
using System.Collections.Generic;
using UnityEngine;

namespace CoreOverclock
{
    /// <summary>Serialized by index in PlayerPrefs: append only.</summary>
    public enum MetaUpgradeId { Armor, Funding, Cooling, Vent, Reroll, Shelf }

    public class MetaUpgradeDef
    {
        public MetaUpgradeId Id;
        public string Name;
        public Func<int, string> Describe; // total effect at a given level
        public int[] Costs;
    }

    /// <summary>
    /// 영구 성장 (기획서 코어 루프): 코어 파편 earned per run, spent in the 연구소 on permanent upgrades,
    /// weapon/chip unlocks and the starting weapon. Stored in PlayerPrefs; automated test runs never save.
    /// </summary>
    public static class MetaProgress
    {
        public const string Currency = "◈";
        const string Prefix = "meta_";
        const int DemoMaxLevel = 3;
        public const int BaseShopSlots = 4;

        public static readonly MetaUpgradeDef[] Upgrades =
        {
            new() { Id = MetaUpgradeId.Armor, Name = "강화 외장", Describe = l => $"최대 HP +{l * 5}", Costs = new[] { 15, 25, 40, 60, 85 } },
            new() { Id = MetaUpgradeId.Funding, Name = "초기 자금", Describe = l => $"시작 스크랩 +{l * 8}", Costs = new[] { 15, 25, 40, 60, 85 } },
            new() { Id = MetaUpgradeId.Cooling, Name = "냉각 효율", Describe = l => $"기본 냉각 +{l}/s", Costs = new[] { 15, 25, 40, 60, 85 } },
            new() { Id = MetaUpgradeId.Vent, Name = "방열 밸브", Describe = l => $"방열 쿨타임 -{l * 6}%", Costs = new[] { 15, 25, 40, 60, 85 } },
            new() { Id = MetaUpgradeId.Reroll, Name = "시장 연줄", Describe = l => $"리롤 비용 -{l}", Costs = new[] { 20, 35, 55 } },
            new() { Id = MetaUpgradeId.Shelf, Name = "확장 진열대", Describe = l => $"상점 진열 +{l}칸", Costs = new[] { 120 } },
        };

        static bool loaded;
        static int fragments;
        static readonly int[] levels = new int[Enum.GetValues(typeof(MetaUpgradeId)).Length];
        static readonly HashSet<string> unlocked = new();
        static readonly HashSet<FusionId> discovered = new();
        static readonly HashSet<FusionId> unseen = new(); // discovered but not yet viewed in the 도감
        static string startWeapon;
        static bool devUnlockAll;

        public static event Action Changed;

        public static int Fragments { get { Load(); return fragments; } }

        static void Load()
        {
            if (loaded) return;
            loaded = true;
            fragments = PlayerPrefs.GetInt(Prefix + "fragments", 0);
            for (int i = 0; i < levels.Length; i++) levels[i] = PlayerPrefs.GetInt(Prefix + "up_" + i, 0);
            unlocked.Clear();
            foreach (var id in PlayerPrefs.GetString(Prefix + "unlocked", "").Split(',', StringSplitOptions.RemoveEmptyEntries)) unlocked.Add(id);
            discovered.Clear();
            foreach (var name in PlayerPrefs.GetString(Prefix + "fusions", "").Split(',', StringSplitOptions.RemoveEmptyEntries))
                if (Enum.TryParse(name, out FusionId f)) discovered.Add(f);
            unseen.Clear();
            foreach (var name in PlayerPrefs.GetString(Prefix + "fusions_new", "").Split(',', StringSplitOptions.RemoveEmptyEntries))
                if (Enum.TryParse(name, out FusionId f)) unseen.Add(f);
            startWeapon = PlayerPrefs.GetString(Prefix + "start_weapon", "");

            // -meta N: balance sweeps with N upgrade levels and everything unlocked (never saved).
            if (DevCommandLine.Enabled && DevCommandLine.MetaLevel >= 0)
            {
                for (int i = 0; i < levels.Length; i++) levels[i] = Mathf.Min(DevCommandLine.MetaLevel, MaxLevel(Upgrades[i]));
                devUnlockAll = true;
            }
        }

        static void Save()
        {
            Changed?.Invoke();
            if (DevCommandLine.Enabled) return; // automated runs must not touch real progress
            PlayerPrefs.SetInt(Prefix + "fragments", fragments);
            for (int i = 0; i < levels.Length; i++) PlayerPrefs.SetInt(Prefix + "up_" + i, levels[i]);
            PlayerPrefs.SetString(Prefix + "unlocked", string.Join(",", unlocked));
            var names = new List<string>();
            foreach (var f in discovered) names.Add(f.ToString());
            PlayerPrefs.SetString(Prefix + "fusions", string.Join(",", names));
            names.Clear();
            foreach (var f in unseen) names.Add(f.ToString());
            PlayerPrefs.SetString(Prefix + "fusions_new", string.Join(",", names));
            PlayerPrefs.SetString(Prefix + "start_weapon", startWeapon ?? "");
            PlayerPrefs.Save();
        }

        // ─────────────── Upgrades ───────────────

        public static int Level(MetaUpgradeId id) { Load(); return levels[(int)id]; }

        public static int MaxLevel(MetaUpgradeDef def) => BuildFlavor.IsDemo ? Mathf.Min(DemoMaxLevel, def.Costs.Length) : def.Costs.Length;

        /// <returns>Cost of the next level, or -1 when maxed.</returns>
        public static int NextCost(MetaUpgradeDef def)
        {
            int l = Level(def.Id);
            return l < MaxLevel(def) ? def.Costs[l] : -1;
        }

        public static bool TryUpgrade(MetaUpgradeDef def)
        {
            int cost = NextCost(def);
            if (cost < 0 || fragments < cost) return false;
            fragments -= cost;
            levels[(int)def.Id]++;
            Save();
            return true;
        }

        public static void ApplyUpgrades(PlayerStats stats)
        {
            stats.Add(StatType.MaxHP, Level(MetaUpgradeId.Armor) * 5f);
            stats.Add(StatType.CoolingFlat, Level(MetaUpgradeId.Cooling));
            stats.Add(StatType.VentCooldownPct, Level(MetaUpgradeId.Vent) * -0.06f);
        }

        public static int StartScrapBonus => Level(MetaUpgradeId.Funding) * 8;
        public static int RerollDiscount => Level(MetaUpgradeId.Reroll);
        public static int ShopSlots => BaseShopSlots + Level(MetaUpgradeId.Shelf);

        // ─────────────── Unlocks ───────────────

        public static bool IsUnlocked(string id, int unlockCost)
        {
            Load();
            return unlockCost <= 0 || devUnlockAll || unlocked.Contains(id);
        }

        /// <summary>The demo only lets players unlock tier 1-2 items.</summary>
        public static bool CanUnlockInThisBuild(int tier) => !BuildFlavor.IsDemo || tier <= 2;

        public static bool TryUnlock(string id, int cost, int tier)
        {
            if (IsUnlocked(id, cost) || !CanUnlockInThisBuild(tier) || fragments < cost) return false;
            fragments -= cost;
            unlocked.Add(id);
            Save();
            return true;
        }

        public static bool IsUnlocked(WeaponData w) => w && IsUnlocked(w.id, w.unlockCost);
        public static bool IsUnlocked(ChipData c) => c && IsUnlocked(c.id, c.unlockCost);

        public static string StartWeaponId
        {
            get { Load(); return startWeapon; }
            set { Load(); startWeapon = value; Save(); }
        }

        // ─────────────── Fusion codex ───────────────

        /// <returns>true the first time a fusion is ever completed.</returns>
        public static bool Discover(FusionId id)
        {
            Load();
            if (!discovered.Add(id)) return false;
            unseen.Add(id);
            Save();
            return true;
        }

        public static bool IsDiscovered(FusionId id) { Load(); return discovered.Contains(id); }
        public static int DiscoveredCount { get { Load(); return discovered.Count; } }
        public static bool IsUnseen(FusionId id) { Load(); return unseen.Contains(id); }
        public static bool HasUnseen { get { Load(); return unseen.Count > 0; } }

        /// <summary>Clears the NEW badges once the player has looked at the 도감.</summary>
        public static void MarkCodexSeen()
        {
            Load();
            if (unseen.Count == 0) return;
            unseen.Clear();
            Save();
        }

        // ─────────────── Run rewards ───────────────

        public static int CalculateReward(int wave, int bossKills, int kills, bool victory) =>
            wave * 2 + bossKills * 10 + kills / 40 + (victory ? 20 : 0);

        public static void AddFragments(int amount)
        {
            Load();
            fragments += Mathf.Max(0, amount);
            Save();
        }

        public static void ResetAll()
        {
            Load();
            fragments = 0;
            Array.Clear(levels, 0, levels.Length);
            unlocked.Clear();
            discovered.Clear();
            unseen.Clear();
            startWeapon = "";
            Save();
        }
    }
}
