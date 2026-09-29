using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CoreOverclock
{
    /// <summary>Pooled player projectiles. Hits are resolved with circle casts so fast bullets never tunnel.</summary>
    public class ProjectileSystem : MonoBehaviour
    {
        static ProjectileSystem instance;

        Pool<Projectile> pool;
        readonly List<Projectile> active = new();
        readonly List<RaycastHit2D> hits = new();
        readonly List<Collider2D> overlaps = new();
        ContactFilter2D filter;
        int clearVersion; // bumped by ClearAll so Update can bail if a hit ended the wave mid-loop
        public static int ClearVersion => instance ? instance.clearVersion : 0;

        public static ProjectileSystem Create(Transform parent)
        {
            var go = new GameObject("Projectiles");
            go.transform.SetParent(parent, false);
            var sys = go.AddComponent<ProjectileSystem>();
            instance = sys;
            sys.filter = new ContactFilter2D { useTriggers = false };
            sys.filter.SetLayerMask(GameLayers.HittableMask);
            sys.pool = new Pool<Projectile>(sys.CreateProjectile, 128);
            return sys;
        }

        Projectile CreateProjectile()
        {
            var go = new GameObject("Projectile");
            go.transform.SetParent(transform, false);
            var p = go.AddComponent<Projectile>();
            p.Glow = Visuals.Glow(go.transform, Color.white, 3f, 0.35f, 29);
            p.Body = Visuals.Sprite("Body", go.transform, ShapeSprites.Circle, Color.white, 30);
            return p;
        }

        public static void Spawn(in ShotParams shot, Vector2 position, Vector2 direction, bool crit)
        {
            if (!instance) return;
            var p = instance.pool.Get();
            p.Launch(shot, position, direction, crit);
            instance.active.Add(p);
        }

        public static void ClearAll()
        {
            if (!instance) return;
            for (int i = instance.active.Count - 1; i >= 0; i--) instance.pool.Release(instance.active[i]);
            instance.active.Clear();
            instance.clearVersion++;
        }

        /// <summary>Area damage for Explosive weapons.</summary>
        /// <param name="chain">EMP 탄: number of nearby enemies zapped afterwards.</param>
        /// <param name="bomblets">굴절 포격: delayed secondary explosions around the blast.</param>
        public static void Explode(Vector2 center, float radius, float damage, float knockback, bool crit, Color color,
            float slowAmount = 0f, float slowDuration = 0f, float burnDps = 0f, float burnDuration = 0f, int chain = 0, int bomblets = 0)
        {
            if (!instance) return;
            if (bomblets > 0)
                instance.StartCoroutine(instance.BombletRoutine(center, radius, damage * Fusions.PrismBombletRatio, knockback, color, bomblets));
            FxSystem.Pulse(center, color, 0.3f, radius * 2.4f, 0.3f);
            FxSystem.Pulse(center, Color.white, 0.2f, radius * 1.2f, 0.15f);
            CameraShake.Add(crit ? 0.08f : 0.04f, minor: true);
            AudioManager.Play(SfxId.Explosion, 0.4f, 0.1f);
            FxSystem.Burst(center, color, 8, radius * 5f, 0.12f, 0.35f);

            int n = Physics2D.OverlapCircle(center, radius, instance.filter, instance.overlaps);
            for (int i = 0; i < n; i++)
            {
                if (!instance.overlaps[i].TryGetComponent(out IDamageable target) || !target.IsAlive) continue;
                Vector2 to = target.Position - center;
                target.TakeDamage(new DamageInfo
                {
                    Amount = damage, Crit = crit, Knockback = knockback,
                    Direction = to.sqrMagnitude > 0.001f ? to.normalized : Vector2.up,
                    SlowAmount = slowAmount, SlowDuration = slowDuration, BurnDps = burnDps, BurnDuration = burnDuration,
                });
            }
            if (chain > 0) ChainLightning(center, chain, damage * Fusions.EmpDamageRatio);
        }

        static readonly List<Enemy> chainTargets = new();

        static void ChainLightning(Vector2 center, int count, float damage)
        {
            chainTargets.Clear();
            float rangeSqr = Fusions.EmpRange * Fusions.EmpRange;
            foreach (var e in Enemy.Active)
                if (e.IsAlive && (e.Position - center).sqrMagnitude < rangeSqr) chainTargets.Add(e);
            chainTargets.Sort((a, b) => (a.Position - center).sqrMagnitude.CompareTo((b.Position - center).sqrMagnitude));

            var color = WeaponTags.ColorOf(WeaponTag.Energy);
            Vector2 from = center;
            for (int i = 0; i < chainTargets.Count && i < count; i++)
            {
                var e = chainTargets[i];
                if (!e.IsAlive) continue;
                FxSystem.Bolt(from, e.Position, color);
                from = e.Position;
                e.TakeDamage(new DamageInfo { Amount = damage, Direction = Vector2.up, BurnDps = damage * 0.5f, BurnDuration = 1.5f });
            }
            if (chainTargets.Count > 0) AudioManager.Play(SfxId.ShootEnergy, 0.3f, 0.2f);
        }

        IEnumerator BombletRoutine(Vector2 center, float radius, float damage, float knockback, Color color, int count)
        {
            int version = clearVersion;
            for (int i = 0; i < count; i++)
            {
                yield return new WaitForSeconds(0.12f);
                var gm = GameManager.Instance;
                if (version != clearVersion || !gm || gm.State != GameState.Combat) yield break;
                Vector2 at = center + Random.insideUnitCircle.normalized * radius * Random.Range(0.7f, 1.2f);
                Explode(at, radius * 0.5f, damage, knockback * 0.5f, false, Color.Lerp(color, WeaponTags.ColorOf(WeaponTag.Energy), 0.5f));
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            int version = clearVersion;
            for (int i = active.Count - 1; i >= 0; i--)
            {
                bool alive = active[i].Step(dt, filter, hits);
                if (version != clearVersion) return;
                if (alive) continue;
                pool.Release(active[i]);
                active[i] = active[^1];
                active.RemoveAt(active.Count - 1);
            }
        }
    }

    public class Projectile : MonoBehaviour
    {
        public SpriteRenderer Body;
        public SpriteRenderer Glow;

        readonly HashSet<Collider2D> alreadyHit = new();
        ShotParams shot;
        Vector2 position, direction;
        float life, damage;
        int pierceLeft, bouncesLeft;
        bool crit;

        public void Launch(in ShotParams s, Vector2 pos, Vector2 dir, bool isCrit)
        {
            shot = s;
            position = pos;
            direction = dir;
            crit = isCrit;
            life = s.Lifetime > 0f ? s.Lifetime : s.Weapon.projectileLifetime;
            pierceLeft = s.Pierce;
            bouncesLeft = s.Bounces;
            damage = s.Damage * (isCrit ? s.Weapon.critMultiplier : 1f);
            alreadyHit.Clear();

            var baseColor = s.Overclocked ? Color.Lerp(s.Weapon.projectileColor, Palette.Overclock, 0.6f) : s.Weapon.projectileColor;
            Body.color = isCrit ? Color.white : baseColor;
            Glow.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.35f);
            float size = s.Weapon.projectileRadius * 2f * (isCrit ? 1.4f : 1f) * (s.Overclocked ? 1.2f : 1f);
            transform.localScale = Vector3.one * size;
            transform.SetPositionAndRotation(pos, Quaternion.identity);
        }

        /// <returns>false when the projectile should be released.</returns>
        public bool Step(float dt, ContactFilter2D filter, List<RaycastHit2D> hits)
        {
            var gm = GameManager.Instance;
            if (!gm || gm.State != GameState.Combat) return false;

            float distance = shot.Speed * dt;
            int n = Physics2D.CircleCast(position, shot.Weapon.projectileRadius, direction, filter, hits, distance);
            for (int i = 0; i < n; i++)
            {
                var col = hits[i].collider;
                if (!alreadyHit.Add(col)) continue;
                if (!col.TryGetComponent(out IDamageable target) || !target.IsAlive) continue;

                if (shot.ExplosionRadius > 0f)
                {
                    Explode(hits[i].centroid);
                    return false;
                }

                bool slowed = target is Enemy enemy && enemy.Slowed;
                int version = ProjectileSystem.ClearVersion;
                target.TakeDamage(new DamageInfo
                {
                    Amount = damage * (slowed ? 1f + shot.SlowedDamageBonus : 1f), Crit = crit, Direction = direction, Knockback = shot.Knockback,
                    SlowAmount = shot.SlowAmount, SlowDuration = shot.SlowDuration,
                    BurnDps = shot.BurnDps, BurnDuration = shot.BurnDuration,
                });
                if (version != ProjectileSystem.ClearVersion) return false; // that hit ended the wave
                if (slowed && shot.SlowedPierceRamp > 0f) damage *= 1f + shot.SlowedPierceRamp;
                if (crit && shot.CritExplosionRadius > 0f)
                {
                    ProjectileSystem.Explode(hits[i].point, shot.CritExplosionRadius, damage * Fusions.BulletHellDamageRatio, shot.Knockback, false,
                        WeaponTags.ColorOf(WeaponTag.Explosive));
                    if (version != ProjectileSystem.ClearVersion) return false;
                }
                if (--pierceLeft < 0)
                {
                    FxSystem.Pulse(hits[i].point, shot.Weapon.projectileColor, 0.1f, 0.6f, 0.12f);
                    return false;
                }
            }

            position += direction * distance;
            if (bouncesLeft > 0 && !Arena.Contains(position)) Bounce();
            transform.position = position;
            life -= dt;

            if (life > 0f && Arena.Contains(position, 0.5f)) return true;
            if (shot.ExplosionRadius > 0f && life <= 0f) Explode(position);
            return false;
        }

        void Bounce()
        {
            var h = Arena.HalfSize;
            if (Mathf.Abs(position.x) > h.x) { direction.x = -direction.x; position.x = Mathf.Clamp(position.x, -h.x, h.x); }
            if (Mathf.Abs(position.y) > h.y) { direction.y = -direction.y; position.y = Mathf.Clamp(position.y, -h.y, h.y); }
            bouncesLeft--;
            alreadyHit.Clear();
            life = Mathf.Max(life, shot.Lifetime * 0.6f);
            FxSystem.Pulse(position, shot.Weapon.projectileColor, 0.1f, 0.5f, 0.1f);
        }

        void Explode(Vector2 at) =>
            ProjectileSystem.Explode(at, shot.ExplosionRadius, damage, shot.Knockback, crit, shot.Weapon.projectileColor,
                shot.SlowAmount, shot.SlowDuration, shot.BurnDps, shot.BurnDuration, shot.ChainLightning, shot.Bomblets);
    }
}
