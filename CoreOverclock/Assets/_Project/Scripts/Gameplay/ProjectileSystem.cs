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
        ContactFilter2D filter;

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

        public static void Spawn(WeaponData data, Vector2 position, Vector2 direction, bool crit)
        {
            if (!instance) return;
            var p = instance.pool.Get();
            p.Launch(data, position, direction, crit);
            instance.active.Add(p);
        }

        public static void ClearAll()
        {
            if (!instance) return;
            for (int i = instance.active.Count - 1; i >= 0; i--) instance.pool.Release(instance.active[i]);
            instance.active.Clear();
        }

        void Update()
        {
            float dt = Time.deltaTime;
            for (int i = active.Count - 1; i >= 0; i--)
            {
                if (active[i].Step(dt, filter, hits)) continue;
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
        WeaponData data;
        Vector2 position, direction;
        float life, damage;
        int pierceLeft;
        bool crit;

        public void Launch(WeaponData weapon, Vector2 pos, Vector2 dir, bool isCrit)
        {
            data = weapon;
            position = pos;
            direction = dir;
            crit = isCrit;
            life = weapon.projectileLifetime;
            pierceLeft = weapon.pierce;
            damage = weapon.damage * (isCrit ? weapon.critMultiplier : 1f);
            alreadyHit.Clear();

            var color = isCrit ? Color.white : weapon.projectileColor;
            Body.color = color;
            Glow.color = new Color(weapon.projectileColor.r, weapon.projectileColor.g, weapon.projectileColor.b, 0.35f);
            transform.localScale = Vector3.one * (weapon.projectileRadius * 2f * (isCrit ? 1.4f : 1f));
            transform.SetPositionAndRotation(pos, Quaternion.identity);
        }

        /// <returns>false when the projectile should be released.</returns>
        public bool Step(float dt, ContactFilter2D filter, List<RaycastHit2D> hits)
        {
            var gm = GameManager.Instance;
            if (!gm || gm.State != GameState.Combat) return false;

            float distance = data.projectileSpeed * dt;
            int n = Physics2D.CircleCast(position, data.projectileRadius, direction, filter, hits, distance);
            for (int i = 0; i < n; i++)
            {
                var col = hits[i].collider;
                if (!alreadyHit.Add(col)) continue;
                if (!col.TryGetComponent(out IDamageable target) || !target.IsAlive) continue;

                target.TakeDamage(new DamageInfo
                {
                    Amount = damage, Crit = crit, Direction = direction, Knockback = data.knockback,
                });
                if (--pierceLeft < 0)
                {
                    FxSystem.Pulse(hits[i].point, data.projectileColor, 0.1f, 0.6f, 0.12f);
                    return false;
                }
            }

            position += direction * distance;
            transform.position = position;
            life -= dt;
            return life > 0f && Arena.Contains(position, 0.5f);
        }
    }
}
