using UnityEngine;

namespace CoreOverclock
{
    /// <summary>Resolved per-shot values after chips, synergies and heat state.</summary>
    public struct ShotParams
    {
        public WeaponData Weapon;
        public float Damage, Speed, Knockback, CritChance, Lifetime;
        public int Pierce, Bounces;
        public float BurnDps, BurnDuration, ExplosionRadius;
        public float SlowAmount, SlowDuration;
        public bool Overclocked;

        // 퓨전 effects (0 = off)
        public float SlowedDamageBonus;   // 열충격: extra damage vs slowed enemies
        public float SlowedPierceRamp;    // 절대영도: damage gain per slowed enemy pierced
        public float CritExplosionRadius; // 탄막 지옥
        public int ChainLightning;        // EMP 탄
        public int Bomblets;              // 굴절 포격
    }

    /// <summary>Auto-aiming weapon mounted around the player (기획서 4.1: 100% 자동 조작).</summary>
    public class Weapon : MonoBehaviour
    {
        const float MuzzleOffset = 0.35f;
        static readonly Color LockedColor = new(0.35f, 0.35f, 0.4f);

        Transform pivot;
        Transform barrel;
        SpriteRenderer barrelRenderer, mountRenderer;
        Color color;
        float cooldown;
        float recoil;

        public WeaponData Data { get; private set; }

        public static Weapon Create(WeaponData data, Transform parent)
        {
            var go = new GameObject($"Weapon_{data.id}");
            go.transform.SetParent(parent, false);
            var w = go.AddComponent<Weapon>();
            w.Data = data;

            w.color = WeaponTags.ColorOf(data.tag);
            w.pivot = new GameObject("Pivot").transform;
            w.pivot.SetParent(go.transform, false);
            w.barrelRenderer = Visuals.Sprite("Barrel", w.pivot, ShapeSprites.White, w.color, 22);
            w.barrelRenderer.transform.localScale = new Vector3(0.42f, 0.14f, 1f);
            w.barrelRenderer.transform.localPosition = new Vector3(0.14f, 0f, 0f);
            w.barrel = w.barrelRenderer.transform;
            w.mountRenderer = Visuals.Sprite("Mount", go.transform, ShapeSprites.Circle, w.color, 23, 0.22f);
            w.cooldown = Random.Range(0f, data.fireInterval);
            return w;
        }

        void Update()
        {
            var gm = GameManager.Instance;
            if (!gm || gm.State != GameState.Combat) return;
            var heat = gm.Heat;
            bool enabled = heat.WeaponsEnabled;

            var c = enabled ? (heat.State == HeatState.Overclock ? Color.Lerp(color, Palette.Overclock, 0.5f) : color) : LockedColor;
            barrelRenderer.color = c;
            mountRenderer.color = c;

            recoil = Mathf.MoveTowards(recoil, 0f, Time.deltaTime * 2f);
            barrel.localPosition = new Vector3(0.14f - recoil, 0f, 0f);
            cooldown -= Time.deltaTime;

            Vector2 origin = transform.position;
            var target = Targeting.FindNearest(origin, Data.range * (1f + gm.Loadout.Stats.RangePct));
            if (target == null) return;

            Vector2 dir = (target.Position - origin).normalized;
            pivot.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);

            if (!enabled || cooldown > 0f) return;
            var loadout = gm.Loadout;
            float rate = 1f + loadout.Stats.FireRatePct + (Data.tag == WeaponTag.Energy ? loadout.Synergy.EnergyFireRateBonus : 0f);
            cooldown = Data.fireInterval / Mathf.Max(0.2f, rate);
            Fire(origin + dir * MuzzleOffset, dir, BuildShot(loadout, heat));
            heat.AddShotHeat(Data.heatPerShot * (loadout.IsTwin(Data) ? 1f - Fusions.TwinHeat : 1f));
        }

