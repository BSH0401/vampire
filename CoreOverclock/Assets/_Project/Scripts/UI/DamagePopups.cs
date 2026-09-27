using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CoreOverclock
{
    /// <summary>Pooled floating damage numbers (기획서 7.2).</summary>
    public class DamagePopups : MonoBehaviour
    {
        const float Lifetime = 0.6f;
        const float Rise = 70f;
        /// <summary>Above this many live numbers, ordinary hits are skipped so crits stay readable.</summary>
        const int CrowdLimit = 30;

        class Popup
        {
            public Text Text;
            public RectTransform Rect;
            public Vector2 Start;
            public Color Color;
            public float Time, Scale;
        }

        static DamagePopups instance;
        RectTransform root;
        Camera cam;
        readonly List<Popup> active = new();
        readonly Stack<Popup> free = new();

        public static DamagePopups Create(Canvas canvas)
        {
            var root = UIFactory.Stretch("DamagePopups", canvas.transform);
            var dp = root.gameObject.AddComponent<DamagePopups>();
            dp.root = root;
            dp.cam = Camera.main;
            instance = dp;
            for (int i = 0; i < 48; i++) dp.free.Push(dp.CreatePopup());
            return dp;
        }

        Popup CreatePopup()
        {
            var rt = UIFactory.Rect("Popup", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200f, 60f));
            var text = UIFactory.Label(rt, "", 30, Color.white);
            rt.gameObject.SetActive(false);
            return new Popup { Text = text, Rect = rt };
        }

        public static void Show(Vector2 world, float amount, Color color, bool big)
        {
            if (!instance || !instance.cam) return;
            if (!big && instance.active.Count >= CrowdLimit) return;
            var p = instance.free.Count > 0 ? instance.free.Pop() : instance.CreatePopup();
            Vector2 screen = instance.cam.WorldToScreenPoint(world + Random.insideUnitCircle * 0.2f);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(instance.root, screen, null, out var local);
            p.Start = local;
            p.Color = color;
            p.Time = 0f;
            p.Scale = big ? 1.45f : 0.8f;
            p.Text.text = Mathf.CeilToInt(amount).ToString();
            p.Text.color = color;
            p.Rect.anchoredPosition = local;
            p.Rect.gameObject.SetActive(true);
            instance.active.Add(p);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var p = active[i];
                p.Time += dt;
                float k = p.Time / Lifetime;
                if (k >= 1f)
                {
                    p.Rect.gameObject.SetActive(false);
                    free.Push(p);
                    active[i] = active[^1];
                    active.RemoveAt(active.Count - 1);
                    continue;
                }
                float ease = 1f - (1f - k) * (1f - k);
                p.Rect.anchoredPosition = p.Start + Vector2.up * (Rise * ease);
                float pop = k < 0.15f ? Mathf.Lerp(1.6f, 1f, k / 0.15f) : 1f;
                p.Rect.localScale = Vector3.one * (p.Scale * pop);
                var c = p.Color;
                c.a = k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f;
                p.Text.color = c;
            }
        }
    }
}
