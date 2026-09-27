using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CoreOverclock
{
    /// <summary>
    /// 연구소: spend 코어 파편 on permanent upgrades and unlocks, browse the 퓨전 도감, pick the starting weapon.
    /// Opened from the title screen; the tab content is rebuilt on every change.
    /// </summary>
    public class LabScreen : MonoBehaviour
    {
        enum Tab { Upgrades, Unlocks, Codex, StartWeapon }

        static readonly Color CardColor = new(0.07f, 0.09f, 0.15f, 1f);
        static readonly Color Muted = new(0.45f, 0.5f, 0.62f);
        static readonly Color SubText = new(0.7f, 0.76f, 0.88f);

        GameManager gm;
        GameObject root;
        RectTransform content;
        Text fragmentsLabel;
        Button backButton;
        readonly List<(Tab tab, Button button)> tabButtons = new();
        Tab tab;
        System.Action onClose;

        public bool IsOpen => root.activeSelf;

        public static LabScreen Create(Canvas canvas, GameManager gm)
        {
            var lab = canvas.gameObject.AddComponent<LabScreen>();
            lab.gm = gm;
            lab.Build(canvas.transform);
            return lab;
        }

        void Build(Transform canvas)
        {
            var dim = UIFactory.Stretch("LabScreen", canvas);
            UIFactory.Image(dim, new Color(0.02f, 0.02f, 0.05f, 1f)).raycastTarget = true;
            root = dim.gameObject;
            Vector2 c = new(0.5f, 0.5f), top = new(0.5f, 1f), tr = new(1f, 1f);

            UIFactory.Label(UIFactory.Rect("Title", dim, top, top, new Vector2(0f, -30f), new Vector2(1200f, 70f)), "연구소", 56, Palette.Fragment);
            UIFactory.Label(UIFactory.Rect("Subtitle", dim, top, top, new Vector2(0f, -100f), new Vector2(1400f, 36f)),
                "코어 파편으로 코어를 영구 강화하고 새 부품을 해금하세요. 파편은 매 판이 끝날 때 얻습니다.", 24, SubText);
            fragmentsLabel = UIFactory.Label(UIFactory.Rect("Fragments", dim, tr, tr, new Vector2(-48f, -40f), new Vector2(500f, 60f)),
                "", 48, Palette.Fragment, TextAnchor.MiddleRight);

            (Tab, string)[] tabs = { (Tab.Upgrades, "영구 강화"), (Tab.Unlocks, "부품 해금"), (Tab.Codex, "퓨전 도감"), (Tab.StartWeapon, "시작 무기") };
            for (int i = 0; i < tabs.Length; i++)
            {
                var t = tabs[i].Item1;
                var b = UIFactory.Button(dim, tabs[i].Item2, new Vector2((i - 1.5f) * 320f, 365f), new Vector2(300f, 64f), () => SelectTab(t));
                tabButtons.Add((t, b));
            }

            content = UIFactory.Rect("Content", dim, c, c, new Vector2(0f, -20f), new Vector2(1720f, 640f));
            backButton = UIFactory.Button(dim, "뒤로", new Vector2(0f, -440f), new Vector2(360f, 70f), Close);
            root.SetActive(false);
        }

        public void Open(System.Action closed, int startTab = 0)
        {
            onClose = closed;
            root.SetActive(true);
            root.transform.SetAsLastSibling();
            SelectTab((Tab)Mathf.Clamp(startTab, 0, 3));
        }

        public void Close()
        {
            root.SetActive(false);
            onClose?.Invoke();
        }

        void Update()
        {
            if (IsOpen && GameInput.PausePressed) Close();
        }

        void SelectTab(Tab t)
        {
            tab = t;
            Rebuild(null);
        }

        /// <param name="focusName">Name of the button to keep selected after the rebuild (gamepad navigation).</param>
        void Rebuild(string focusName)
        {
            fragmentsLabel.text = $"{MetaProgress.Currency} {MetaProgress.Fragments}";
            foreach (var (t, b) in tabButtons)
            {
                var colors = b.colors;
                colors.normalColor = t == tab ? new Color(0.35f, 0.25f, 0.55f, 1f) : new Color(0.12f, 0.2f, 0.3f, 0.95f);
                b.colors = colors;
            }

            for (int i = content.childCount - 1; i >= 0; i--)
            {
                var child = content.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }

            switch (tab)
            {
                case Tab.Upgrades: BuildUpgrades(); break;
                case Tab.Unlocks: BuildUnlocks(); break;
                case Tab.Codex: BuildCodex(); break;
                case Tab.StartWeapon: BuildStartWeapons(); break;
            }

            if (!EventSystem.current) return;
            GameObject focus = null;
            if (focusName != null)
                foreach (var btn in content.GetComponentsInChildren<Button>())
                    if (btn.name == focusName) focus = btn.gameObject;
            if (!focus)
                foreach (var (t, b) in tabButtons) if (t == tab) focus = b.gameObject;
            EventSystem.current.SetSelectedGameObject(focus);
        }

        static void Feedback(bool ok) => AudioManager.Play(ok ? SfxId.Buy : SfxId.Deny, 0.5f, 0f);

        RectTransform Card(string name, Vector2 pos, Vector2 size, Color accent)
        {
            var rt = UIFactory.Rect(name, content, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
            UIFactory.Image(rt, CardColor);
            var bar = UIFactory.Rect("Accent", rt, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(6f, size.y));
            UIFactory.Image(bar, accent);
            return rt;
        }

        static Text AddText(RectTransform parent, string text, Vector2 pos, Vector2 size, int fontSize, Color color, TextAnchor align = TextAnchor.MiddleLeft)
        {
            var rt = UIFactory.Rect("Text", parent, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), pos, size);
            return UIFactory.Label(rt, text, fontSize, color, align);
        }

        Button CardButton(RectTransform card, string id, string label, Vector2 pos, Vector2 size, bool interactable, UnityEngine.Events.UnityAction onClick)
        {
            var b = UIFactory.Button(card, label, pos, size, onClick);
            b.name = id;
            b.interactable = interactable;
            b.GetComponentInChildren<Text>().fontSize = 26;
            return b;
        }

        // ─────────────── 영구 강화 ───────────────

        void BuildUpgrades()
        {
            var defs = MetaProgress.Upgrades;
            for (int i = 0; i < defs.Length; i++)
            {
                var def = defs[i];
                int level = MetaProgress.Level(def.Id);
                int max = MetaProgress.MaxLevel(def);
                int cost = MetaProgress.NextCost(def);
                var card = Card($"Upgrade_{def.Id}", new Vector2(0f, 262f - i * 104f), new Vector2(1300f, 90f), Palette.Fragment);

                AddText(card, def.Name, new Vector2(30f, 0f), new Vector2(260f, 50f), 32, Palette.Text);
                string effect = level == 0 ? "효과 없음" : def.Describe(level);
                string next = cost >= 0 ? $"  →  <color=#{ColorUtility.ToHtmlStringRGB(Palette.Fragment)}>{def.Describe(level + 1)}</color>" : "";
                AddText(card, effect + next, new Vector2(300f, 0f), new Vector2(560f, 50f), 26, SubText);

                var pips = new System.Text.StringBuilder();
                for (int l = 0; l < def.Costs.Length; l++)
                    pips.Append(l < level ? "■" : l < max ? "□" : "<color=#333A4A>□</color>");
                AddText(card, pips.ToString(), new Vector2(860f, 0f), new Vector2(180f, 50f), 30, Palette.Fragment);

                bool affordable = cost >= 0 && MetaProgress.Fragments >= cost;
                string label = cost < 0 ? (level < def.Costs.Length ? "데모 한도" : "MAX") : $"강화  {MetaProgress.Currency} {cost}";
                string id = $"Buy_{def.Id}";
                CardButton(card, id, label, new Vector2(520f, 0f), new Vector2(230f, 62f), affordable, () =>
                {
                    bool ok = MetaProgress.TryUpgrade(def);
                    Feedback(ok);
                    Rebuild(id);
                });
            }
        }

        // ─────────────── 부품 해금 ───────────────

        void BuildUnlocks()
        {
            var items = new List<(string id, string name, string sub, string detail, int tier, int cost, Color color)>();
            foreach (var w in gm.Database.weapons)
                if (w && w.unlockCost > 0)
                    items.Add((w.id, w.displayName, $"무기 · {WeaponTags.KoreanName(w.tag)} · T{w.tier}", WeaponSummary(w), w.tier, w.unlockCost, WeaponTags.ColorOf(w.tag)));
            foreach (var ch in gm.Database.chips)
                if (ch && ch.unlockCost > 0)
                    items.Add((ch.id, ch.displayName, $"칩셋 · T{ch.tier}", ChipSummary(ch), ch.tier, ch.unlockCost, Palette.Chip));

            const int columns = 4;
            for (int i = 0; i < items.Count; i++)
            {
                var it = items[i];
                int col = i % columns, row = i / columns;
                var card = Card($"Unlock_{it.id}", new Vector2((col - 1.5f) * 425f, 262f - row * 124f), new Vector2(410f, 112f), it.color);
                bool unlocked = MetaProgress.IsUnlocked(it.id, it.cost);
                AddText(card, it.name, new Vector2(24f, 28f), new Vector2(260f, 40f), 26, unlocked ? it.color : Palette.Text);
                AddText(card, it.sub, new Vector2(24f, -3f), new Vector2(260f, 26f), 18, SubText);
                AddText(card, it.detail, new Vector2(24f, -30f), new Vector2(260f, 26f), 16, Muted);

                bool allowed = MetaProgress.CanUnlockInThisBuild(it.tier);
                string label = unlocked ? "해금됨" : !allowed ? "정식판" : $"{MetaProgress.Currency} {it.cost}";
                string id = $"Unlock_{it.id}";
                var (itemId, cost, tier) = (it.id, it.cost, it.tier);
                CardButton(card, id, label, new Vector2(142f, 0f), new Vector2(110f, 58f),
                    !unlocked && allowed && MetaProgress.Fragments >= cost, () =>
                    {
                        bool ok = MetaProgress.TryUnlock(itemId, cost, tier);
                        Feedback(ok);
                        Rebuild(id);
                    });
            }
        }

        static string ChipSummary(ChipData c)
        {
            var parts = new List<string>();
            foreach (var m in c.modifiers) parts.Add(m.Describe());
            return string.Join(", ", parts);
        }

        static string WeaponSummary(WeaponData w)
        {
            string count = w.projectileCount > 1 ? $"x{w.projectileCount}" : "";
            string extra = w.explosionRadius > 0f ? " · 폭발" : w.pierce > 0 ? $" · 관통 {w.pierce}" : "";
            return $"피해 {w.damage:0.#}{count} · 초당 {w.ShotsPerSecond:0.#}{extra}";
        }

        // ─────────────── 퓨전 도감 ───────────────

        void BuildCodex()
        {
            var all = Fusions.All;
            for (int i = 0; i < all.Length; i++)
            {
                var f = all[i];
                bool found = MetaProgress.IsDiscovered(f.Id);
                int col = i % 2, row = i / 2;
                var card = Card($"Fusion_{f.Id}", new Vector2((col - 0.5f) * 850f, 240f - row * 150f), new Vector2(830f, 136f),
                    found ? Palette.Fusion : Muted);
                AddText(card, found ? $"★ {f.Name}" : "???", new Vector2(28f, 38f), new Vector2(780f, 44f), 32, found ? Palette.Fusion : Muted);
                // Undiscovered entries hint at half of the recipe.
                string recipe = found ? f.Recipe : HalfRecipe(f.Recipe);
                AddText(card, recipe, new Vector2(28f, -4f), new Vector2(780f, 34f), 24, found ? Palette.Text : Muted);
                AddText(card, found ? f.Effect : "아직 발견하지 못한 퓨전", new Vector2(28f, -42f), new Vector2(780f, 34f), 22, SubText);
            }
            var footer = UIFactory.Rect("CodexCount", content, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -330f), new Vector2(800f, 40f));
            UIFactory.Label(footer, $"발견 {MetaProgress.DiscoveredCount} / {all.Length}", 26, Palette.Fusion);
        }

        static string HalfRecipe(string recipe)
        {
            int plus = recipe.IndexOf(" + ", System.StringComparison.Ordinal);
            return plus > 0 ? recipe.Substring(0, plus) + " + ???" : "???";
        }

        // ─────────────── 시작 무기 ───────────────

        void BuildStartWeapons()
        {
            string current = MetaProgress.StartWeaponId;
            if (string.IsNullOrEmpty(current) && gm.DefaultStartingWeapon) current = gm.DefaultStartingWeapon.id;

            var options = new List<WeaponData>();
            foreach (var w in gm.Database.weapons) if (w && w.tier <= 2) options.Add(w);

            const int columns = 4;
            for (int i = 0; i < options.Count; i++)
            {
                var w = options[i];
                int col = i % columns, row = i / columns;
                var color = WeaponTags.ColorOf(w.tag);
                bool available = GameManager.IsStartWeaponOption(w);
                bool selected = available && w.id == current;
                var card = Card($"Start_{w.id}", new Vector2((col - 1.5f) * 425f, 230f - row * 130f), new Vector2(410f, 116f), available ? color : Muted);
                AddText(card, w.displayName, new Vector2(24f, 20f), new Vector2(260f, 40f), 28, available ? color : Muted);
                AddText(card, $"{WeaponTags.KoreanName(w.tag)} · T{w.tier}", new Vector2(24f, -22f), new Vector2(260f, 30f), 20, SubText);
                string id = $"Pick_{w.id}";
                var weaponId = w.id;
                CardButton(card, id, selected ? "선택됨" : available ? "선택" : "잠김", new Vector2(135f, 0f), new Vector2(120f, 60f),
                    available && !selected, () =>
                    {
                        MetaProgress.StartWeaponId = weaponId;
                        Feedback(true);
                        Rebuild(id);
                    });
            }
            var note = UIFactory.Rect("StartNote", content, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -300f), new Vector2(1400f, 40f));
            UIFactory.Label(note, "티어 1~2 무기 중 해금한 무기로 시작할 수 있습니다. 부품 해금 탭에서 무기를 해금하세요.", 24, SubText);
        }
    }
}
