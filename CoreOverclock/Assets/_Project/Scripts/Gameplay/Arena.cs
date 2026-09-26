using UnityEngine;

namespace CoreOverclock
{
    /// <summary>16:9 fixed arena surrounded by electric walls (기획서 4.2).</summary>
    public class Arena : MonoBehaviour
    {
        public static readonly Vector2 HalfSize = new(15f, 8f);

        SpriteRenderer[] wallLines;
        SpriteRenderer[] wallGlows;

        public static bool Contains(Vector2 p, float margin = 0f) =>
            Mathf.Abs(p.x) <= HalfSize.x + margin && Mathf.Abs(p.y) <= HalfSize.y + margin;

        public static Vector2 RandomPoint(float inset) => new(
            Random.Range(-HalfSize.x + inset, HalfSize.x - inset),
            Random.Range(-HalfSize.y + inset, HalfSize.y - inset));

        public static Arena Create(Transform parent)
        {
            var go = new GameObject("Arena");
            go.transform.SetParent(parent, false);
            var arena = go.AddComponent<Arena>();
            arena.Build();
            return arena;
        }

        void Build()
        {
            var grid = Visuals.Sprite("Grid", transform, ShapeSprites.GridTile, Palette.Grid, -100);
            grid.drawMode = SpriteDrawMode.Tiled;
            grid.size = HalfSize * 2f;

            const float thick = 2f;
            float w = HalfSize.x, h = HalfSize.y;
            var defs = new (Vector2 pos, Vector2 size)[]
            {
                (new Vector2(0f, h + thick / 2f), new Vector2(w * 2f + thick * 2f, thick)),
                (new Vector2(0f, -h - thick / 2f), new Vector2(w * 2f + thick * 2f, thick)),
                (new Vector2(-w - thick / 2f, 0f), new Vector2(thick, h * 2f)),
                (new Vector2(w + thick / 2f, 0f), new Vector2(thick, h * 2f)),
            };
            var lineDefs = new (Vector2 pos, Vector2 size)[]
            {
                (new Vector2(0f, h), new Vector2(w * 2f + 0.12f, 0.12f)),
                (new Vector2(0f, -h), new Vector2(w * 2f + 0.12f, 0.12f)),
                (new Vector2(-w, 0f), new Vector2(0.12f, h * 2f)),
                (new Vector2(w, 0f), new Vector2(0.12f, h * 2f)),
            };

            wallLines = new SpriteRenderer[4];
            wallGlows = new SpriteRenderer[4];
            for (int i = 0; i < 4; i++)
            {
                var wall = new GameObject($"Wall_{i}") { layer = GameLayers.Wall };
                wall.transform.SetParent(transform, false);
                wall.transform.localPosition = defs[i].pos;
                wall.AddComponent<BoxCollider2D>().size = defs[i].size;

                var line = Visuals.Sprite($"WallLine_{i}", transform, ShapeSprites.White, Palette.Wall, -5);
                line.transform.localPosition = lineDefs[i].pos;
                line.transform.localScale = lineDefs[i].size; // White sprite is 1x1 unit
                wallLines[i] = line;

                var glowColor = Palette.Wall;
                glowColor.a = 0.18f;
                var glow = Visuals.Sprite($"WallGlow_{i}", transform, ShapeSprites.White, glowColor, -6);
                glow.transform.localPosition = lineDefs[i].pos;
                var glowSize = lineDefs[i].size;
                if (glowSize.x < 1f) glowSize.x = 0.35f; else glowSize.y = 0.35f;
                glow.transform.localScale = glowSize;
                wallGlows[i] = glow;
            }
        }

        void Update()
        {
            // Electric flicker
            float t = Time.time * 12f;
            for (int i = 0; i < 4; i++)
            {
                float n = Mathf.PerlinNoise(t, i * 3.1f);
                var c = Palette.Wall;
                c.a = 0.65f + 0.35f * n;
                wallLines[i].color = c;
                var g = Palette.Wall;
                g.a = 0.08f + 0.2f * n;
                wallGlows[i].color = g;
            }
        }
    }
}
