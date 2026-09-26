using System.Collections.Generic;
using UnityEngine;

namespace CoreOverclock
{
    /// <summary>Pooled enemy bullets (터렛 드론, 보스 탄막). Collision is a cheap distance check against the player.</summary>
    public class EnemyProjectileSystem : MonoBehaviour
    {
        const int MaxActive = 700;
        const float PlayerRadius = 0.32f;
        const float Lifetime = 8f;

        struct Bullet
        {
            public Transform Transform;
            public SpriteRenderer Ring, Glow;
            public Vector2 Position, Velocity;
            public float Damage, Radius, Life;
        }

        static EnemyProjectileSystem instance;
        Pool<Transform> pool;
        readonly List<Bullet> active = new();
        int clearVersion; // bumped by ClearAll so Update can bail if a hit killed the player mid-loop

        public static EnemyProjectileSystem Create(Transform parent)
        {
            var go = new GameObject("EnemyProjectiles");
            go.transform.SetParent(parent, false);
            var sys = go.AddComponent<EnemyProjectileSystem>();
            instance = sys;
            sys.pool = new Pool<Transform>(() =>
            {
                var t = new GameObject("EnemyBullet").transform;
                t.SetParent(go.transform, false);
                Visuals.Glow(t, Color.white, 2.6f, 0.35f, 34);
                Visuals.Sprite("Ring", t, ShapeSprites.Circle, Color.white, 35, 1.25f);
                Visuals.Sprite("Core", t, ShapeSprites.Circle, Color.white, 36, 0.7f);
                return t;
            }, 128);
            return sys;
        }

        public static void Fire(Vector2 position, Vector2 velocity, float damage, Color color, float radius = 0.16f)
        {
            if (!instance || instance.active.Count >= MaxActive) return;
            var t = instance.pool.Get();
            t.position = position;
            t.localScale = Vector3.one * (radius * 2f);
            var glow = t.GetChild(0).GetComponent<SpriteRenderer>();
            var ring = t.GetChild(1).GetComponent<SpriteRenderer>();
            glow.color = new Color(color.r, color.g, color.b, 0.4f);
            ring.color = color;
            instance.active.Add(new Bullet
            {
                Transform = t, Ring = ring, Glow = glow, Position = position, Velocity = velocity,
                Damage = damage, Radius = radius, Life = Lifetime,
            });
        }

        public static void ClearAll()
        {
            if (!instance) return;
            foreach (var b in instance.active) instance.pool.Release(b.Transform);
            instance.active.Clear();
            instance.clearVersion++;
        }

        /// <summary>긴급 방열 also wipes nearby bullets.</summary>
        public static void ClearInRadius(Vector2 center, float radius)
        {
            if (!instance) return;
            var list = instance.active;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if ((list[i].Position - center).sqrMagnitude > radius * radius) continue;
                instance.pool.Release(list[i].Transform);
                list[i] = list[^1];
                list.RemoveAt(list.Count - 1);
            }
        }

        void Update()
        {
            var gm = GameManager.Instance;
            if (!gm || gm.State != GameState.Combat) return;
            float dt = Time.deltaTime;
            var player = gm.Player;
            Vector2 pp = player.Position;
            int version = clearVersion;

            for (int i = active.Count - 1; i >= 0; i--)
            {
                var b = active[i];
                b.Position += b.Velocity * dt;
                b.Life -= dt;
                bool remove = b.Life <= 0f || !Arena.Contains(b.Position, 0.3f);

                float hit = b.Radius + PlayerRadius;
                if (!remove && player.IsAlive && (b.Position - pp).sqrMagnitude < hit * hit)
                {
                    player.TakeDamage(b.Damage, b.Velocity.normalized * 4f);
                    if (version != clearVersion) return;
                    remove = true;
                }

                if (remove)
                {
                    pool.Release(b.Transform);
                    active[i] = active[^1];
                    active.RemoveAt(active.Count - 1);
                    continue;
                }
                b.Transform.position = b.Position;
                active[i] = b;
            }
        }
    }
}
