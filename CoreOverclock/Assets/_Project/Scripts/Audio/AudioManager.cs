using System.Collections.Generic;
using UnityEngine;

namespace CoreOverclock
{
    public enum SfxId
    {
        ShootBallistic, ShootEnergy, ShootCryo, ShootExplosive, Hit, EnemyDie, Explosion, PlayerHurt, Zap,
        Pickup, Vent, Overclock, Meltdown, BossSpawn, EnemyShoot, ChargeWindup, WaveClear, Victory, GameOver,
        UIClick, Buy, Deny,
    }

    public enum MusicId { None, Title, Combat, Boss, Shop }

    /// <summary>
    /// SFX voice pool with per-sound rate limiting (hundreds of shots per second would otherwise
    /// turn into noise) plus a crossfading music player. Volumes come from <see cref="GameSettings"/>.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        const int Voices = 24;
        const float CrossfadeTime = 1.2f;

        static AudioManager instance;
        static readonly Dictionary<SfxId, AudioClip> sfx = new();
        static readonly Dictionary<MusicId, AudioClip> music = new();
        static readonly Dictionary<MusicId, float[]> rendered = new(); // filled by the worker thread
        static bool renderStarted;

        AudioSource[] voices;
        readonly float[] lastPlayed = new float[System.Enum.GetValues(typeof(SfxId)).Length];
        int nextVoice;
        AudioSource musicA, musicB;
        AudioLowPassFilter musicFilter;
        MusicId currentMusic;
        float fade = 1f;
        float pickupCombo, lastPickup;

        /// <summary>Muffles music (pause menu, meltdown).</summary>
        public static bool Muffled { get; set; }

        public static void Create(GameObject host)
        {
            if (instance) return;
            instance = host.AddComponent<AudioManager>();
            instance.Build();
            Prewarm();
        }

        void Build()
        {
            if (!FindFirstObjectByType<AudioListener>())
            {
                var cam = Camera.main;
                (cam ? cam.gameObject : gameObject).AddComponent<AudioListener>();
            }

            // Clips are static so a scene reload (restart) doesn't re-synthesize them.
            if (sfx.Count == 0)
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                foreach (SfxId id in System.Enum.GetValues(typeof(SfxId))) sfx[id] = SoundSynth.Make(id);
                Debug.Log($"[Audio] {sfx.Count} SFX synthesized in {sw.ElapsedMilliseconds} ms");
            }

            voices = new AudioSource[Voices];
            for (int i = 0; i < Voices; i++)
            {
                voices[i] = gameObject.AddComponent<AudioSource>();
                voices[i].playOnAwake = false;
            }

            var musicHost = new GameObject("Music");
            musicHost.transform.SetParent(transform, false);
            musicA = musicHost.AddComponent<AudioSource>();
            musicB = musicHost.AddComponent<AudioSource>();
            foreach (var m in new[] { musicA, musicB })
            {
                m.loop = true;
                m.playOnAwake = false;
                m.volume = 0f;
                m.ignoreListenerPause = true;
            }
            musicFilter = musicHost.AddComponent<AudioLowPassFilter>();
            musicFilter.cutoffFrequency = 22000f;
        }

        void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        // ───────────────────────────── SFX ─────────────────────────────

        static float MinInterval(SfxId id) => id switch
        {
            SfxId.ShootBallistic or SfxId.ShootEnergy or SfxId.ShootCryo => 0.05f,
            SfxId.ShootExplosive => 0.08f,
            SfxId.Hit => 0.035f,
            SfxId.EnemyDie => 0.03f,
            SfxId.Explosion => 0.07f,
            SfxId.EnemyShoot => 0.06f,
            SfxId.Pickup => 0.03f,
            SfxId.ChargeWindup => 0.2f,
            _ => 0f,
        };

        public static void Play(SfxId id, float volume = 1f, float pitchJitter = 0.05f)
        {
            if (!instance) return;
            instance.PlayInternal(id, volume, 1f + Random.Range(-pitchJitter, pitchJitter));
        }

        /// <summary>Scrap pickups rise in pitch while collected in quick succession.</summary>
        public static void PlayPickup()
        {
            if (!instance) return;
            float now = Time.unscaledTime;
            instance.pickupCombo = now - instance.lastPickup < 0.3f ? Mathf.Min(instance.pickupCombo + 1f, 12f) : 0f;
            instance.lastPickup = now;
            instance.PlayInternal(SfxId.Pickup, 0.5f, 1f + instance.pickupCombo * 0.06f);
        }

        void PlayInternal(SfxId id, float volume, float pitch)
        {
            float now = Time.unscaledTime;
            int i = (int)id;
            if (now - lastPlayed[i] < MinInterval(id)) return;
            lastPlayed[i] = now;

            var v = voices[nextVoice];
            nextVoice = (nextVoice + 1) % voices.Length;
            v.clip = sfx[id];
            v.pitch = pitch;
            v.volume = volume * GameSettings.SfxVolume * GameSettings.MasterVolume;
            v.Play();
        }

        // ───────────────────────────── Music ─────────────────────────────

        MusicId pendingMusic = MusicId.None;

        public static void PlayMusic(MusicId id)
        {
            if (!instance || instance.currentMusic == id) return;
            instance.currentMusic = id;
            instance.pendingMusic = id;
            instance.TryStartPending();
        }

        /// <summary>
        /// Starts rendering every music track on a worker thread (~2 s of CPU) so the main thread never hitches.
        /// Tracks begin playing as soon as they are ready.
        /// </summary>
        public static void Prewarm()
        {
            if (renderStarted) return;
            renderStarted = true;
            System.Threading.Tasks.Task.Run(() =>
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                foreach (MusicId id in new[] { MusicId.Title, MusicId.Combat, MusicId.Shop, MusicId.Boss })
                {
                    var samples = SoundSynth.RenderMusic(id);
                    lock (rendered) rendered[id] = samples;
                }
                Debug.Log($"[Audio] music rendered in background in {sw.ElapsedMilliseconds} ms");
            });
        }

        void TryStartPending()
        {
            var id = pendingMusic;
            if (id != MusicId.None && !music.ContainsKey(id))
            {
                float[] samples;
                lock (rendered) rendered.TryGetValue(id, out samples);
                if (samples == null) return; // still rendering; Update retries
                music[id] = SoundSynth.ToClip($"music_{id}", samples);
            }
            pendingMusic = MusicId.None;

            // Swap sources: B becomes the outgoing track, A the incoming one.
            (musicA, musicB) = (musicB, musicA);
            musicA.clip = id == MusicId.None ? null : music[id];
            if (musicA.clip) musicA.Play();
            fade = 0f;
        }

        void Update()
        {
            if (pendingMusic != MusicId.None) TryStartPending();

            fade = Mathf.MoveTowards(fade, 1f, Time.unscaledDeltaTime / CrossfadeTime);
            float vol = GameSettings.MusicVolume * GameSettings.MasterVolume * 0.55f;
            musicA.volume = fade * vol;
            musicB.volume = (1f - fade) * vol;
            if (fade >= 1f && musicB.isPlaying) musicB.Stop();

            float targetCutoff = Muffled ? 900f : 22000f;
            musicFilter.cutoffFrequency = Mathf.Lerp(musicFilter.cutoffFrequency, targetCutoff, Time.unscaledDeltaTime * 6f);
        }
    }
}
