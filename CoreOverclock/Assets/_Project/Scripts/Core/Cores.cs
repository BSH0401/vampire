using System.Collections.Generic;
using UnityEngine;

namespace CoreOverclock
{
    /// <summary>Serialized by name in PlayerPrefs.</summary>
    public enum CoreId { Standard, Furnace, Collector, Twin, Glacier, Glass }

    /// <summary>A playable core (캐릭터): passive stats plus one or two rule changes.</summary>
    public class CoreDef
    {
        public CoreId Id;
        public string Name, Tagline;
        public string[] Perks;      // "+..." lines are shown as upsides, "-..." lines as drawbacks
        public int UnlockCost;
        public bool InDemo = true;
        public Color Color;
        public (StatType stat, float value)[] Mods = { };

        public float OverclockThreshold = 70f;
        public int WeaponSlots = Player.MaxWeapons;
        public float ShopPriceMultiplier = 1f;
        public float UpgradeCostMultiplier = 1f;
        public int StartWeaponCopies = 1;

        public string UnlockKey => "core_" + Id;
    }

    /// <summary>시작 코어: chosen before each run on the 코어 선택 screen, unlocked with 코어 파편.</summary>
    public static class Cores
    {
        public static readonly CoreDef[] All =
        {
            new()
            {
                Id = CoreId.Standard, Name = "표준 코어", Tagline = "균형 잡힌 기본형 코어", Color = Palette.Player,
                Perks = new[] { "특별한 장단점 없음", "처음 플레이할 때 추천" },
            },
            new()
            {
                Id = CoreId.Furnace, Name = "용광로 코어", Tagline = "뜨거울수록 강해진다", Color = new Color(1f, 0.5f, 0.15f), UnlockCost = 40,
                OverclockThreshold = 60f, Mods = new[] { (StatType.OverclockDamagePct, 0.3f), (StatType.CoolingFlat, -1f) },
                Perks = new[] { "+오버클럭 진입 70% → 60%", "+오버클럭 공격력 +30%", "-냉각 -1/s" },
            },
            new()
            {
                Id = CoreId.Collector, Name = "수집가 코어", Tagline = "고철이 곧 힘이다", Color = Palette.Scrap, UnlockCost = 40,
                ShopPriceMultiplier = 0.9f, WeaponSlots = 5, Mods = new[] { (StatType.ScrapGainPct, 0.35f) },
                Perks = new[] { "+스크랩 획득 +35%", "+상점 가격 -10%", "-무기 슬롯 6 → 5" },
            },
            new()
            {
                Id = CoreId.Glacier, Name = "빙하 코어", Tagline = "절대 녹지 않는 안전 제일", Color = new Color(0.55f, 0.9f, 1f), UnlockCost = 30,
                Mods = new[] { (StatType.CoolingFlat, 3f), (StatType.HeatGenPct, -0.15f), (StatType.DamagePct, -0.1f) },
                Perks = new[] { "+냉각 +3/s", "+발열량 -15%", "-공격력 -10%" },
            },
            new()
            {
                Id = CoreId.Twin, Name = "쌍둥이 코어", Tagline = "둘이 하나보다 낫다", Color = new Color(1f, 0.45f, 0.75f), UnlockCost = 60, InDemo = false,
                StartWeaponCopies = 2, UpgradeCostMultiplier = 0.7f, Mods = new[] { (StatType.MaxHP, -5f) },
                Perks = new[] { "+시작 무기 2개 (트윈 링크 즉시 발동)", "+무기 강화 비용 -30%", "-최대 HP -5" },
            },
            new()
            {
                Id = CoreId.Glass, Name = "유리 코어", Tagline = "맞기 전에 부순다", Color = new Color(0.85f, 0.9f, 1f), UnlockCost = 60, InDemo = false,
                Mods = new[] { (StatType.DamagePct, 0.35f), (StatType.CritChance, 0.1f), (StatType.MaxHP, -12f) },
                Perks = new[] { "+공격력 +35%", "+치명타 확률 +10%", "-최대 HP -12" },
            },
        };

        public static CoreDef Get(CoreId id)
        {
            foreach (var c in All) if (c.Id == id) return c;
            return All[0];
        }

        /// <summary>The core of the current run (the -core test flag wins over the saved choice).</summary>
        public static CoreDef Current => Get(DevCommandLine.CoreOverride ?? MetaProgress.SelectedCore);

        public static void Apply(PlayerStats stats)
        {
            foreach (var (stat, value) in Current.Mods) stats.Add(stat, value);
        }

        public static bool CanUnlockInThisBuild(CoreDef c) => !BuildFlavor.IsDemo || c.InDemo;
    }
}
