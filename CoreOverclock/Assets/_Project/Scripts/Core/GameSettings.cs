using UnityEngine;

namespace CoreOverclock
{
    /// <summary>Player options persisted in PlayerPrefs.</summary>
    public static class GameSettings
    {
        const string KeyMaster = "opt_master", KeyMusic = "opt_music", KeySfx = "opt_sfx", KeyShake = "opt_shake", KeyFullscreen = "opt_fullscreen";

        static bool loaded;
        static float master = 0.8f, musicVol = 0.7f, sfxVol = 0.8f;
        static bool shake = true;

        public static float MasterVolume { get { Load(); return master; } set { master = Mathf.Clamp01(value); Save(); } }
        public static float MusicVolume { get { Load(); return musicVol; } set { musicVol = Mathf.Clamp01(value); Save(); } }
        public static float SfxVolume { get { Load(); return sfxVol; } set { sfxVol = Mathf.Clamp01(value); Save(); } }
        public static bool ScreenShake { get { Load(); return shake; } set { shake = value; Save(); } }

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
            shake = PlayerPrefs.GetInt(KeyShake, 1) == 1;
        }

        static void Save()
        {
            PlayerPrefs.SetFloat(KeyMaster, master);
            PlayerPrefs.SetFloat(KeyMusic, musicVol);
            PlayerPrefs.SetFloat(KeySfx, sfxVol);
            PlayerPrefs.SetInt(KeyShake, shake ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
