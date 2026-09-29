using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CoreOverclock
{
    /// <summary>
    /// 시너지 · 퓨전 상세 창: every tag tier and every discovered fusion with its full text.
    /// Opened from the shop (button / Tab) and from the pause menu, so the compact shop panel can stay short.
    /// </summary>
    public class SynergyPanel : MonoBehaviour
    {
        GameManager gm;
        GameObject root;
        Text synergyBody, fusionBody, fusionTitle;
        Button closeButton;
        System.Action onClose;
        int openedFrame, closedFrame = -1;

        public bool IsOpen => root.activeSelf;
        /// <summary>True on the frame the panel handled Esc, so the pause menu doesn't also react.</summary>
        public bool ConsumedInputThisFrame => closedFrame == Time.frameCount;

        public static SynergyPanel Create(Canvas canvas, GameManager gm)
        {
            var p = canvas.gameObject.AddComponent<SynergyPanel>();
            p.gm = gm;
            p.Build(canvas.transform);
            return p;
        }

        /// <summary>Tab / gamepad Y toggles the detail window.</summary>
        public static bool TogglePressed =>
            (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame) ||
            (Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame);

        void Build(Transform canvas)
        {
            var dim = UIFactory.Stretch("SynergyPanel", canvas);
            UIFactory.Image(dim, new Color(0f, 0f, 0.02f, 0.85f)).raycastTarget = true;
            root = dim.gameObject;
            var box = UIFactory.Rect("Box", dim, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1680f, 900f));
            UIFactory.Image(box, new Color(0.05f, 0.07f, 0.12f, 1f));
            Vector2 top = new(0.5f, 1f);
            UIFactory.Image(UIFactory.Rect("Frame", box, top, top, Vector2.zero, new Vector2(1680f, 6f)), Palette.Fusion);
            UIFactory.Label(UIFactory.Rect("Title", box, top, top, new Vector2(0f, -50f), new Vector2(1000f, 64f)), "시너지 · 퓨전", 50, Palette.Fusion);

            UIFactory.Label(UIFactory.Rect("SynergyTitle", box, top, top, new Vector2(-410f, -120f), new Vector2(760f, 40f)),
                "태그 시너지  <size=22><color=#7F8AA8>같은 태그 무기 수에 따라 발동</color></size>", 32, Palette.Text, TextAnchor.MiddleLeft);
            synergyBody = Body(box, new Vector2(-410f, -160f));

            fusionTitle = UIFactory.Label(UIFactory.Rect("FusionTitle", box, top, top, new Vector2(410f, -120f), new Vector2(760f, 40f)),
                "", 32, Palette.Fusion, TextAnchor.MiddleLeft);
            fusionBody = Body(box, new Vector2(410f, -160f));

            closeButton = UIFactory.Button(box, "닫기  (Tab · Esc)", new Vector2(0f, -385f), new Vector2(420f, 70f), Close);
            root.SetActive(false);
        }

        static Text Body(RectTransform box, Vector2 pos)
        {
            var rt = UIFactory.Rect("Body", box, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), pos, new Vector2(760f, 610f));
            var t = UIFactory.Label(rt, "", 24, Palette.Text, TextAnchor.UpperLeft);
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.lineSpacing = 1.1f;
            return t;
        }

        public void Open(System.Action closed = null)
        {
            onClose = closed;
            Refresh();
            root.SetActive(true);
            root.transform.SetAsLastSibling();
            openedFrame = Time.frameCount;
            if (EventSystem.current) EventSystem.current.SetSelectedGameObject(closeButton.gameObject);
        }

        public void Close()
        {
            if (!IsOpen) return;
            root.SetActive(false);
            closedFrame = Time.frameCount;
            onClose?.Invoke();
        }

        void Update()
        {
            if (!IsOpen || openedFrame == Time.frameCount) return;
            if (GameInput.PausePressed || TogglePressed) Close();
        }

        void Refresh()
        {
            var loadout = gm.Loadout;
            var syn = loadout.Synergy;
            var sb = new StringBuilder();
            foreach (WeaponTag tag in System.Enum.GetValues(typeof(WeaponTag)))
            {
                int n = syn.Count(tag);
                var hex = ColorUtility.ToHtmlStringRGB(WeaponTags.ColorOf(tag));
                sb.Append($"<size=28><color=#{hex}>{WeaponTags.KoreanName(tag)}</color></size>  <color=#9AA4BC>장착 {n}개</color>\n");
                foreach (var (count, text) in SynergyState.Tiers(tag))
                    sb.Append(n >= count
                        ? $"   <color=#FFFFFF>■ [{count}개] {text}</color>\n"
                        : $"   <color=#5A6378>[{count}개] {text}</color>\n");
                sb.Append('\n');
            }
            synergyBody.text = sb.ToString();

            sb.Clear();
            int hidden = 0, active = 0;
            foreach (var f in Fusions.All)
            {
                if (!MetaProgress.IsDiscovered(f.Id) && !loadout.Has(f.Id)) { hidden++; continue; }
                bool on = loadout.Has(f.Id);
                if (on) active++;
                sb.Append(on
                    ? $"<size=27><color=#FFD24A>★ {f.Name}</color></size>  <color=#7CFF8A>발동 중</color>  <size=21><color=#E6F0FF>{f.Recipe}</color></size>\n"
                    : $"<size=27><color=#8A93A8>☆ {f.Name}</color></size>  <size=21><color=#6C7488>{f.Recipe}</color></size>\n");
                sb.Append($"   <size=22><color=#{(on ? "B8C4DC" : "5A6378")}>{f.Effect}</color></size>\n");
            }
            if (hidden > 0) sb.Append($"<color=#7F8AA8>미발견 퓨전 {hidden}개 — 무기를 모으다 보면 발견할 수 있습니다</color>");
            fusionBody.text = sb.ToString();
            fusionTitle.text = $"퓨전  <size=22><color=#7F8AA8>발동 {active} · 발견 {MetaProgress.DiscoveredCount}/{Fusions.All.Length}</color></size>";
        }
    }
}
