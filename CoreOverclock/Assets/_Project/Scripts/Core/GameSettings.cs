using UnityEngine;

namespace CoreOverclock
{
    /// <summary>Player options persisted in PlayerPrefs.</summary>
    public static class GameSettings
    {
        const string KeyMaster = "opt_master", KeyMusic = "opt_music", KeySfx = "opt_sfx", KeyShake = "opt_shake", KeyShakeLevel = "opt_shake_level", KeyFullscreen = "opt_fullscreen";
        public static readonly string[] ShakeLevelNames = { "끔", "약하게", "보통" };
        static readonly float[] ShakeLevelStrength = { 0f, 0.5f, 1f };

        static bool loaded;
        static float master = 0.8f, musicVol = 0.7f, sfxVol = 0.8f;
        static int shakeLevel = 1; // default 약하게

        public static float MasterVolume { get { Load(); return master; } set { master = Mathf.Clamp01(value); Save(); } }
        public static float MusicVolume { get { Load(); return musicVol; } set { musicVol = Mathf.Clamp01(value); Save(); } }
        public static float SfxVolume { get { Load(); return sfxVol; } set { sfxVol = Mathf.Clamp01(value); Save(); } }
        public static int ShakeLevel { get { Load(); return shakeLevel; } set { shakeLevel = (value % 3 + 3) % 3; Save(); } }
        public static float ShakeStrength => ShakeLevelStrength[ShakeLevel];

        static bool? fullscreenRequested; // screen mode changes apply a frame late, so remember what was asked for

        public static bool Fullscreen
        {
            get => fullscreenRequested ?? Screen.fullScreenMode != FullScreenMode.Windowed;
            set
            {
                fullscreenRequested = value;
                Screen.fullScreenMode = value ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
                PlayerPrefs.SetInt(KeyFullscreen, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        static void Load()
        {
            if (loaded) return;
            loaded = true;
            master = PlayerPrefs.GetFloat(KeyMaster, master);
            musicVol = PlayerPrefs.GetFloat(KeyMusic, musicVol);
            sfxVol = PlayerPrefs.GetFloat(KeySfx, sfxVol);
            // Older builds stored an on/off flag; "off" carries over, everything else starts at 약하게.
            shakeLevel = PlayerPrefs.HasKey(KeyShakeLevel) ? Mathf.Clamp(PlayerPrefs.GetInt(KeyShakeLevel), 0, 2)
                : PlayerPrefs.GetInt(KeyShake, 1) == 0 ? 0 : 1;
        }

        static void Save()
        {
            PlayerPrefs.SetFloat(KeyMaster, master);
            PlayerPrefs.SetFloat(KeyMusic, musicVol);
            PlayerPrefs.SetFloat(KeySfx, sfxVol);
            PlayerPrefs.SetInt(KeyShakeLevel, shakeLevel);
            PlayerPrefs.Save();
        }
    }
}