        ShotParams BuildShot(Loadout loadout, HeatSystem heat)
        {
            var stats = loadout.Stats;
            var syn = loadout.Synergy;
            bool ballistic = Data.tag == WeaponTag.Ballistic;
            float dmgMul = (1f + stats.DamagePct) * heat.DamageMultiplier * (loadout.IsTwin(Data) ? 1f + Fusions.TwinDamage : 1f);
            var shot = new ShotParams
            {
                Weapon = Data,
                Damage = Data.damage * dmgMul * (Data.explosionRadius > 0f ? syn.ExplosionDamageMultiplier : 1f),
                Speed = Data.projectileSpeed * heat.ProjectileSpeedMultiplier,
                Knockback = Data.knockback * (ballistic ? syn.KnockbackMultiplier : 1f) * Mathf.Max(0f, 1f + stats.KnockbackPct),
                Lifetime = Data.projectileLifetime * (1f + stats.RangePct),
                CritChance = Data.critChance + stats.CritChance,
                Pierce = Data.pierce + (ballistic ? syn.BonusPierce : 0),
                Bounces = syn.WallBounces,
                BurnDps = Data.burnDamagePerSecond * syn.BurnMultiplier * (1f + stats.DamagePct + stats.BurnDamagePct),
                BurnDuration = Data.burnDuration,
                SlowAmount = Data.slowAmount,
                SlowDuration = Data.slowDuration,
                ExplosionRadius = Data.explosionRadius * syn.ExplosionRadiusMultiplier * (1f + stats.ExplosionRadiusPct),
                Overclocked = heat.State == HeatState.Overclock,
            };
            ApplyFusions(loadout, ref shot);
            return shot;
        }

        void ApplyFusions(Loadout loadout, ref ShotParams shot)
        {
            if (loadout.ActiveFusions.Count == 0) return;
            var stats = loadout.Stats;
            if (Data.tag == WeaponTag.Energy && loadout.Has(FusionId.ThermalShock)) shot.SlowedDamageBonus = Fusions.ThermalShockBonus;
            switch (Data.id)
            {
                case "grenade" when loadout.Has(FusionId.EmpRound):
                    shot.ChainLightning = Fusions.EmpChains;
                    break;
                case "minigun" when loadout.Has(FusionId.BulletHell):
                    shot.CritExplosionRadius = Fusions.BulletHellRadius * (1f + stats.ExplosionRadiusPct);
                    break;
                case "railgun" when loadout.Has(FusionId.AbsoluteZero):
                    shot.SlowedPierceRamp = Fusions.AbsoluteZeroRamp;
                    break;
                case "flak" when loadout.Has(FusionId.MoltenShrapnel):
                    shot.BurnDps = Fusions.MoltenBurnDps * loadout.Synergy.BurnMultiplier * (1f + stats.DamagePct + stats.BurnDamagePct);
                    shot.BurnDuration = Fusions.MoltenBurnDuration;
                    break;
                case "scattergun" when loadout.Has(FusionId.FrostShot):
                    shot.SlowAmount = Fusions.FrostSlow;
                    shot.SlowDuration = Fusions.FrostSlowDuration;
                    break;
                case "cluster_mortar" when loadout.Has(FusionId.PrismBarrage):
                    shot.Bomblets = Fusions.PrismBomblets;
                    break;
            }
        }

        void Fire(Vector2 muzzle, Vector2 dir, in ShotParams shot)
        {
            int count = Data.projectileCount;
            float baseAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            for (int i = 0; i < count; i++)
            {
                float offset = count == 1 ? 0f : Mathf.Lerp(-Data.spreadAngle, Data.spreadAngle, i / (count - 1f));
                offset += Random.Range(-2f, 2f);
                float a = (baseAngle + offset) * Mathf.Deg2Rad;
                bool crit = Random.value < shot.CritChance;
                ProjectileSystem.Spawn(shot, muzzle, new Vector2(Mathf.Cos(a), Mathf.Sin(a)), crit);
            }
            recoil = 0.08f;
            AudioManager.Play(Data.tag switch
            {
                WeaponTag.Energy => SfxId.ShootEnergy,
                WeaponTag.Cryo => SfxId.ShootCryo,
                WeaponTag.Explosive => SfxId.ShootExplosive,
                _ => SfxId.ShootBallistic,
            }, 0.28f);
        }
    }

    public static class Targeting
    {
        /// <summary>Nearest enemy in range; data towers are only targeted when no enemy is in range.</summary>
        public static IDamageable FindNearest(Vector2 from, float range)
        {
            IDamageable best = null;
            float bestSqr = range * range;
            foreach (var e in Enemy.Active)
            {
                if (!e.IsAlive) continue;
                float d = (e.Position - from).sqrMagnitude;
                if (d < bestSqr) { bestSqr = d; best = e; }
            }
            if (best != null) return best;

            foreach (var t in DataTower.Active)
            {
                if (!t.IsAlive) continue;
                float d = (t.Position - from).sqrMagnitude;
                if (d < bestSqr) { bestSqr = d; best = t; }
            }
            return best;
        }
    }
}
