using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CoreOverclock
{
    /// <summary>암시장 window opened from the 코어 작업실 when the smuggler is in.</summary>
    public class BlackMarketPanel : MonoBehaviour
    {
        class Card
        {
            public Text Name, Kind, Body, Price, BuyLabel;
            public Button Buy;
            public Image Frame;
        }

        static readonly Color CardColor = new(0.09f, 0.05f, 0.13f, 1f);
        static readonly Color Muted = new(0.45f, 0.4f, 0.55f);

        GameManager gm;
        Shop shop;
        GameObject root;
        Text scrapLabel;
        Button closeButton;
        readonly List<Card> cards = new();
        int closedFrame = -1;
        System.Action onClose;

        public bool IsOpen => root.activeSelf;
        /// <summary>True on the frame Esc closed the window, so nothing else reacts to the same press.</summary>
        public bool ConsumedInputThisFrame => closedFrame == Time.frameCount;

        public static BlackMarketPanel Create(Canvas canvas, GameManager gm, Shop shop)
        {
            var p = canvas.gameObject.AddComponent<BlackMarketPanel>();
            p.gm = gm;
            p.shop = shop;
            p.Build(canvas.transform);
            shop.Changed += p.Refresh;
            return p;
        }

        void Build(Transform canvas)
        {
            var dim = UIFactory.Stretch("BlackMarket", canvas);
            UIFactory.Image(dim, new Color(0f, 0f, 0.01f, 0.85f)).raycastTarget = true;
            root = dim.gameObject;
            var box = UIFactory.Rect("Box", dim, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1560f, 800f));
            UIFactory.Image(box, new Color(0.04f, 0.025f, 0.07f, 1f));
            UIFactory.Image(UIFactory.Rect("Top", box, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(1560f, 6f)), Palette.BlackMarket);
            Vector2 top = new(0.5f, 1f), tr = new(1f, 1f);

            UIFactory.Label(UIFactory.Rect("Title", box, top, top, new Vector2(0f, -48f), new Vector2(1000f, 70f)), "암시장", 56, Palette.BlackMarket);
            UIFactory.Label(UIFactory.Rect("Subtitle", box, top, top, new Vector2(0f, -128f), new Vector2(1400f, 34f)),
                "강력하지만 대가가 따르는 금지 부품. 이번 정비 시간에만 거래할 수 있습니다.", 24, new Color(0.75f, 0.68f, 0.85f));
            scrapLabel = UIFactory.Label(UIFactory.Rect("Scrap", box, tr, tr, new Vector2(-40f, -40f), new Vector2(300f, 50f)), "", 40, Palette.Scrap, TextAnchor.MiddleRight);

            for (int i = 0; i < BlackMarket.OfferCount; i++)
            {
                int index = i;
                cards.Add(BuildCard(box, new Vector2((i - 1) * 490f, -30f), () =>
                {
                    bool ok = shop.BuyMarket(index);
                    AudioManager.Play(ok ? SfxId.Buy : SfxId.Deny, 0.5f, 0f);
                }));
            }
            closeButton = UIFactory.Button(box, "나가기 (Esc)", new Vector2(0f, -330f), new Vector2(360f, 68f), Close);
            root.SetActive(false);
        }

        Card BuildCard(Transform parent, Vector2 pos, UnityEngine.Events.UnityAction buy)
        {
            var card = new Card();
            var rt = UIFactory.Rect("Card", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(460f, 440f));
            card.Frame = UIFactory.Image(rt, CardColor);
            UIFactory.Image(UIFactory.Rect("Header", rt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(460f, 6f)), Palette.BlackMarket);
            Vector2 top = new(0.5f, 1f), bottom = new(0.5f, 0f);
            card.Name = UIFactory.Label(UIFactory.Rect("Name", rt, top, top, new Vector2(0f, -40f), new Vector2(430f, 44f)), "", 30, Palette.Text);
            card.Kind = UIFactory.Label(UIFactory.Rect("Kind", rt, top, top, new Vector2(0f, -82f), new Vector2(430f, 30f)), "", 22, Palette.BlackMarket);
            card.Body = UIFactory.Label(UIFactory.Rect("Body", rt, top, top, new Vector2(0f, -116f), new Vector2(410f, 190f)), "", 22,
                new Color(0.82f, 0.85f, 0.95f), TextAnchor.UpperLeft);
            card.Body.horizontalOverflow = HorizontalWrapMode.Wrap;
            card.Body.lineSpacing = 1.15f;
            card.Price = UIFactory.Label(UIFactory.Rect("Price", rt, bottom, bottom, new Vector2(0f, 96f), new Vector2(430f, 40f)), "", 32, Palette.Scrap);
            card.Buy = UIFactory.Button(rt, "거래", new Vector2(0f, -176f), new Vector2(300f, 56f), buy);
            card.BuyLabel = card.Buy.GetComponentInChildren<Text>();
            return card;
        }

        public void Open(System.Action closed)
        {
            onClose = closed;
            root.SetActive(true);
            root.transform.SetAsLastSibling();
            AudioManager.Play(SfxId.Overclock, 0.4f, 0f);
            Refresh();
            if (EventSystem.current) EventSystem.current.SetSelectedGameObject((cards.Count > 0 && cards[0].Buy.interactable ? cards[0].Buy : closeButton).gameObject);
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
            if (!IsOpen) return;
            scrapLabel.text = $"◆ {gm.Scrap}";
            if (GameInput.PausePressed) Close();
        }

        void Refresh()
        {
            if (!root || !IsOpen) return;
            scrapLabel.text = $"◆ {gm.Scrap}";
            for (int i = 0; i < cards.Count; i++)
            {
                var card = cards[i];
                bool exists = i < shop.MarketOffers.Count;
                card.Frame.transform.gameObject.SetActive(exists);
                if (!exists) continue;
                var o = shop.MarketOffers[i];
                string blocked = shop.MarketBlockReason(i);

                if (o.IsMod)
                {
                    var w = o.Target;
                    card.Name.text = "불법 개조";
                    card.Kind.text = "무기 등급 즉시 상승";
                    int from = o.Sold ? w.Grade - 1 : w.Grade;
                    card.Body.text =
                        $"{w.Data.displayName}  <color=#{WeaponGrades.Hex(from)}>{WeaponGrades.Roman(from)}</color> → <color=#{WeaponGrades.Hex(from + 1)}>{WeaponGrades.Roman(from + 1)}</color>\n" +
                        $"{WeaponGrades.Describe(from + 1)}\n" +
                        $"<color=#FF6B6B>대가: 최대 HP -{BlackMarket.ModHpCost:0} (이번 판 동안)</color>\n" +
                        "<color=#7F8AA8>정식 강화보다 훨씬 싸다.</color>";
                }
                else
                {
                    card.Name.text = o.Chip.displayName;
                    card.Kind.text = "금지 칩셋";
                    var sb = new StringBuilder();
                    foreach (var m in o.Chip.modifiers)
                        sb.Append(m.IsDrawback ? $"<color=#FF6B6B>{m.Describe()}</color>\n" : $"{m.Describe()}\n");
                    if (!string.IsNullOrEmpty(o.Chip.description)) sb.Append($"<color=#7F8AA8>{o.Chip.description}</color>");
                    card.Body.text = sb.ToString();
                }
                card.Name.color = o.Sold ? Muted : Palette.Text;
                card.Price.text = o.Sold ? "거래 완료" : $"◆ {o.Price}";
                card.Price.color = o.Sold ? Muted : gm.Scrap >= o.Price ? Palette.Scrap : Palette.Danger;
                card.Buy.interactable = blocked == null;
                card.BuyLabel.text = blocked ?? "거래";
            }
        }
    }
}
