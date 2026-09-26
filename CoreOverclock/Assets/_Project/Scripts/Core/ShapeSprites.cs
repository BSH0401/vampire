using System;
using System.Collections.Generic;
using UnityEngine;

namespace CoreOverclock
{
    /// <summary>
    /// Generates the minimal neon geometry sprites at runtime (graybox phase: no art assets needed).
    /// "Neon" variants are a dim fill with a bright outline.
    /// </summary>
    public static class ShapeSprites
    {
        const int Res = 128;
        static readonly Dictionary<string, Sprite> cache = new();

        static readonly Vector2[] TriangleVerts = { new(0.95f, 0f), new(-0.7f, 0.8f), new(-0.7f, -0.8f) };
        static readonly Vector2[] ArrowVerts = { new(1f, 0f), new(-0.45f, 0.6f), new(-0.9f, 0f), new(-0.45f, -0.6f) };
        static readonly Vector2[] SquareVerts = { new(-0.85f, -0.85f), new(0.85f, -0.85f), new(0.85f, 0.85f), new(-0.85f, 0.85f) };
        static readonly Vector2[] DiamondVerts = { new(0f, 0.9f), new(-0.6f, 0f), new(0f, -0.9f), new(0.6f, 0f) };
        static readonly Vector2[] OctagonVerts = RegularPolygon(8, 0.9f, 22.5f);

        public static Sprite White => Get("white", false, p => Mathf.Max(Mathf.Abs(p.x), Mathf.Abs(p.y)) - 1.1f);
        public static Sprite Circle => Get("circle", false, p => p.magnitude - 0.9f);
        public static Sprite Diamond => Get("diamond", false, p => Poly(p, DiamondVerts));
        public static Sprite Ring => Get("ring", false, p => Mathf.Abs(p.magnitude - 0.82f) - 0.1f);
        public static Sprite ThinRing => Get("ring_thin", false, p => Mathf.Abs(p.magnitude - 0.9f) - 0.025f);
        public static Sprite Cross => Get("cross", false, p =>
        {
            var r = Rotate(p, 45f);
            return Mathf.Min(Box(r, new Vector2(0.85f, 0.14f)), Box(r, new Vector2(0.14f, 0.85f)));
        });
        public static Sprite NeonCircle => Get("n_circle", true, p => p.magnitude - 0.85f);
        public static Sprite NeonSquare => Get("n_square", true, p => Poly(p, SquareVerts));

        public static Sprite Glow
        {
            get
            {
                if (cache.TryGetValue("glow", out var s)) return s;
                s = Build("glow", p => { float t = Mathf.Clamp01(1f - p.magnitude); return t * t; });
                cache["glow"] = s;
                return s;
            }
        }

        public static Sprite ForEnemy(EnemyShape shape) => shape switch
        {
            EnemyShape.Triangle => Get("n_tri", true, p => Poly(p, TriangleVerts)),
            EnemyShape.Arrow => Get("n_arrow", true, p => Poly(p, ArrowVerts)),
            EnemyShape.Octagon => Get("n_oct", true, p => Poly(p, OctagonVerts)),
            EnemyShape.Square => NeonSquare,
            _ => NeonCircle,
        };

        public static Sprite GridTile
        {
            get
            {
                if (cache.TryGetValue("grid", out var s)) return s;
                const int n = 32;
                var tex = new Texture2D(n, n, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Repeat, name = "grid"
                };
                var px = new Color32[n * n];
                for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    bool line = x == 0 || y == 0;
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(line ? 255 : 0));
                }
                tex.SetPixels32(px);
                tex.Apply(false, true);
                s = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n, 0, SpriteMeshType.FullRect);
                cache["grid"] = s;
                return s;
            }
        }

        static Sprite Get(string key, bool neon, Func<Vector2, float> sdf)
        {
            if (cache.TryGetValue(key, out var s)) return s;
            const float px = 2f / Res;
            s = neon
                ? Build(key, p =>
                {
                    float d = sdf(p);
                    float fill = Coverage(d, px) * 0.22f;
                    float edge = Coverage(Mathf.Abs(d + 0.07f) - 0.07f, px);
                    return Mathf.Max(fill, edge);
                })
                : Build(key, p => Coverage(sdf(p), px));
            cache[key] = s;
            return s;
        }

        static Sprite Build(string name, Func<Vector2, float> alpha)
        {
            var tex = new Texture2D(Res, Res, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, name = name
            };
            var pixels = new Color32[Res * Res];
            for (int y = 0; y < Res; y++)
            for (int x = 0; x < Res; x++)
            {
                var p = new Vector2((x + 0.5f) / Res * 2f - 1f, (y + 0.5f) / Res * 2f - 1f);
                pixels[y * Res + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(alpha(p)) * 255f));
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, Res, Res), new Vector2(0.5f, 0.5f), Res, 0, SpriteMeshType.FullRect);
        }

        static float Coverage(float d, float px) => Mathf.Clamp01(0.5f - d / px);

        /// <summary>Signed distance (approx.) to a convex CCW polygon.</summary>
        static float Poly(Vector2 p, Vector2[] v)
        {
            float m = float.MinValue;
            for (int i = 0; i < v.Length; i++)
            {
                Vector2 a = v[i], e = v[(i + 1) % v.Length] - a;
                var n = new Vector2(e.y, -e.x).normalized;
                m = Mathf.Max(m, Vector2.Dot(p - a, n));
            }
            return m;
        }

        static float Box(Vector2 p, Vector2 b)
        {
            var q = new Vector2(Mathf.Abs(p.x) - b.x, Mathf.Abs(p.y) - b.y);
            return Vector2.Max(q, Vector2.zero).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f);
        }

        static Vector2 Rotate(Vector2 p, float deg)
        {
            float r = deg * Mathf.Deg2Rad, c = Mathf.Cos(r), s = Mathf.Sin(r);
            return new Vector2(c * p.x - s * p.y, s * p.x + c * p.y);
        }

        static Vector2[] RegularPolygon(int n, float radius, float startDeg)
        {
            var v = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                float a = (startDeg + 360f / n * i) * Mathf.Deg2Rad;
                v[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius;
            }
            return v;
        }
    }

    public static class Visuals
    {
        public static Material SpriteMaterial;

        public static SpriteRenderer Sprite(string name, Transform parent, Sprite sprite, Color color, int order, float scale = 1f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localScale = Vector3.one * scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
            if (SpriteMaterial) sr.sharedMaterial = SpriteMaterial;
            return sr;
        }

        public static SpriteRenderer Glow(Transform parent, Color color, float scale, float alpha = 0.3f, int order = -1)
        {
            color.a = alpha;
            return Sprite("Glow", parent, ShapeSprites.Glow, color, order, scale);
        }
    }
}
