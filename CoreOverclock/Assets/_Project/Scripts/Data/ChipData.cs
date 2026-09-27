using System;
using System.Collections.Generic;
using UnityEngine;

namespace CoreOverclock
{
    /// <summary>Serialized as ints: append only.</summary>
    public enum StatType
    {
        MaxHP, MoveSpeedPct, DamagePct, FireRatePct, HeatGenPct, CoolingFlat, PickupRange, CritChance,
        RegenPerSec, ArmorPct, VentCooldownPct, RangePct, OverclockDamagePct, ScrapGainPct,
        ExplosionRadiusPct, BurnDamagePct, KnockbackPct, MeltdownDurationPct,
    }

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
            StatType.RegenPerSec => $"HP 재생 {Signed(value)}/s",
            StatType.ArmorPct => $"받는 피해 {Pct(-value)}",
            StatType.VentCooldownPct => $"방열 쿨타임 {Pct(value)}",
            StatType.RangePct => $"사거리 {Pct(value)}",
            StatType.OverclockDamagePct => $"오버클럭 공격력 {Pct(value)}",
            StatType.ScrapGainPct => $"스크랩 획득 {Pct(value)}",
            StatType.ExplosionRadiusPct => $"폭발 범위 {Pct(value)}",
            StatType.BurnDamagePct => $"도트 피해 {Pct(value)}",
            StatType.KnockbackPct => $"넉백 {Pct(value)}",
            StatType.MeltdownDurationPct => $"과열 락다운 시간 {Pct(value)}",
            _ => stat.ToString(),
        };

        static string Signed(float v) => (v >= 0 ? "+" : "") + v.ToString("0.#");
        /// <summary>True when a positive value is bad for the player (shown in red).</summary>
        public bool IsDrawback => stat switch
        {
            StatType.HeatGenPct or StatType.VentCooldownPct or StatType.MeltdownDurationPct => value > 0f,
            _ => value < 0f,
        };

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
        [Tooltip("코어 파편 cost to unlock in the 연구소; 0 = available from the start")] [Min(0)] public int unlockCost;
        public List<StatModifier> modifiers = new();
    }
}
