using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CoreOverclock
{
    public class HUD : MonoBehaviour
    {
        GameManager gm;
        Text waveLabel, timerLabel, hpLabel, scrapLabel, killLabel, banner;
        Text heatLabel, heatState, ventLabel;
        RectTransform hpFill, heatFill;
        Image heatFillImage, ventFillImage;
        RectTransform ventFill;
        GameObject bossBar;
        RectTransform bossFill;
        Text bossLabel;
        GameObject gameOverPanel, pausePanel;
        Text gameOverTitle, gameOverStats;
        Button restartButton, resumeButton;
        float bannerTimer, toastTimer, hintTimer;
        Text hint, weaponStrip;
        RectTransform gameplayRoot;
        Text toast;
        SettingsPanel settings;

        public Canvas Canvas { get; private set; }

        public static HUD Create(GameManager gm)
        {
            UIFactory.EnsureEventSystem();
            var canvas = UIFactory.CreateCanvas("HUD", 10);
            var hud = canvas.gameObject.AddComponent<HUD>();
            hud.gm = gm;
            hud.Canvas = canvas;
            hud.Build();
            return hud;
        }

        void Build()
        {
            // Gameplay widgets live under one container so the title screen can hide them together.
            gameplayRoot = UIFactory.Stretch("Gameplay", Canvas.transform);
            var root = (Transform)gameplayRoot;
            Vector2 top = new(0.5f, 1f), topLeft = new(0f, 1f), topRight = new(1f, 1f), bottom = new(0.5f, 0f);

            waveLabel = UIFactory.Label(UIFactory.Rect("Wave", root, topRight, topRight, new Vector2(-32f, -32f), new Vector2(420f, 40f)),
                "WAVE 1", 34, new Color(0.6f, 0.8f, 1f), TextAnchor.MiddleRight);
            timerLabel = UIFactory.Label(UIFactory.Rect("Timer", root, top, top, new Vector2(0f, -12f), new Vector2(300f, 76f)),
                "30", 64, Palette.Text);

            // HP bar
            var hpBack = UIFactory.Rect("HPBack", root, topLeft, topLeft, new Vector2(32f, -32f), new Vector2(420f, 38f));
            UIFactory.Image(hpBack, new Color(0.15f, 0.05f, 0.08f, 0.9f));
            hpFill = UIFactory.Stretch("HPFill", hpBack);
            UIFactory.Image(hpFill, Palette.Danger);
            hpLabel = UIFactory.Label(UIFactory.Stretch("HPText", hpBack), "30 / 30", 26, Color.white);

            scrapLabel = UIFactory.Label(UIFactory.Rect("Scrap", root, topLeft, topLeft, new Vector2(32f, -82f), new Vector2(420f, 40f)),
                "◆ 0", 32, Palette.Scrap, TextAnchor.MiddleLeft);
            killLabel = UIFactory.Label(UIFactory.Rect("Kills", root, topLeft, topLeft, new Vector2(32f, -122f), new Vector2(420f, 34f)),
                "처치 0", 24, new Color(0.7f, 0.75f, 0.85f), TextAnchor.MiddleLeft);

            banner = UIFactory.Label(UIFactory.Rect("Banner", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 180f), new Vector2(1200f, 120f)),
                "", 80, Color.white);
            BuildHeatGauge(root, bottom);
            BuildBossBar(root, top);

            hint = UIFactory.Label(UIFactory.Rect("Hint", root, bottom, bottom, new Vector2(0f, 150f), new Vector2(1500f, 40f)),
                "", 26, new Color(0.85f, 0.95f, 1f));
            hint.gameObject.SetActive(false);
            weaponStrip = UIFactory.Label(UIFactory.Rect("Weapons", root, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(32f, 30f), new Vector2(380f, 36f)),
                "", 26, Palette.Text, TextAnchor.MiddleLeft);
            weaponStrip.rectTransform.pivot = new Vector2(0f, 0.5f);
            gm.Loadout.Changed += RefreshWeaponStrip;
            RefreshWeaponStrip();

            gameOverPanel = BuildPanel("GameOverPanel", out gameOverTitle, out gameOverStats);
            restartButton = UIFactory.Button(gameOverPanel.transform, "다시 시작", new Vector2(-220f, -170f), new Vector2(400f, 80f), gm.Restart);
            UIFactory.Button(gameOverPanel.transform, "타이틀로", new Vector2(220f, -170f), new Vector2(400f, 80f), gm.ReturnToTitle);

            pausePanel = BuildPanel("PausePanel", out var pauseTitle, out var pauseStats);
            pauseTitle.text = "일시정지";
            pauseStats.text = "<size=24>이동: WASD / 방향키 / 좌스틱\n긴급 방열: Space / 우클릭 / A·RB\n일시정지: ESC / Start</size>";
            pauseStats.rectTransform.anchoredPosition = new Vector2(0f, -120f);
            resumeButton = UIFactory.Button(pausePanel.transform, "계속하기", new Vector2(0f, -40f), new Vector2(420f, 80f), () => gm.SetPaused(false));
            UIFactory.Button(pausePanel.transform, "설정", new Vector2(0f, -130f), new Vector2(420f, 72f),
                () => settings.Open(() => pausePanel.SetActive(false), () => Show(pausePanel, resumeButton)));
            UIFactory.Button(pausePanel.transform, "타이틀로", new Vector2(0f, -215f), new Vector2(420f, 72f), gm.ReturnToTitle);

            toast = UIFactory.Label(UIFactory.Rect("Toast", root, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-32f, -80f), new Vector2(700f, 44f)),
                "", 28, Palette.Scrap, TextAnchor.MiddleRight);
        }

        void BuildBossBar(Transform root, Vector2 top)
        {
            var back = UIFactory.Rect("BossBar", root, top, top, new Vector2(0f, -122f), new Vector2(900f, 30f));
            UIFactory.Image(back, new Color(0.15f, 0.03f, 0.06f, 0.85f));
            bossFill = UIFactory.Stretch("BossFill", back);
            UIFactory.Image(bossFill, new Color(1f, 0.2f, 0.35f, 0.9f));
            bossLabel = UIFactory.Label(UIFactory.Stretch("BossName", back), "", 22, Color.white);
            bossBar = back.gameObject;
            bossBar.SetActive(false);
        }

        void BuildHeatGauge(Transform root, Vector2 bottom)
        {
            // Heat gauge (기획서 2장) sits under the arena; the 70% mark separates safe/overclock.
            const float width = 760f;
            heatLabel = UIFactory.Label(UIFactory.Rect("HeatLabel", root, bottom, bottom, new Vector2(-width / 2f - 70f, 30f), new Vector2(120f, 36f)),
                "HEAT", 26, Palette.Overclock, TextAnchor.MiddleRight);
            var back = UIFactory.Rect("HeatBack", root, bottom, bottom, new Vector2(0f, 30f), new Vector2(width, 26f));
            UIFactory.Image(back, new Color(0.08f, 0.08f, 0.12f, 0.95f));
            heatFill = UIFactory.Stretch("HeatFill", back);
            heatFillImage = UIFactory.Image(heatFill, Palette.Player);
            var mark = UIFactory.Rect("OverclockMark", back, new Vector2(HeatSystem.OverclockThreshold / HeatSystem.Max, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(3f, 34f));
            UIFactory.Image(mark, Color.white);
            heatState = UIFactory.Label(UIFactory.Rect("HeatState", root, bottom, bottom, new Vector2(width / 2f + 110f, 30f), new Vector2(200f, 36f)),
                "안전", 24, Palette.Text, TextAnchor.MiddleLeft);

            var ventBack = UIFactory.Rect("VentBack", root, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-32f, 20f), new Vector2(300f, 44f));
            UIFactory.Image(ventBack, new Color(0.08f, 0.1f, 0.16f, 0.95f));
            ventFill = UIFactory.Stretch("VentFill", ventBack);
            ventFillImage = UIFactory.Image(ventFill, new Color(0.3f, 0.8f, 1f, 0.45f));
            ventLabel = UIFactory.Label(UIFactory.Stretch("VentLabel", ventBack), "", 22, Palette.Text);
        }

        GameObject BuildPanel(string name, out Text title, out Text stats)
        {
            var dim = UIFactory.Stretch(name, Canvas.transform);
            var dimImage = UIFactory.Image(dim, new Color(0f, 0f, 0.02f, 0.72f));
            dimImage.raycastTarget = true;
            var box = UIFactory.Rect("Box", dim, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 560f));
            UIFactory.Image(box, new Color(0.05f, 0.07f, 0.12f, 0.95f));
            var frame = UIFactory.Rect("Frame", box, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(900f, 6f));
            UIFactory.Image(frame, Palette.Player);
            title = UIFactory.Label(UIFactory.Rect("Title", box, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(860f, 90f)),
                "", 64, Palette.Player);
            stats = UIFactory.Label(UIFactory.Rect("Stats", box, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(820f, 200f)),
                "", 30, Palette.Text, TextAnchor.UpperCenter);
            stats.lineSpacing = 1.3f;
            dim.gameObject.SetActive(false);
            return dim.gameObject;
        }

        void Update()
        {
            if (!gm || !gm.Player) return;
            bool inGame = gm.State != GameState.Title;
            if (gameplayRoot.gameObject.activeSelf != inGame) gameplayRoot.gameObject.SetActive(inGame);
            if (!inGame) return;

            waveLabel.text = $"WAVE {gm.Wave} / {gm.TotalWaves}";
            int seconds = Mathf.CeilToInt(Mathf.Max(0f, gm.TimeLeft));
            timerLabel.text = seconds.ToString();
            bool urgent = gm.State == GameState.Combat && gm.TimeLeft <= 5f;
            timerLabel.color = urgent ? Palette.Danger : Palette.Text;
            timerLabel.rectTransform.localScale = Vector3.one * (urgent ? 1f + 0.12f * Mathf.Abs(Mathf.Sin(Time.time * 6f)) : 1f);

            float ratio = gm.Player.MaxHP > 0f ? gm.Player.HP / gm.Player.MaxHP : 0f;
            hpFill.anchorMax = new Vector2(ratio, 1f);
            hpLabel.text = $"{Mathf.CeilToInt(gm.Player.HP)} / {Mathf.CeilToInt(gm.Player.MaxHP)}";
            scrapLabel.text = $"◆ {gm.Scrap}";
            killLabel.text = $"처치 {gm.TotalKills}";

            UpdateHeat();

            var boss = gm.State == GameState.Combat ? gm.Boss : null;
            bossBar.SetActive(boss);
            if (boss)
            {
                float r = Mathf.Clamp01(boss.HP / boss.MaxHP);
                bossFill.anchorMax = new Vector2(r, 1f);
                bossLabel.text = $"{boss.Data.displayName}   {Mathf.CeilToInt(r * 100f)}%";
            }

            if (hintTimer > 0f)
            {
                hintTimer -= Time.deltaTime;
                var hc = hint.color;
                hc.a = Mathf.Clamp01(hintTimer / 0.6f);
                hint.color = hc;
                if (hintTimer <= 0f) hint.gameObject.SetActive(false);
            }

            toastTimer -= Time.unscaledDeltaTime;
            var tc = toast.color;
            tc.a = Mathf.Clamp01(toastTimer / 0.5f);
            toast.color = tc;

            if (bannerTimer > 0f)
            {
                bannerTimer -= Time.unscaledDeltaTime;
                var c = banner.color;
                c.a = Mathf.Clamp01(bannerTimer / 0.4f);
                banner.color = c;
            }
        }

        void UpdateHeat()
        {
            var heat = gm.Heat;
            heatFill.anchorMax = new Vector2(heat.Ratio, 1f);
            string pct = $"{Mathf.RoundToInt(heat.Value)}%";
            switch (heat.State)
            {
                case HeatState.Meltdown:
                    bool on = Mathf.Repeat(Time.unscaledTime * 5f, 1f) > 0.5f;
                    heatFillImage.color = on ? Palette.Danger : Color.white;
                    heatState.text = $"<color=#FF4050>과열! {heat.MeltdownTimeLeft:0.0}s</color>";
                    break;
                case HeatState.Overclock:
                    heatFillImage.color = Color.Lerp(Palette.Overclock, Color.white, 0.25f * Mathf.Abs(Mathf.Sin(Time.time * 8f)));
                    heatState.text = $"<color=#FF9A3C>오버클럭 {pct}</color>";
                    break;
                default:
                    heatFillImage.color = Color.Lerp(Palette.Player, Palette.Overclock, heat.Ratio / 0.7f * 0.6f);
                    heatState.text = $"안전 {pct}";
                    break;
            }

            float ventRatio = 1f - heat.VentCooldownLeft / heat.CurrentVentCooldown;
            ventFill.anchorMax = new Vector2(ventRatio, 1f);
            if (heat.State == HeatState.Meltdown) ventLabel.text = "<color=#FF4050>방열 불가</color>";
            else if (heat.VentReady) ventLabel.text = "긴급 방열 준비 [Space]";
            else ventLabel.text = $"긴급 방열 {heat.VentCooldownLeft:0}s";
            ventFillImage.color = heat.VentReady ? new Color(0.3f, 0.9f, 1f, 0.6f) : new Color(0.3f, 0.6f, 0.8f, 0.35f);
        }

        public void SetSettingsPanel(SettingsPanel panel) => settings = panel;

        /// <summary>Tutorial line above the heat gauge.</summary>
        public void ShowHint(string text, float duration)
        {
            hint.text = text;
            hint.gameObject.SetActive(true);
            hintTimer = duration;
        }

        /// <summary>Equipped weapons as tag-coloured blocks, e.g. "무기 ■■■□□□".</summary>
        void RefreshWeaponStrip()
        {
            var sb = new System.Text.StringBuilder("무기 ");
            var weapons = gm.Loadout.Weapons;
            for (int i = 0; i < Player.MaxWeapons; i++)
            {
                if (i < weapons.Count)
                    sb.Append($"<color=#{ColorUtility.ToHtmlStringRGB(WeaponTags.ColorOf(weapons[i].Data.tag))}>■</color>");
                else sb.Append("<color=#3A4050>□</color>");
            }
            weaponStrip.text = sb.ToString();
        }

        /// <summary>Small notification under the wave label (achievements).</summary>
        public void ShowToast(string text)
        {
            toast.text = text;
            toastTimer = 3f;
        }

        public void ShowBanner(string text, Color color, float duration = 1.6f)
        {
            banner.text = text;
            banner.color = color;
            bannerTimer = duration;
        }

        public void ShowGameOver(bool victory, int wave, int kills, int totalScrap)
        {
            bool demoEnd = victory && BuildFlavor.IsDemo;
            gameOverTitle.text = demoEnd ? "데모 클리어!" : victory ? "탈출 성공!" : "코어 파괴";
            gameOverTitle.color = victory ? Palette.Scrap : Palette.Danger;
            gameOverStats.text = $"도달 웨이브: {wave}\n총 처치: {kills}\n보유 스크랩: ◆ {totalScrap}" +
                (demoEnd ? "\n<size=24><color=#FF9A3C>웨이브 11~20과 최종 보스는 정식판에서!\nSteam 위시리스트에 추가해 주세요.</color></size>" : "");
            Show(gameOverPanel, restartButton);
        }

        public void SetPaused(bool paused)
        {
            if (paused) Show(pausePanel, resumeButton);
            else pausePanel.SetActive(false);
        }

        public void HidePanels()
        {
            gameOverPanel.SetActive(false);
            pausePanel.SetActive(false);
        }

        static void Show(GameObject panel, Button focus)
        {
            panel.SetActive(true);
            panel.transform.SetAsLastSibling();
            // Select the default button so Enter / gamepad A works immediately.
            if (EventSystem.current) EventSystem.current.SetSelectedGameObject(focus.gameObject);
        }
    }
}
