using System.Collections.Generic;
using UnityEngine;

namespace CoreOverclock
{
    public class MarketOffer
    {
        /// <summary>A forbidden chip, or null for an illegal weapon modification.</summary>
        public ChipData Chip;
        /// <summary>불법 개조 target: raised one grade at the cost of max HP.</summary>
        public OwnedWeapon Target;
        public int Price;
        public bool Sold;

        public bool IsMod => Chip == null;
    }

    /// <summary>
    /// 암시장: a smuggler sometimes shows up in the 코어 작업실 with forbidden parts —
    /// strong chips with real drawbacks, and cheap grade upgrades that cost max HP.
    /// </summary>
    public static class BlackMarket
    {
        public const int FirstWave = 4;           // never before the shop that leads into wave 4
        public const float Chance = 0.3f;
        public const int PityVisits = 4;          // guaranteed after this many shops without it
        public const int OfferCount = 3;
        public const float ModPriceRatio = 0.45f; // of the normal upgrade cost
        public const float ModHpCost = 4f;

        static List<ChipData> catalog;

        /// <summary>Forbidden chips, built at runtime (they are never sold in the normal shop).</summary>
        public static IReadOnlyList<ChipData> Catalog => catalog ??= new List<ChipData>
        {
            Make("bm_illegal_oc", "불법 오버클럭 모듈", 30, "출력 제한을 해제한다.", (StatType.DamagePct, 0.45f), (StatType.HeatGenPct, 0.3f)),
            Make("bm_reactor", "개조 원자로", 32, "안전 장치는 떼어냈다.", (StatType.FireRatePct, 0.35f), (StatType.MaxHP, -12f)),
            Make("bm_coolant", "밀수 냉각제", 26, "출처를 묻지 마라.", (StatType.CoolingFlat, 10f), (StatType.DamagePct, -0.1f)),
            Make("bm_collector", "해킹된 수집기", 24, "고철을 긁어모으느라 방어가 허술해진다.", (StatType.ScrapGainPct, 0.6f), (StatType.ArmorPct, -0.2f)),
            Make("bm_armor", "폭발 반응 장갑", 28, "튼튼하지만 무겁다.", (StatType.MaxHP, 30f), (StatType.MoveSpeedPct, -0.15f)),
            Make("bm_lens", "블랙박스 렌즈", 30, "급소만 노린다. 멀리는 못 본다.", (StatType.CritChance, 0.25f), (StatType.RangePct, -0.2f)),
            Make("bm_coil", "폭주 코일", 30, "오버클럭에 모든 것을. 녹으면 오래 간다.", (StatType.OverclockDamagePct, 0.8f), (StatType.MeltdownDurationPct, 0.5f)),
            Make("bm_warhead", "불안정 탄두", 26, "가끔 내 쪽으로도 튄다.", (StatType.ExplosionRadiusPct, 0.4f), (StatType.BurnDamagePct, 0.4f), (StatType.ArmorPct, -0.1f)),
        };

        static ChipData Make(string id, string name, int price, string desc, params (StatType stat, float value)[] mods)
        {
            var c = ScriptableObject.CreateInstance<ChipData>();
            c.hideFlags = HideFlags.HideAndDontSave; // survives scene reloads (UnloadUnusedAssets)
            c.name = id;
            c.id = id;
            c.displayName = name;
            c.tier = 3;
            c.price = price;
            c.description = desc;
            foreach (var (stat, value) in mods) c.modifiers.Add(new StatModifier { stat = stat, value = value });
            return c;
        }

        public static bool IsForbidden(ChipData c) => c && c.id != null && c.id.StartsWith("bm_");
    }
}
