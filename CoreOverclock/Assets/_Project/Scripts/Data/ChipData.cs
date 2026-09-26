using System;
using System.Collections.Generic;
using UnityEngine;

namespace CoreOverclock
{
    public enum StatType { MaxHP, MoveSpeedPct, DamagePct, FireRatePct, HeatGenPct, CoolingFlat, PickupRange, CritChance }

    [Serializable]
    public class StatModifier
    {
        public StatType stat;
        public float value;

        public string Describe() => stat switch
        {
            StatType.MaxHP => $"최대 HP {Signed(value)}",
            StatType.MoveSpeedPct => $"이동 속도 {Pct(value)}",
            StatType.DamagePct => $"공격력 {Pct(value)}",
            StatType.FireRatePct => $"연사력 {Pct(value)}",
            StatType.HeatGenPct => $"발열량 {Pct(value)}",
            StatType.CoolingFlat => $"냉각 {Signed(value)}/s",
            StatType.PickupRange => $"회수 범위 {Signed(value)}",
            StatType.CritChance => $"치명타 확률 {Pct(value)}",
            _ => stat.ToString(),
        };

        static string Signed(float v) => (v >= 0 ? "+" : "") + v.ToString("0.#");
        static string Pct(float v) => (v >= 0 ? "+" : "") + Mathf.RoundToInt(v * 100f) + "%";
    }

    /// <summary>Passive chipset bought in the shop (기획서 6.2).</summary>
    [CreateAssetMenu(menuName = "Core Overclock/Chip Data", fileName = "C_NewChip")]
    public class ChipData : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        [Min(1)] public int tier = 1;
        [Min(0)] public int price = 15;
        public List<StatModifier> modifiers = new();
    }
}
