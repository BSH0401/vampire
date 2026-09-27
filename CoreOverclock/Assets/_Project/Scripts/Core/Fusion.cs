using System.Collections.Generic;

namespace CoreOverclock
{
    /// <summary>Serialized by name in PlayerPrefs (codex): rename with care.</summary>
    public enum FusionId { ThermalShock, EmpRound, BulletHell, AbsoluteZero, MoltenShrapnel, FrostShot, PrismBarrage, TwinLink }

    public class FusionDef
    {
        public FusionId Id;
        public string Name;
        /// <summary>Weapon ids; both null for 트윈 링크 (any two copies of the same weapon).</summary>
        public string WeaponA, WeaponB;
        public string Recipe, Effect;
    }

    /// <summary>
    /// 퓨전: specific weapon pairs unlock a special effect on top of the tag set bonuses.
    /// Effects are applied in <see cref="Weapon"/> (shot params) and <see cref="ProjectileSystem"/> (on hit / explode).
    /// </summary>
    public static class Fusions
    {
        // Tuning knobs (balance later).
        public const float ThermalShockBonus = 0.4f;     // energy damage vs slowed
        public const int EmpChains = 3;
        public const float EmpDamageRatio = 0.5f;
        public const float EmpRange = 4f;
        public const float BulletHellRadius = 0.9f;      // minigun crit explosion
        public const float BulletHellDamageRatio = 0.8f;
        public const float AbsoluteZeroRamp = 0.5f;      // railgun damage gain per slowed enemy pierced
        public const float MoltenBurnDps = 5f;
        public const float MoltenBurnDuration = 2f;
        public const float FrostSlow = 0.4f;
        public const float FrostSlowDuration = 1.2f;
        public const int PrismBomblets = 3;
        public const float PrismBombletRatio = 0.4f;
        public const float TwinDamage = 0.15f;
        public const float TwinHeat = 0.1f;

        public static readonly FusionDef[] All =
        {
            Def(FusionId.ThermalShock, "열충격", "laser", "cryo_shard", "펄스 레이저 + 크라이오 샤드",
                $"둔화된 적이 레이저 피해를 {ThermalShockBonus * 100f:0}% 더 받는다"),
            Def(FusionId.EmpRound, "EMP 탄", "grenade", "arc_bolt", "그레네이드 런처 + 아크 볼트",
                $"폭발이 주변 적 {EmpChains}명에게 전기로 연쇄된다"),
            Def(FusionId.BulletHell, "탄막 지옥", "minigun", "rocket_pod", "미니건 + 로켓 포드",
                "미니건 치명타가 소형 폭발을 일으킨다"),
            Def(FusionId.AbsoluteZero, "절대영도", "railgun", "glacier", "레일건 + 글레이셔 캐논",
                $"레일건이 둔화된 적을 꿰뚫을 때마다 피해 +{AbsoluteZeroRamp * 100f:0}%"),
            Def(FusionId.MoltenShrapnel, "용융 파편", "plasma_lance", "flak", "플라즈마 랜스 + 플랙 캐논",
                "플랙 파편에 화상 부여"),
            Def(FusionId.FrostShot, "서리 산탄", "scattergun", "frost_needler", "스캐터건 + 프로스트 니들러",
                $"스캐터건 산탄이 적을 {FrostSlow * 100f:0}% 둔화"),
            Def(FusionId.PrismBarrage, "굴절 포격", "prism", "cluster_mortar", "프리즘 빔 + 클러스터 박격포",
                $"박격포 폭발 후 자탄 {PrismBomblets}발이 추가로 터진다"),
            Def(FusionId.TwinLink, "트윈 링크", null, null, "같은 무기 2개",
                $"해당 무기 피해 +{TwinDamage * 100f:0}%, 발열 -{TwinHeat * 100f:0}%"),
        };

        static FusionDef Def(FusionId id, string name, string a, string b, string recipe, string effect) =>
            new() { Id = id, Name = name, WeaponA = a, WeaponB = b, Recipe = recipe, Effect = effect };

        public static FusionDef Get(FusionId id) => All[(int)id];

        /// <summary>Fusions active for the given list of weapon ids.</summary>
        public static void Evaluate(IReadOnlyList<string> weaponIds, HashSet<FusionId> result)
        {
            result.Clear();
            foreach (var f in All)
            {
                if (f.WeaponA == null)
                {
                    if (HasDuplicate(weaponIds)) result.Add(f.Id);
                }
                else if (Contains(weaponIds, f.WeaponA) && Contains(weaponIds, f.WeaponB)) result.Add(f.Id);
            }
        }

        static bool Contains(IReadOnlyList<string> ids, string id)
        {
            for (int i = 0; i < ids.Count; i++) if (ids[i] == id) return true;
            return false;
        }

        static bool HasDuplicate(IReadOnlyList<string> ids)
        {
            for (int i = 0; i < ids.Count; i++)
            for (int j = i + 1; j < ids.Count; j++)
                if (ids[i] == ids[j]) return true;
            return false;
        }
    }
}
