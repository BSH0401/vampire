using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CoreOverclock
{
    /// <summary>
    /// 코어 선택: shown after "게임 시작". Pick the core for this run or unlock one with 코어 파편.
    /// The choice is saved, so "다시 시작" keeps the same core.
    /// </summary>
    public class CoreSelectScreen : MonoBehaviour
    {
        static readonly Color CardColor = new(0.07f, 0.09f, 0.15f, 1f);
        static readonly Color SelectedCardColor = new(0.1f, 0.14f, 0.22f, 1f);
        static readonly Color Muted = new(0.45f, 0.5f, 0.62f);
        static readonly Color SubText = new(0.7f, 0.76f, 0.88f);
        static readonly Color Upside = new(0.55f, 1f, 0.65f);
        static readonly Color Downside = new(1f, 0.45f, 0.45f);

        GameManager gm;
        GameObject root;
        RectTransform content;
        Text fragmentsLabel, startWeaponLabel;
        Button launchButton;
        System.Action onClose;

        public bool IsOpen => root.activeSelf;

        public static CoreSelectScreen Create(Canvas canvas, GameManager gm)
        {
            var s = canvas.gameObject.AddComponent<CoreSelectScreen>();
            s.gm = gm;
            s.Build(canvas.transform);
            return s;
        }

        void Build(Transform canvas)
        {
            var dim = UIFactory.Stretch("CoreSelectScreen", canvas);
            UIFactory.Image(dim, new Color(0.02f, 0.02f, 0.05f, 1f)).raycastTarget = true;
            root = dim.gameObject;
            Vector2 c = new(0.5f, 0.5f), top = new(0.5f, 1f), tr = new(1f, 1f);

            UIFactory.Label(UIFactory.Rect("Title", dim, top, top, new Vector2(0f, -30f), new Vector2(1200f, 70f)), "코어 선택", 56, Palette.Player);
            UIFactory.Label(UIFactory.Rect("Subtitle", dim, top, top, new Vector2(0f, -100f), new Vector2(1500f, 36f)),
                "이번 판에 사용할 코어를 고르세요. 잠긴 코어는 코어 파편으로 해금할 수 있습니다.", 24, SubText);
            fragmentsLabel = UIFactory.Label(UIFactory.Rect("Fragments", dim, tr, tr, new Vector2(-48f, -40f), new Vector2(500f, 60f)),
                "", 48, Palette.Fragment, TextAnchor.MiddleRight);

            content = UIFactory.Rect("Content", dim, c, c, new Vector2(0f, 20f), new Vector2(1760f, 660f));
            startWeaponLabel = UIFactory.Label(UIFactory.Rect("StartWeapon", dim, c, c, new Vector2(0f, -350f), new Vector2(1400f, 36f)), "", 24, SubText);

            UIFactory.Button(dim, "뒤로", new Vector2(-230f, -440f), new Vector2(360f, 72f), Close);
            launchButton = UIFactory.Button(dim, "출격 ▶", new Vector2(230f, -440f), new Vector2(360f, 72f), Launch);
            root.SetActive(false);
        }

        public void Open(System.Action closed)
        {
            onClose = closed;
            root.SetActive(true);
            root.transform.SetAsLastSibling();
            Rebuild(null);
            if (EventSystem.current) EventSystem.current.SetSelectedGameObject(launchButton.gameObject);
        }

        public void Close()
        {
            root.SetActive(false);
            onClose?.Invoke();
        }

        void Launch()
        {
            root.SetActive(false);
            gm.StartGame();
        }

        void Update()
        {
            if (IsOpen && GameInput.PausePressed) Close();
        }

        void Rebuild(string focusName)
        {
            fragmentsLabel.text = $"{MetaProgress.Currency} {MetaProgress.Fragments}";
            for (int i = content.childCount - 1; i >= 0; i--)
            {
                var child = content.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }

            var selected = MetaProgress.SelectedCore;
            var all = Cores.All;
            for (int i = 0; i < all.Length; i++)
            {
                int col = i % 3, row = i / 3;
                BuildCard(all[i], new Vector2((col - 1) * 580f, 165f - row * 330f), all[i].Id == selected);
            }

            string weapon = gm.DefaultStartingWeapon ? gm.DefaultStartingWeapon.displayName : "";
            foreach (var w in gm.Database.weapons)
                if (w && w.id == MetaProgress.StartWeaponId && GameManager.IsStartWeaponOption(w)) weapon = w.displayName;
            startWeaponLabel.text = $"시작 무기: <color=#FFFFFF>{weapon}</color>   <color=#7F8AA8>(연구소 › 시작 무기에서 변경)</color>";

            if (focusName == null || !EventSystem.current) return;
            foreach (var b in content.GetComponentsInChildren<Button>())
                if (b.name == focusName) EventSystem.current.SetSelectedGameObject(b.gameObject);
        }

        void BuildCard(CoreDef core, Vector2 pos, bool selected)
        {
            bool unlocked = MetaProgress.IsUnlocked(core);
            var rt = UIFactory.Rect($"Core_{core.Id}", content, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(560f, 310f));
            UIFactory.Image(rt, selected ? SelectedCardColor : CardColor);
            UIFactory.Image(UIFactory.Rect("Accent", rt, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(8f, 310f)),
                unlocked ? core.Color : Muted);
            if (selected)
                UIFactory.Image(UIFactory.Rect("SelectedTop", rt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(560f, 5f)), core.Color);

            Vector2 tl = new(0f, 1f);
            Text Line(string name, string text, float y, float h, int size, Color color)
            {
                var r = UIFactory.Rect(name, rt, tl, tl, new Vector2(32f, y), new Vector2(500f, h));
                return UIFactory.Label(r, text, size, color, TextAnchor.MiddleLeft);
            }

            Line("Name", core.Name + (selected ? "  <size=22><color=#FFFFFF>· 선택됨</color></size>" : ""), -40f, 48f, 36, unlocked ? core.Color : Muted);
            Line("Tagline", core.Tagline, -90f, 30f, 22, SubText);
            for (int i = 0; i < core.Perks.Length; i++)
            {
                string perk = core.Perks[i];
                bool up = perk.StartsWith("+"), down = perk.StartsWith("-");
                string text = up || down ? (up ? "▲ " : "▼ ") + perk.Substring(1) : "· " + perk;
                Line($"Perk{i}", text, -138f - i * 34f, 32f, 23, !unlocked ? Muted : up ? Upside : down ? Downside : Palette.Text);
            }

            string id = $"Pick_{core.Id}";
            string label;
            bool interactable;
            UnityEngine.Events.UnityAction action;
            if (unlocked)
            {
                label = selected ? "선택됨" : "선택";
                interactable = !selected;
                action = () =>
                {
                    MetaProgress.SelectedCore = core.Id;
                    AudioManager.Play(SfxId.Buy, 0.5f, 0f);
                    Rebuild(id);
                };
            }
            else if (!Cores.CanUnlockInThisBuild(core))
            {
                label = "정식판 전용";
                interactable = false;
                action = () => { };
            }
            else
            {
                label = $"해금  {MetaProgress.Currency} {core.UnlockCost}";
                interactable = MetaProgress.Fragments >= core.UnlockCost;
                action = () =>
                {
                    bool ok = MetaProgress.TryUnlock(core);
                    if (ok) MetaProgress.SelectedCore = core.Id;
                    AudioManager.Play(ok ? SfxId.Buy : SfxId.Deny, 0.5f, 0f);
                    Rebuild(id);
                };
            }
            var button = UIFactory.Button(rt, label, new Vector2(150f, -115f), new Vector2(220f, 56f), action);
            button.name = id;
            button.interactable = interactable;
            button.GetComponentInChildren<Text>().fontSize = 24;
        }
    }
}
