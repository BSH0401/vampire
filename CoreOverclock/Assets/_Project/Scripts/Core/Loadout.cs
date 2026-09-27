using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CoreOverclock
{
    /// <summary>Summed passive-chip bonuses.</summary>
    public class PlayerStats
    {
        public float MaxHP, MoveSpeedPct, DamagePct, FireRatePct, HeatGenPct, CoolingFlat, PickupRange, CritChance;
        public float RegenPerSec, ArmorPct, VentCooldownPct, RangePct, OverclockDamagePct, ScrapGainPct;
        public float ExplosionRadiusPct, BurnDamagePct, KnockbackPct, MeltdownDurationPct;

        public void Add(StatType type, float v)
        {
            switch (type)
            {
                case StatType.MaxHP: MaxHP += v; break;
                case StatType.MoveSpeedPct: MoveSpeedPct += v; break;
                case StatType.DamagePct: DamagePct += v; break;
                case StatType.FireRatePct: FireRatePct += v; break;
                case StatType.HeatGenPct: HeatGenPct += v; break;
                case StatType.CoolingFlat: CoolingFlat += v; break;
                case StatType.PickupRange: PickupRange += v; break;
                case StatType.CritChance: CritChance += v; break;
                case StatType.RegenPerSec: RegenPerSec += v; break;
                case StatType.ArmorPct: ArmorPct += v; break;
                case StatType.VentCooldownPct: VentCooldownPct += v; break;
                case StatType.RangePct: RangePct += v; break;
                case StatType.OverclockDamagePct: OverclockDamagePct += v; break;
                case StatType.ScrapGainPct: ScrapGainPct += v; break;
                case StatType.ExplosionRadiusPct: ExplosionRadiusPct += v; break;
                case StatType.BurnDamagePct: BurnDamagePct += v; break;
                case StatType.KnockbackPct: KnockbackPct += v; break;
                case StatType.MeltdownDurationPct: MeltdownDurationPct += v; break;
            }
        }
    }

    /// <summary>
    /// Set bonuses by weapon tag count (기획서 6.1).
    /// Documented: 냉각 4 = 과열 락다운 면역, 탄도 6 = 벽 바운스 2회. The rest are Phase 2 proposals.
    /// </summary>
    public struct SynergyState
    {
        public int Ballistic, Energy, Cryo, Explosive;

        public int Count(WeaponTag tag) => tag switch
        {
            WeaponTag.Ballistic => Ballistic,
            WeaponTag.Energy => Energy,
            WeaponTag.Cryo => Cryo,
            _ => Explosive,
        };

        public int BonusPierce => Ballistic >= 2 ? 1 : 0;
        public float KnockbackMultiplier => Ballistic >= 4 ? 1.5f : 1f;
        public int WallBounces => Ballistic >= 6 ? 2 : 0;
        public float EnergyFireRateBonus => Energy >= 6 ? 0.25f : Energy >= 2 ? 0.1f : 0f;
        public float BurnMultiplier => Energy >= 4 ? 2f : 1f;
        /// <summary>Every Cryo weapon cools the core by 8%, plus 10% at 2 set.</summary>
        public float CryoHeatReduction => Cryo * 0.08f + (Cryo >= 2 ? 0.1f : 0f);
        public bool MeltdownImmune => Cryo >= 4;
        public float ExplosionRadiusMultiplier => Explosive >= 2 ? 1.25f : 1f;
        public float ExplosionDamageMultiplier => Explosive >= 4 ? 1.3f : 1f;

        public static (int count, string text)[] Tiers(WeaponTag tag) => tag switch
        {
            WeaponTag.Ballistic => new[] { (2, "관통 +1"), (4, "넉백 +50%"), (6, "벽 바운스 2회") },
            WeaponTag.Energy => new[] { (2, "연사 +10%"), (4, "도트 피해 x2"), (6, "연사 +25%") },
            WeaponTag.Cryo => new[] { (2, "발열 -10%"), (4, "과열 락다운 면역") },
            _ => new[] { (2, "폭발 범위 +25%"), (4, "폭발 피해 +30%") },
        };

        public string Describe(WeaponTag tag)
        {
            int n = Count(tag);
            var color = ColorUtility.ToHtmlStringRGB(WeaponTags.ColorOf(tag));
            var sb = new StringBuilder($"<color=#{color}>{WeaponTags.KoreanName(tag)} {n}</color>  ");
            foreach (var (count, text) in Tiers(tag))
                sb.Append(n >= count ? $"<color=#FFFFFF>[{count}] {text}</color>  " : $"<color=#5A6378>[{count}] {text}</color>  ");
            return sb.ToString();
        }
    }

    public class OwnedWeapon
    {
        public WeaponData Data;
        public int PaidPrice;
    }

    /// <summary>What the player has equipped: up to 6 weapons plus any number of chips.</summary>
    public class Loadout
    {
        public readonly List<OwnedWeapon> Weapons = new();
        public readonly List<ChipData> Chips = new();

        public PlayerStats Stats { get; private set; } = new();
        public SynergyState Synergy { get; private set; }
        /// <summary>Weapon-pair fusions currently active (see <see cref="Fusions"/>).</summary>
        public readonly HashSet<FusionId> ActiveFusions = new();
        public bool WeaponSlotsFull => Weapons.Count >= Player.MaxWeapons;

        public event Action Changed;
        /// <summary>Raised during Recalculate for each fusion that just became active.</summary>
        public event Action<FusionId> FusionActivated;

        readonly List<string> idScratch = new();
        readonly HashSet<FusionId> previousFusions = new();

        public bool Has(FusionId id) => ActiveFusions.Contains(id);

        /// <summary>트윈 링크 applies to weapons owned at least twice.</summary>
        public bool IsTwin(WeaponData data)
        {
            if (!Has(FusionId.TwinLink)) return false;
            int n = 0;
            foreach (var w in Weapons) if (w.Data == data && ++n >= 2) return true;
            return false;
        }

        /// <summary>Fusions that buying <paramref name="candidate"/> would newly complete (shop preview).</summary>
        public List<FusionId> FusionsCompletedBy(WeaponData candidate)
        {
            var result = new List<FusionId>();
            if (!candidate || WeaponSlotsFull) return result;
            idScratch.Clear();
            foreach (var w in Weapons) idScratch.Add(w.Data.id);
            idScratch.Add(candidate.id);
            var after = new HashSet<FusionId>();
            Fusions.Evaluate(idScratch, after);
            foreach (var f in after) if (!ActiveFusions.Contains(f)) result.Add(f);
            return result;
        }

        public bool AddWeapon(WeaponData data, int paid)
        {
            if (WeaponSlotsFull || !data) return false;
            Weapons.Add(new OwnedWeapon { Data = data, PaidPrice = paid });
            Recalculate();
            return true;
        }

        public void RemoveWeaponAt(int index)
        {
            Weapons.RemoveAt(index);
            Recalculate();
        }

        public void AddChip(ChipData chip)
        {
            Chips.Add(chip);
            Recalculate();
        }

        public void Recalculate()
        {
            var stats = new PlayerStats();
            MetaProgress.ApplyUpgrades(stats);
            foreach (var chip in Chips)
            foreach (var m in chip.modifiers)
                stats.Add(m.stat, m.value);
            Stats = stats;

            var syn = new SynergyState();
            foreach (var w in Weapons)
            {
                switch (w.Data.tag)
                {
                    case WeaponTag.Ballistic: syn.Ballistic++; break;
                    case WeaponTag.Energy: syn.Energy++; break;
                    case WeaponTag.Cryo: syn.Cryo++; break;
                    case WeaponTag.Explosive: syn.Explosive++; break;
                }
            }
            Synergy = syn;

            previousFusions.Clear();
            previousFusions.UnionWith(ActiveFusions);
            idScratch.Clear();
            foreach (var w in Weapons) idScratch.Add(w.Data.id);
            Fusions.Evaluate(idScratch, ActiveFusions);
            Changed?.Invoke();
            foreach (var f in ActiveFusions)
                if (!previousFusions.Contains(f)) FusionActivated?.Invoke(f);
        }

        /// <summary>Multiplier applied to all heat generated by shots.</summary>
        public float HeatGenMultiplier => Mathf.Max(0.2f, (1f + Stats.HeatGenPct) * (1f - Synergy.CryoHeatReduction));
    }
}
