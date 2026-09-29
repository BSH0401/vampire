using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace CoreOverclock
{
    /// <summary>Tiny uGUI builder so the graybox needs no prefabs.</summary>
    public static class UIFactory
    {
        static Font font;

        public static Font Font => font ? font : font = Font.CreateDynamicFontFromOSFont(
            new[] { "Malgun Gothic", "Segoe UI", "Arial" }, 32);

        public static Canvas CreateCanvas(string name, int sortingOrder)
        {
            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>()) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        public static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Stretch(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static Image Image(RectTransform rt, Color color)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        public static Text Label(RectTransform rt, string text, int size, Color color, TextAnchor align = TextAnchor.MiddleCenter)
        {
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            var outline = rt.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
            outline.effectDistance = new Vector2(2f, -2f);
            return t;
        }

        public static Button Button(Transform parent, string label, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction onClick)
        {
            var rt = Rect($"Button_{label}", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = Color.white;
            var btn = rt.gameObject.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = new Color(0.12f, 0.2f, 0.3f, 0.95f);
            colors.highlightedColor = new Color(0.2f, 0.55f, 0.65f, 1f);
            colors.selectedColor = new Color(0.2f, 0.55f, 0.65f, 1f);
            colors.pressedColor = new Color(0.3f, 0.9f, 0.9f, 1f);
            colors.disabledColor = new Color(0.1f, 0.12f, 0.17f, 0.9f);
            colors.fadeDuration = 0f; // instant tint: buttons built mid-frame could freeze half-faded
            btn.colors = colors;
            btn.onClick.AddListener(() => AudioManager.Play(SfxId.UIClick, 0.4f, 0f));
            btn.onClick.AddListener(onClick);
            Label(Stretch("Label", rt), label, 34, Palette.Text);
            return btn;
        }
    }
}
