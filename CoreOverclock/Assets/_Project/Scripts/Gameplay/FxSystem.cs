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

        static FxSystem instance;
        Pool<SpriteRenderer> pool;
        readonly List<PulseFx> active = new();

        public static FxSystem Create(Transform parent)
        {
            var go = new GameObject("Fx");
            go.transform.SetParent(parent, false);
            var fx = go.AddComponent<FxSystem>();
            instance = fx;
            fx.pool = new Pool<SpriteRenderer>(
                () => Visuals.Sprite("Pulse", go.transform, ShapeSprites.Ring, Color.white, 40), 32);
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

        void Update()
        {
            float dt = Time.deltaTime;
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
