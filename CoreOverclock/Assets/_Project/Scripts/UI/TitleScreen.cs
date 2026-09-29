using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CoreOverclock
{
    /// <summary>Main menu shown over the idle arena: start, 연구소, settings, quit.</summary>
    public class TitleScreen : MonoBehaviour
    {
        GameObject root;
        Button startButton;
        Text footerRight;

        Text labLabel;

        public static TitleScreen Create(Canvas canvas, GameManager gm, SettingsPanel settings, LabScreen lab)
        {
            var ts = canvas.gameObject.AddComponent<TitleScreen>();
            ts.Build(canvas.transform, gm, settings, lab);
            return ts;
        }

        void Build(Transform canvas, GameManager gm, SettingsPanel settings, LabScreen lab)
        {
            var dim = UIFactory.Stretch("TitleScreen", canvas);
            UIFactory.Image(dim, new Color(0.01f, 0.01f, 0.03f, 0.86f)).raycastTarget = true;
            root = dim.gameObject;
            Vector2 c = new(0.5f, 0.5f), bl = new(0f, 0f), br = new(1f, 0f), bottom = new(0.5f, 0f);

            var title = UIFactory.Label(UIFactory.Rect("Title", dim, c, c, new Vector2(0f, 260f), new Vector2(1600f, 140f)),
                "CORE OVERCLOCK", 124, Palette.Player);
            title.GetComponent<Outline>().effectColor = new Color(0f, 0.6f, 0.7f, 0.6f);
            title.GetComponent<Outline>().effectDistance = new Vector2(4f, -4f);
            UIFactory.Label(UIFactory.Rect("Subtitle", dim, c, c, new Vector2(0f, 160f), new Vector2(1200f, 60f)),
                "코어 오버클럭", 44, Palette.Overclock);
            UIFactory.Label(UIFactory.Rect("Tagline", dim, c, c, new Vector2(0f, 95f), new Vector2(1600f, 40f)),
                "몰려오는 고철 군단을 부수고, 코어를 오버클럭하여 궁극의 살인 병기를 조립하라.", 26, new Color(0.65f, 0.72f, 0.85f));

            startButton = UIFactory.Button(dim, "게임 시작", new Vector2(0f, -30f), new Vector2(460f, 84f), gm.StartGame);
            var labButton = UIFactory.Button(dim, "연구소", new Vector2(0f, -125f), new Vector2(460f, 72f), () => { Hide(); lab.Open(Show); });
            labLabel = labButton.GetComponentInChildren<Text>();
            UIFactory.Button(dim, "설정", new Vector2(0f, -210f), new Vector2(460f, 72f), () => settings.Open(Hide, Show));
            UIFactory.Button(dim, "종료", new Vector2(0f, -295f), new Vector2(460f, 72f), Quit);

            UIFactory.Label(UIFactory.Rect("Controls", dim, bottom, bottom, new Vector2(0f, 60f), new Vector2(1600f, 34f)),
                "이동  WASD · 방향키 · 좌스틱     긴급 방열  Space · 우클릭 · A/RB     일시정지  ESC · Start", 22, new Color(0.55f, 0.6f, 0.75f));
            string flavor = BuildFlavor.IsDemo ? $"DEMO (웨이브 1~{BuildFlavor.DemoLastWave})" : "";
            UIFactory.Label(UIFactory.Rect("Version", dim, bl, bl, new Vector2(24f, 18f), new Vector2(600f, 30f)),
                $"v{BuildFlavor.Version}  {flavor}", 20, new Color(0.45f, 0.5f, 0.6f), TextAnchor.LowerLeft)
                .rectTransform.pivot = new Vector2(0f, 0f);
            footerRight = UIFactory.Label(UIFactory.Rect("Achievements", dim, br, br, new Vector2(-24f, 18f), new Vector2(600f, 30f)),
                "", 20, new Color(0.45f, 0.5f, 0.6f), TextAnchor.LowerRight);
            footerRight.rectTransform.pivot = new Vector2(1f, 0f);
            root.SetActive(false);
        }

        public void Show()
        {
            labLabel.text = $"연구소  <color=#{ColorUtility.ToHtmlStringRGB(Palette.Fragment)}>{MetaProgress.Currency} {MetaProgress.Fragments}</color>" +
                            (MetaProgress.HasUnseen ? "  <color=#FF6B6B><size=22>NEW</size></color>" : "");
            footerRight.text = $"퓨전 도감 {MetaProgress.DiscoveredCount} / {Fusions.All.Length}  ·  업적 {SteamService.UnlockedCount()} / {Achievements.All.Length}" +
                               (SteamService.Available ? "  ·  Steam 연결됨" : "");
            root.SetActive(true);
            root.transform.SetAsLastSibling();
            if (EventSystem.current) EventSystem.current.SetSelectedGameObject(startButton.gameObject);
        }

        public void Hide() => root.SetActive(false);

        static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }

    /// <summary>Options: volumes, screen shake, fullscreen. Opened from the title or pause menu.</summary>
    public class SettingsPanel : MonoBehaviour
    {
        GameObject root;
        Text master, musicVol, sfxVol, shake, fullscreen, resetLabel;
        bool resetArmed;
        Button closeButton;
        System.Action onClose;

        public static SettingsPanel Create(Canvas canvas)
        {
            var sp = canvas.gameObject.AddComponent<SettingsPanel>();
            sp.Build(canvas.transform);
            return sp;
        }

        void Build(Transform canvas)
        {
            var dim = UIFactory.Stretch("SettingsPanel", canvas);
            UIFactory.Image(dim, new Color(0f, 0f, 0.02f, 0.8f)).raycastTarget = true;
            root = dim.gameObject;
            var box = UIFactory.Rect("Box", dim, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(860f, 800f));
            UIFactory.Image(box, new Color(0.05f, 0.07f, 0.12f, 1f));
            UIFactory.Image(UIFactory.Rect("Frame", box, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(860f, 6f)), Palette.Player);
            // Rows below are laid out for the original 700px box; shift them up to fit the reset row.
            var rows = UIFactory.Rect("Rows", box, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(860f, 700f));
            UIFactory.Label(UIFactory.Rect("Title", box, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(800f, 70f)),
                "설정", 56, Palette.Player);

            master = Row(rows, "전체 볼륨", 190f, () => GameSettings.MasterVolume -= 0.1f, () => GameSettings.MasterVolume += 0.1f);
            musicVol = Row(rows, "음악", 100f, () => GameSettings.MusicVolume -= 0.1f, () => GameSettings.MusicVolume += 0.1f);
            sfxVol = Row(rows, "효과음", 10f, () => GameSettings.SfxVolume -= 0.1f, () => GameSettings.SfxVolume += 0.1f);
            shake = Toggle(rows, "화면 흔들림", -80f, () => GameSettings.ShakeLevel++);
            fullscreen = Toggle(rows, "전체 화면", -170f, () => GameSettings.Fullscreen = !GameSettings.Fullscreen);
            resetLabel = Toggle(rows, "진행 초기화", -260f, ResetProgress);
            closeButton = UIFactory.Button(box, "닫기", new Vector2(0f, -330f), new Vector2(360f, 72f), Close);
            root.SetActive(false);
        }

        Text Row(Transform box, string label, float y, UnityEngine.Events.UnityAction minus, UnityEngine.Events.UnityAction plus)
        {
            UIFactory.Label(UIFactory.Rect(label, box, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-220f, y), new Vector2(300f, 50f)),
                label, 32, Palette.Text, TextAnchor.MiddleLeft);
            UIFactory.Button(box, "-", new Vector2(80f, y), new Vector2(70f, 60f), () => { minus(); Refresh(); });
            var value = UIFactory.Label(UIFactory.Rect(label + "Value", box, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(190f, y), new Vector2(140f, 50f)),
                "", 32, Palette.Scrap);
            UIFactory.Button(box, "+", new Vector2(300f, y), new Vector2(70f, 60f), () => { plus(); Refresh(); });
            return value;
        }

        Text Toggle(Transform box, string label, float y, UnityEngine.Events.UnityAction toggle)
        {
            UIFactory.Label(UIFactory.Rect(label, box, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-220f, y), new Vector2(300f, 50f)),
                label, 32, Palette.Text, TextAnchor.MiddleLeft);
            var btn = UIFactory.Button(box, "", new Vector2(190f, y), new Vector2(290f, 60f), () => { toggle(); Refresh(); });
            return btn.GetComponentInChildren<Text>();
        }

        /// <summary>Wipes 코어 파편, upgrades, unlocks and the codex; the first press only arms it.</summary>
        void ResetProgress()
        {
            if (!resetArmed)
            {
                resetArmed = true;
                return;
            }
            resetArmed = false;
            MetaProgress.ResetAll();
            AudioManager.Play(SfxId.Deny, 0.6f, 0f);
        }

        public void Open(System.Action onOpen, System.Action closed)
        {
            resetArmed = false;
            onOpen?.Invoke();
            onClose = closed;
            root.SetActive(true);
            root.transform.SetAsLastSibling();
            Refresh();
            if (EventSystem.current) EventSystem.current.SetSelectedGameObject(closeButton.gameObject);
        }

        public bool IsOpen => root.activeSelf;

        public void Close()
        {
            root.SetActive(false);
            onClose?.Invoke();
        }

        void Refresh()
        {
            static string Pct(float v) => $"{Mathf.RoundToInt(v * 100f)}%";
            master.text = Pct(GameSettings.MasterVolume);
            musicVol.text = Pct(GameSettings.MusicVolume);
            sfxVol.text = Pct(GameSettings.SfxVolume);
            shake.text = GameSettings.ShakeLevelNames[GameSettings.ShakeLevel];
            fullscreen.text = GameSettings.Fullscreen ? "켜짐" : "꺼짐";
            resetLabel.text = resetArmed ? "<color=#FF6B6B>정말 초기화?</color>" : "초기화";
        }
    }
}
