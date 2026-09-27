using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CoreOverclock
{
    /// <summary>코어 작업실 화면: offers, reroll, lock, owned weapons (sell), chips, synergies and stats.</summary>
    public class ShopUI : MonoBehaviour
    {
        class Card
        {
            public Image Header, Frame;
            public Text Name, Kind, Body, Price, BuyLabel, LockLabel;
            public Button Buy, Lock;
        }

        class Slot
        {
            public Image Frame, Bar;
            public Text Name, Info, SellLabel;
            public Button Sell;
        }

        GameManager gm;
        Shop shop;
        Loadout loadout;
        GameObject root;
        Text title, summary, scrapLabel, rerollLabel, slotsTitle, chipsText, synergyText, statsText;
        Button rerollButton, nextButton;
        readonly List<Card> cards = new();
        readonly List<Slot> slots = new();
        string summaryText;
        float fusionNoticeTime;

        public bool IsOpen => root.activeSelf;

        public static ShopUI Create(Canvas canvas, GameManager gm, Shop shop, Loadout loadout)
        {
            var ui = canvas.gameObject.AddComponent<ShopUI>();
            ui.gm = gm;
            ui.shop = shop;
            ui.loadout = loadout;
            ui.Build(canvas.transform);
            shop.Changed += ui.Refresh;
            loadout.Changed += ui.Refresh;
            loadout.FusionActivated += ui.OnFusionActivated;
            return ui;
        }

        void Build(Transform canvas)
        {
            var dim = UIFactory.Stretch("ShopScreen", canvas);
            var dimImage = UIFactory.Image(dim, new Color(0.02f, 0.02f, 0.05f, 1f));
            dimImage.raycastTarget = true;
            root = dim.gameObject;
            Vector2 c = new(0.5f, 0.5f), top = new(0.5f, 1f);

            title = UIFactory.Label(UIFactory.Rect("Title", dim, top, top, new Vector2(0f, -30f), new Vector2(1200f, 70f)),
                "코어 작업실", 56, Palette.Player);
            summary = UIFactory.Label(UIFactory.Rect("Summary", dim, top, top, new Vector2(0f, -100f), new Vector2(1400f, 40f)),
                "", 26, new Color(0.7f, 0.78f, 0.9f));
            scrapLabel = UIFactory.Label(UIFactory.Rect("Scrap", dim, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-48f, -40f), new Vector2(400f, 60f)),
                "◆ 0", 48, Palette.Scrap, TextAnchor.MiddleRight);

            // Offer cards
            float spacing = shop.SlotCount > 4 ? 380f : 400f;
            for (int i = 0; i < shop.SlotCount; i++)
            {
                int index = i;
                float x = (i - (shop.SlotCount - 1) * 0.5f) * spacing;
                cards.Add(BuildCard(dim, new Vector2(x, 170f), () => Feedback(shop.Buy(index)), () => shop.ToggleLock(index)));
            }

            rerollButton = UIFactory.Button(dim, "리롤", new Vector2(-300f, -105f), new Vector2(360f, 70f), () => Feedback(shop.Reroll()));
            rerollLabel = rerollButton.GetComponentInChildren<Text>();
            nextButton = UIFactory.Button(dim, "다음 웨이브 ▶", new Vector2(300f, -105f), new Vector2(360f, 70f), gm.NextWave);

            // Owned weapons
            slotsTitle = UIFactory.Label(UIFactory.Rect("SlotsTitle", dim, c, c, new Vector2(-542f, -180f), new Vector2(400f, 36f)),
                "장착 무기", 28, Palette.Text, TextAnchor.MiddleLeft);
            for (int i = 0; i < Player.MaxWeapons; i++)
            {
                int index = i;
                float x = -625f + i * 250f;
                slots.Add(BuildSlot(dim, new Vector2(x, -270f), () => Feedback(shop.Sell(index))));
            }

            synergyText = UIFactory.Label(UIFactory.Rect("Synergy", dim, c, c, new Vector2(-390f, -427f), new Vector2(1100f, 150f)),
                "", 21, Palette.Text, TextAnchor.UpperLeft);
            synergyText.lineSpacing = 1.02f;
            statsText = UIFactory.Label(UIFactory.Rect("Stats", dim, c, c, new Vector2(560f, -435f), new Vector2(760f, 150f)),
                "", 22, Palette.Text, TextAnchor.UpperLeft);
            statsText.lineSpacing = 1.15f;
            chipsText = UIFactory.Label(UIFactory.Rect("Chips", dim, c, c, new Vector2(0f, -205f), new Vector2(1000f, 36f)),
                "", 22, Palette.Chip, TextAnchor.MiddleRight);
            chipsText.rectTransform.anchoredPosition = new Vector2(242f, -180f);

            root.SetActive(false);
        }

        Card BuildCard(Transform parent, Vector2 pos, UnityEngine.Events.UnityAction buy, UnityEngine.Events.UnityAction toggleLock)
        {
            var card = new Card();
            var rt = UIFactory.Rect("Card", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(370f, 430f));
            card.Frame = UIFactory.Image(rt, new Color(0.07f, 0.09f, 0.15f, 1f));
            var header = UIFactory.Rect("Header", rt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(370f, 8f));
            card.Header = UIFactory.Image(header, Color.white);

            card.Name = UIFactory.Label(UIFactory.Rect("Name", rt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -38f), new Vector2(340f, 44f)),
                "", 32, Palette.Text);
            card.Kind = UIFactory.Label(UIFactory.Rect("Kind", rt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -80f), new Vector2(340f, 30f)),
                "", 22, Palette.Text);
            card.Body = UIFactory.Label(UIFactory.Rect("Body", rt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(330f, 200f)),
                "", 21, new Color(0.8f, 0.85f, 0.95f), TextAnchor.UpperLeft);
            card.Body.horizontalOverflow = HorizontalWrapMode.Wrap;
            card.Body.lineSpacing = 1.1f;
            card.Price = UIFactory.Label(UIFactory.Rect("Price", rt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 88f), new Vector2(340f, 40f)),
                "", 32, Palette.Scrap);

            card.Buy = UIFactory.Button(rt, "구매", new Vector2(-45f, -178f), new Vector2(250f, 56f), buy);
            card.BuyLabel = card.Buy.GetComponentInChildren<Text>();
            card.Lock = UIFactory.Button(rt, "잠금", new Vector2(130f, -178f), new Vector2(90f, 56f), toggleLock);
            card.LockLabel = card.Lock.GetComponentInChildren<Text>();
            card.LockLabel.fontSize = 24;
            return card;
        }

        Slot BuildSlot(Transform parent, Vector2 pos, UnityEngine.Events.UnityAction sell)
        {
            var slot = new Slot();
            var rt = UIFactory.Rect("WeaponSlot", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(235f, 140f));
            slot.Frame = UIFactory.Image(rt, new Color(0.07f, 0.09f, 0.15f, 1f));
            var bar = UIFactory.Rect("Bar", rt, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(6f, 140f));
            slot.Bar = UIFactory.Image(bar, Color.white);
            slot.Name = UIFactory.Label(UIFactory.Rect("Name", rt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(220f, 34f)),
                "", 24, Palette.Text);
            slot.Info = UIFactory.Label(UIFactory.Rect("Info", rt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -54f), new Vector2(220f, 28f)),
                "", 18, new Color(0.7f, 0.75f, 0.85f));
            slot.Sell = UIFactory.Button(rt, "판매", new Vector2(0f, -38f), new Vector2(200f, 46f), sell);
            slot.SellLabel = slot.Sell.GetComponentInChildren<Text>();
            slot.SellLabel.fontSize = 22;
            return slot;
        }

        public void Show(int clearedWave, int kills, int scrapGained, int clearBonus)
        {
            summaryText = clearedWave > 0
                ? $"WAVE {clearedWave} 클리어  ·  처치 {kills}  ·  스크랩 +{scrapGained} (클리어 보너스 {clearBonus} 포함)   →   다음: WAVE {clearedWave + 1}"
                : "";
            summary.text = summaryText;
            summary.color = new Color(0.7f, 0.78f, 0.9f);
            fusionNoticeTime = 0f;
            root.SetActive(true);
            root.transform.SetAsLastSibling();
            Refresh();
            if (EventSystem.current) EventSystem.current.SetSelectedGameObject(nextButton.gameObject);
        }

        public void Hide() => root.SetActive(false);

        static void Feedback(bool success) => AudioManager.Play(success ? SfxId.Buy : SfxId.Deny, 0.5f, 0f);

        void Update()
        {
            if (!IsOpen) return;
            scrapLabel.text = $"◆ {gm.Scrap}";
            if (fusionNoticeTime > 0f && (fusionNoticeTime -= Time.unscaledDeltaTime) <= 0f)
            {
                summary.text = summaryText;
                summary.color = new Color(0.7f, 0.78f, 0.9f);
            }
        }

        void OnFusionActivated(FusionId id)
        {
            if (!IsOpen) return;
            var f = Fusions.Get(id);
            summary.text = $"★ 퓨전 완성!  {f.Name}  —  {f.Effect}";
            summary.color = Palette.Fusion;
            fusionNoticeTime = 3.5f;
        }

        public void Refresh()
        {
            if (!root || !root.activeSelf) return;
            scrapLabel.text = $"◆ {gm.Scrap}";

            for (int i = 0; i < cards.Count; i++) RefreshCard(cards[i], shop.Offers[i], shop.BlockReason(i));

            rerollLabel.text = $"리롤  ◆ {shop.RerollCost}";
            rerollButton.interactable = gm.Scrap >= shop.RerollCost;

            slotsTitle.text = $"장착 무기  {loadout.Weapons.Count} / {Player.MaxWeapons}";
            for (int i = 0; i < slots.Count; i++)
            {
                var s = slots[i];
                if (i < loadout.Weapons.Count)
                {
                    var w = loadout.Weapons[i];
                    var col = WeaponTags.ColorOf(w.Data.tag);
                    s.Bar.color = col;
                    s.Name.text = w.Data.displayName;
                    s.Name.color = col;
                    s.Info.text = $"{WeaponTags.KoreanName(w.Data.tag)} · 구매가 ◆{w.PaidPrice}";
                    s.Sell.gameObject.SetActive(true);
                    s.Sell.interactable = shop.CanSell(i);
                    s.SellLabel.text = shop.CanSell(i) ? $"판매  +◆{Shop.SellValue(w)}" : "마지막 무기";
                }
                else
                {
                    s.Bar.color = new Color(0.2f, 0.22f, 0.3f);
                    s.Name.text = "빈 슬롯";
                    s.Name.color = new Color(0.4f, 0.45f, 0.55f);
                    s.Info.text = "";
                    s.Sell.gameObject.SetActive(false);
                }
            }

            chipsText.text = loadout.Chips.Count == 0 ? "칩셋 없음" : "칩셋: " + ChipSummary();

            var syn = loadout.Synergy;
            var sb = new StringBuilder("<b>시너지</b>\n");
            foreach (WeaponTag tag in System.Enum.GetValues(typeof(WeaponTag))) sb.Append(syn.Describe(tag)).Append('\n');
            sb.Append(FusionSummary());
            synergyText.text = sb.ToString();
            statsText.text = StatsSummary();
        }

        void RefreshCard(Card card, ShopOffer o, string blocked)
        {
            bool weapon = o.IsWeapon;
            var color = weapon ? WeaponTags.ColorOf(o.Weapon.tag) : Palette.Chip;
            card.Header.color = color;
            card.Name.text = o.Name;
            card.Name.color = o.Sold ? new Color(0.4f, 0.45f, 0.55f) : color;
            card.Kind.text = weapon ? $"무기 · {WeaponTags.KoreanName(o.Weapon.tag)} · T{o.Weapon.tier}" : $"패시브 칩셋 · T{o.Chip.tier}";
            card.Body.text = o.Sold ? "" : weapon ? FusionPreview(o.Weapon) + WeaponBody(o.Weapon) : ChipBody(o.Chip);
            card.Price.text = o.Sold ? "SOLD" : $"◆ {o.Price}";
            card.Price.color = o.Sold ? new Color(0.4f, 0.45f, 0.55f) : gm.Scrap >= o.Price ? Palette.Scrap : Palette.Danger;

            card.Buy.interactable = blocked == null;
            card.BuyLabel.text = blocked ?? "구매";
            card.Lock.interactable = !o.Sold;
            card.LockLabel.text = o.Locked ? "잠김" : "잠금";
            card.Frame.color = o.Locked ? new Color(0.16f, 0.14f, 0.05f, 1f) : new Color(0.07f, 0.09f, 0.15f, 1f);
        }

        string FusionPreview(WeaponData w)
        {
            var completed = loadout.FusionsCompletedBy(w);
            if (completed.Count == 0) return "";
            var names = new List<string>();
            foreach (var id in completed) names.Add(Fusions.Get(id).Name);
            return $"<color=#{ColorUtility.ToHtmlStringRGB(Palette.Fusion)}>★ 퓨전 완성: {string.Join(", ", names)}</color>\n";
        }

        string FusionSummary()
        {
            var hex = ColorUtility.ToHtmlStringRGB(Palette.Fusion);
            if (loadout.ActiveFusions.Count == 0) return $"<color=#{hex}>퓨전</color>  <color=#5A6378>특정 무기 2개를 함께 장착하면 발동</color>";
            var names = new List<string>();
            foreach (var f in Fusions.All) if (loadout.Has(f.Id)) names.Add(f.Name);
            return $"<color=#{hex}>퓨전  {string.Join(" · ", names)}</color>";
        }

        static string WeaponBody(WeaponData w)
        {
            var sb = new StringBuilder();
            string count = w.projectileCount > 1 ? $" x{w.projectileCount}" : "";
            sb.Append($"피해 {w.damage:0.#}{count}  ·  초당 {w.ShotsPerSecond:0.#}발\n");
            sb.Append($"사거리 {w.range:0.#}  ·  발열 {w.heatPerShot * w.ShotsPerSecond:0.#}/s\n");
            if (w.pierce > 0) sb.Append($"관통 {w.pierce}\n");
            if (w.slowAmount > 0f) sb.Append($"둔화 {w.slowAmount * 100f:0}% ({w.slowDuration:0.#}s)\n");
            if (w.burnDamagePerSecond > 0f) sb.Append($"도트 {w.burnDamagePerSecond:0.#}/s ({w.burnDuration:0.#}s)\n");
            if (w.explosionRadius > 0f) sb.Append($"폭발 반경 {w.explosionRadius:0.#}\n");
            if (!string.IsNullOrEmpty(w.description)) sb.Append($"<color=#7F8AA8>{w.description}</color>");
            return sb.ToString();
        }

        static string ChipBody(ChipData c)
        {
            var sb = new StringBuilder();
            foreach (var m in c.modifiers)
            {
                bool bad = m.IsDrawback;
                sb.Append(bad ? $"<color=#FF6B6B>{m.Describe()}</color>\n" : $"{m.Describe()}\n");
            }
            if (!string.IsNullOrEmpty(c.description)) sb.Append($"<color=#7F8AA8>{c.description}</color>");
            return sb.ToString();
        }

        string ChipSummary()
        {
            var counts = new Dictionary<string, int>();
            foreach (var chip in loadout.Chips)
                counts[chip.displayName] = counts.TryGetValue(chip.displayName, out var n) ? n + 1 : 1;
            var parts = new List<string>();
            foreach (var kv in counts) parts.Add(kv.Value > 1 ? $"{kv.Key} x{kv.Value}" : kv.Key);
            return string.Join(", ", parts);
        }

        string StatsSummary()
        {
            var s = loadout.Stats;
            float heatGen = 0f;
            foreach (var w in loadout.Weapons) heatGen += w.Data.heatPerShot * w.Data.ShotsPerSecond * (1f + s.FireRatePct);
            heatGen *= loadout.HeatGenMultiplier;
            string P(float v) => (v >= 0 ? "+" : "") + Mathf.RoundToInt(v * 100f) + "%";
            return "<b>코어 상태</b>\n" +
                   $"최대 HP {gm.Player.MaxHP:0}   이동 {P(s.MoveSpeedPct)}   공격력 {P(s.DamagePct)}\n" +
                   $"연사 {P(s.FireRatePct)}   치명타 {P(s.CritChance)}   발열 배율 x{loadout.HeatGenMultiplier:0.00}\n" +
                   $"<color=#FF9A3C>예상 발열 {heatGen:0.#}/s</color>  (냉각 {4f + s.CoolingFlat:0.#}/s + 열의 10%)";
        }
    }
}
