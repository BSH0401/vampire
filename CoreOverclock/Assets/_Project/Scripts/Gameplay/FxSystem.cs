using System.Collections.Generic;
using UnityEngine;

namespace CoreOverclock
{
    /// <summary>Pooled expanding-ring pulses used for deaths, impacts and wall zaps.</summary>
    public class FxSystem : MonoBehaviour
    {
        struct PulseFx
        {
            public SpriteRenderer Renderer;
            public Color Color;
            public float Time, Duration, From, To;
        }

        struct Shard
        {
            public SpriteRenderer Renderer;
            public Vector2 Position, Velocity;
            public Color Color;
            public float Time, Duration, Size, Spin;
        }

        const int MaxShards = 400;

        static FxSystem instance;
        Pool<SpriteRenderer> pool;
        Pool<SpriteRenderer> shardPool;
        readonly List<PulseFx> active = new();
        readonly List<Shard> shards = new();

        public static FxSystem Create(Transform parent)
        {
            var go = new GameObject("Fx");
            go.transform.SetParent(parent, false);
            var fx = go.AddComponent<FxSystem>();
            instance = fx;
            fx.pool = new Pool<SpriteRenderer>(
                () => Visuals.Sprite("Pulse", go.transform, ShapeSprites.Ring, Color.white, 40), 32);
            fx.shardPool = new Pool<SpriteRenderer>(
                () => Visuals.Sprite("Shard", go.transform, ShapeSprites.White, Color.white, 41), 64);
            return fx;
        }

        public static void Pulse(Vector2 position, Color color, float fromScale, float toScale, float duration)
        {
            if (!instance) return;
            var sr = instance.pool.Get();
            // Large pulses use a thin ring so the stroke doesn't balloon with scale.
            sr.sprite = toScale > 3f ? ShapeSprites.ThinRing : ShapeSprites.Ring;
            sr.transform.position = position;
            sr.transform.localScale = Vector3.one * fromScale;
            sr.color = color;
            instance.active.Add(new PulseFx { Renderer = sr, Color = color, Duration = duration, From = fromScale, To = toScale });
        }

        /// <summary>Scatters small spinning shards (enemy deaths, hits, explosions).</summary>
        public static void Burst(Vector2 position, Color color, int count, float speed, float size = 0.12f, float duration = 0.4f)
        {
            if (!instance) return;
            for (int i = 0; i < count && instance.shards.Count < MaxShards; i++)
            {
                var sr = instance.shardPool.Get();
                var dir = Random.insideUnitCircle.normalized;
                instance.shards.Add(new Shard
                {
                    Renderer = sr, Position = position, Velocity = dir * speed * Random.Range(0.4f, 1f), Color = color,
                    Duration = duration * Random.Range(0.7f, 1.2f), Size = size * Random.Range(0.6f, 1.3f),
                    Spin = Random.Range(-720f, 720f),
                });
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            for (int i = shards.Count - 1; i >= 0; i--)
            {
                var s = shards[i];
                s.Time += dt;
                float k = s.Time / s.Duration;
                if (k >= 1f)
                {
                    shardPool.Release(s.Renderer);
                    shards[i] = shards[^1];
                    shards.RemoveAt(shards.Count - 1);
                    continue;
                }
                s.Velocity *= 1f - 6f * dt; // drag
                s.Position += s.Velocity * dt;
                var t = s.Renderer.transform;
                t.SetPositionAndRotation(s.Position, Quaternion.Euler(0f, 0f, s.Spin * s.Time));
                t.localScale = Vector3.one * (s.Size * (1f - k * 0.6f));
                var c = s.Color;
                c.a = 1f - k * k;
                s.Renderer.color = c;
                shards[i] = s;
            }

            for (int i = active.Count - 1; i >= 0; i--)
            {
                var p = active[i];
                p.Time += dt;
                float k = Mathf.Clamp01(p.Time / p.Duration);
                if (k >= 1f)
                {
                    pool.Release(p.Renderer);
                    active[i] = active[^1];
                    active.RemoveAt(active.Count - 1);
                    continue;
                }
                float eased = 1f - (1f - k) * (1f - k);
                p.Renderer.transform.localScale = Vector3.one * Mathf.Lerp(p.From, p.To, eased);
                var c = p.Color;
                c.a = 1f - k;
                p.Renderer.color = c;
                active[i] = p;
            }
        }
    }
}
