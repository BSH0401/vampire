using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CoreOverclock
{
    public class HUD : MonoBehaviour
    {
        GameManager gm;
        Text waveLabel, timerLabel, hpLabel, scrapLabel, killLabel, banner, hint;
        RectTransform hpFill;
        GameObject waveClearPanel, gameOverPanel, pausePanel;
        Text waveClearTitle, waveClearStats, gameOverTitle, gameOverStats;
        Button nextButton, restartButton, resumeButton;
        float bannerTimer;

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
            var root = Canvas.transform;
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
            hint = UIFactory.Label(UIFactory.Rect("Hint", root, bottom, bottom, new Vector2(0f, 18f), new Vector2(1400f, 36f)),
                "이동: WASD / 방향키 / 좌스틱   ·   공격: 자동   ·   일시정지: ESC / Start", 22, new Color(0.6f, 0.65f, 0.8f, 0.8f));

            waveClearPanel = BuildPanel("WaveClearPanel", out waveClearTitle, out waveClearStats);
            nextButton = UIFactory.Button(waveClearPanel.transform, "다음 웨이브 ▶", new Vector2(0f, -170f), new Vector2(420f, 80f), gm.NextWave);

            gameOverPanel = BuildPanel("GameOverPanel", out gameOverTitle, out gameOverStats);
            restartButton = UIFactory.Button(gameOverPanel.transform, "다시 시작", new Vector2(0f, -170f), new Vector2(420f, 80f), gm.Restart);

            pausePanel = BuildPanel("PausePanel", out var pauseTitle, out var pauseStats);
            pauseTitle.text = "일시정지";
            pauseStats.text = "";
            resumeButton = UIFactory.Button(pausePanel.transform, "계속하기", new Vector2(0f, -40f), new Vector2(420f, 80f), () => gm.SetPaused(false));
            UIFactory.Button(pausePanel.transform, "처음부터", new Vector2(0f, -140f), new Vector2(420f, 80f), gm.Restart);
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

            if (bannerTimer > 0f)
            {
                bannerTimer -= Time.unscaledDeltaTime;
                var c = banner.color;
                c.a = Mathf.Clamp01(bannerTimer / 0.4f);
                banner.color = c;
            }
        }

        public void ShowBanner(string text, Color color, float duration = 1.6f)
        {
            banner.text = text;
            banner.color = color;
            bannerTimer = duration;
        }

        public void ShowWaveClear(int wave, int kills, int scrapGained, int totalScrap)
        {
            waveClearTitle.text = $"WAVE {wave} CLEAR";
            waveClearStats.text =
                $"처치: {kills}\n획득 스크랩: +{scrapGained}   (보유 ◆ {totalScrap})\n\n<color=#7f8aa8><size=24>코어 작업실(상점)은 Phase 2에서 연결됩니다</size></color>";
            Show(waveClearPanel, nextButton);
        }

        public void ShowGameOver(bool victory, int wave, int kills, int totalScrap)
        {
            gameOverTitle.text = victory ? "탈출 성공!" : "코어 파괴";
            gameOverTitle.color = victory ? Palette.Scrap : Palette.Danger;
            gameOverStats.text = $"도달 웨이브: {wave}\n총 처치: {kills}\n보유 스크랩: ◆ {totalScrap}";
            Show(gameOverPanel, restartButton);
        }

        public void SetPaused(bool paused)
        {
            if (paused) Show(pausePanel, resumeButton);
            else pausePanel.SetActive(false);
        }

        public void HidePanels()
        {
            waveClearPanel.SetActive(false);
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
