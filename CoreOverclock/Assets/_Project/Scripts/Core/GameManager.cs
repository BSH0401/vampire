using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CoreOverclock
{
    public enum GameState { Combat, Resolving, Intermission, GameOver, Title }

    /// <summary>
    /// Entry point and wave flow: 아레나 전투 → 결산 → 코어 작업실 → 다음 웨이브 (기획서 3장).
    /// Everything else in the scene is constructed at runtime from the data assets below.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        const int TowerCount = 2;

        [SerializeField] Material spriteMaterial;
        [SerializeField] PlayerData playerData;
        [SerializeField] WeaponData startingWeapon;
        [SerializeField] WaveTable waveTable;
        [SerializeField] ShopDatabase shopDatabase;

        ScrapSystem scrap;
        HUD hud;
        ShopUI shopUI;
        TitleScreen title;
        Tutorial tutorial;
        SettingsPanel settings;
        static bool skipTitleOnce; // "다시 시작" reloads the scene straight into a run
        DataTower[] towers;
        WaveDefinition currentWave;
        int waveStartFrame;
        float waveStartRealtime;
        int waveKills, waveScrapStart;
        float scrapRemainder, waveStartTime;
        float lastOverclockBanner = -10f;

        public static GameManager Instance { get; private set; }
        public GameState State { get; private set; }
        public Player Player { get; private set; }
        public Loadout Loadout { get; private set; }
        public HeatSystem Heat { get; private set; }
        public Shop Shop { get; private set; }
        public EnemySpawner Spawner { get; private set; }
        public WaveDefinition CurrentWave => currentWave;
        public int Wave { get; private set; }
        public int TotalWaves => BuildFlavor.IsDemo ? Mathf.Min(BuildFlavor.DemoLastWave, waveTable.Count) : waveTable.Count;
        public float TimeLeft { get; private set; }
        public int Scrap { get; private set; }
        public int TotalKills { get; private set; }
        public bool PlayerCanMove => State == GameState.Combat || State == GameState.Resolving;

        void Awake()
        {
            Instance = this;
            Enemy.Active.Clear();
            DataTower.Active.Clear();

            GameInput.Init();
            GameLayers.Init();
            Visuals.SpriteMaterial = spriteMaterial;
            DevCommandLine.Parse();

            gameObject.AddComponent<TimeControl>();
            gameObject.AddComponent<SteamRunner>();
            AudioManager.Create(gameObject);
            Heat = gameObject.AddComponent<HeatSystem>();
            Heat.StateChanged += OnHeatStateChanged;
            Loadout = new Loadout();
            Loadout.AddWeapon(startingWeapon, startingWeapon.price);
            var cam = Camera.main;
            if (cam)
            {
                cam.backgroundColor = Palette.Background;
                if (!cam.GetComponent<CameraShake>()) cam.gameObject.AddComponent<CameraShake>();
                PostFx.Create(cam);
            }

            var world = new GameObject("World").transform;
            Arena.Create(world);
            Player = Player.Create(playerData, world);
            Player.SyncWeapons(Loadout);
            Player.Died += OnPlayerDied;
            Loadout.Changed += () =>
            {
                Player.SyncWeapons(Loadout);
                if (Loadout.WeaponSlotsFull) Unlock(Achievements.FullArsenal);
            };

            ProjectileSystem.Create(world);
            FxSystem.Create(world);
            scrap = ScrapSystem.Create(world);
            EnemyProjectileSystem.Create(world);
            Spawner = EnemySpawner.Create(world);
            towers = new DataTower[TowerCount];
            for (int i = 0; i < TowerCount; i++) towers[i] = DataTower.Create(world);

            hud = HUD.Create(this);
            DamagePopups.Create(hud.Canvas);
            Shop = new Shop(shopDatabase, Loadout, this);
            shopUI = ShopUI.Create(hud.Canvas, this, Shop, Loadout);
            settings = SettingsPanel.Create(hud.Canvas);
            title = TitleScreen.Create(hud.Canvas, this, settings);
            tutorial = new Tutorial(hud);
            hud.SetSettingsPanel(settings);
            Scrap += DevCommandLine.StartScrap;

            if (DevCommandLine.Enabled) gameObject.AddComponent<DevCommandLine>();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Alt-tab / focus loss pauses the fight (automated test runs keep going).</summary>
        void OnApplicationFocus(bool focused)
        {
            if (!focused && !DevCommandLine.Enabled && State == GameState.Combat && !TimeControl.Paused) SetPaused(true);
        }

        void Start()
        {
            if ((DevCommandLine.Enabled && !DevCommandLine.ShowTitle) || skipTitleOnce)
            {
                skipTitleOnce = false;
                StartGame(Mathf.Clamp(DevCommandLine.StartWave, 1, TotalWaves));
                return;
            }
            State = GameState.Title;
            AudioManager.Prewarm();
            AudioManager.PlayMusic(MusicId.Title);
            title.Show();
        }

        public void StartGame() => StartGame(1);

        void StartGame(int wave)
        {
            title.Hide();
            StartWave(wave);
        }

        void Update()
        {
            if (GameInput.PausePressed && !settings.IsOpen && (State == GameState.Combat || TimeControl.Paused))
                SetPaused(!TimeControl.Paused);
            AudioManager.Muffled = TimeControl.Paused || (State == GameState.Combat && Heat.State == HeatState.Meltdown);

            if (State != GameState.Combat || TimeControl.Paused) return;
            tutorial.Tick(this);
            TimeLeft -= Time.deltaTime;
            if (TimeLeft <= 0f) EndWave();
        }

        void StartWave(int wave)
        {
            Wave = wave;
            var def = currentWave = waveTable.Get(wave);
            waveStartTime = Time.time;
            waveStartFrame = Time.frameCount;
            waveStartRealtime = Time.realtimeSinceStartup;
            TimeLeft = DevCommandLine.WaveTimeOverride > 0f ? DevCommandLine.WaveTimeOverride : def.duration;
            waveKills = 0;
            waveScrapStart = Scrap;

            Player.ResetForWave();
            Heat.ResetForWave();
            scrap.ResetForWave();
            PlaceTowers();
            hud.HidePanels();
            shopUI.Hide();
            State = GameState.Combat;
            Spawner.Begin(def);
            tutorial.OnWaveStart(wave);
            AudioManager.PlayMusic(def.bosses.Count > 0 ? MusicId.Boss : MusicId.Combat);

            bool boss = def.bosses.Count > 0;
            hud.ShowBanner(boss ? $"WAVE {wave} · 보스 출현" : $"WAVE {wave}", boss ? Palette.Danger : Palette.Player);
        }

        void PlaceTowers()
        {
            for (int i = 0; i < towers.Length; i++)
            {
                // Left and right halves so the towers never overlap each other or spawn on the player.
                float side = i % 2 == 0 ? -1f : 1f;
                var pos = new Vector2(side * Random.Range(5f, Arena.HalfSize.x - 2.5f), Random.Range(-Arena.HalfSize.y + 2.5f, Arena.HalfSize.y - 2.5f));
                towers[i].Activate(pos, Wave);
            }
        }

        void EndWave()
        {
            if (DevCommandLine.Enabled)
                Debug.Log($"[Dev] WaveEnd wave={Wave} time={Time.time - waveStartTime:F1}s timeLeft={TimeLeft:F1} kills={waveKills} " +
                          $"scrap={Scrap} hpLeft={Player.HP:F0}/{Player.MaxHP:F0} dmgTaken={Player.DamageTakenThisWave:F0} weapons={Loadout.Weapons.Count} chips={Loadout.Chips.Count} bossAlive={(Boss != null ? $"{Boss.HP / Boss.MaxHP:P0}" : "no")} " +
                          $"fps={(Time.frameCount - waveStartFrame) / Mathf.Max(0.01f, Time.realtimeSinceStartup - waveStartRealtime):F0}");
            TimeLeft = 0f;
            State = GameState.Resolving;
            Heat.ResetForWave(); // don't carry the last reading into the shop screen
            Spawner.StopAndDissolveAll();
            ProjectileSystem.ClearAll();
            EnemyProjectileSystem.ClearAll();
            scrap.CollectAll();
            CameraShake.Add(0.3f);
            hud.ShowBanner("WAVE CLEAR", Palette.Scrap);
            AudioManager.Play(SfxId.WaveClear, 0.6f, 0f);
            if (Wave == 1) Unlock(Achievements.FirstWave);
            StartCoroutine(ResolveRoutine());
        }

        IEnumerator ResolveRoutine()
        {
            // 결산 페이즈: field scrap flies to the player before the summary appears.
            float t = 0f;
            while (t < 1.2f || (!scrap.IsEmpty && t < 4f))
            {
                t += Time.deltaTime;
                yield return null;
            }

            if (Wave >= TotalWaves)
            {
                State = GameState.GameOver;
                Unlock(BuildFlavor.IsDemo ? Achievements.DemoClear : Achievements.Escape);
                AudioManager.PlayMusic(MusicId.None);
                AudioManager.Play(SfxId.Victory, 0.8f, 0f);
                hud.ShowGameOver(true, Wave, TotalKills, Scrap);
                yield break;
            }

            // 결산: wave clear bonus on top of collected scrap.
            int bonus = WaveClearBonus(Wave);
            Scrap += bonus;

            // 코어 작업실
            State = GameState.Intermission;
            AudioManager.PlayMusic(MusicId.Shop);
            Shop.Open(Wave + 1);
            shopUI.Show(Wave, waveKills, Scrap - waveScrapStart, bonus);
        }

        public void NextWave()
        {
            if (State != GameState.Intermission) return;
            StartWave(Wave + 1);
        }

        void OnPlayerDied()
        {
            if (State == GameState.GameOver) return;
            State = GameState.GameOver;
            if (DevCommandLine.Enabled) Debug.Log($"[Dev] PlayerDied wave={Wave} after {Time.time - waveStartTime:F1}s");
            Spawner.StopAll();
            EnemyProjectileSystem.ClearAll();
            foreach (var e in Enemy.Active.ToArray()) e.FreezeAndDissolve(Random.Range(0.3f, 1f));
            hud.ShowBanner("CORE DESTROYED", Palette.Danger, 2f);
            AudioManager.PlayMusic(MusicId.None);
            AudioManager.Play(SfxId.GameOver, 0.8f, 0f);
            StartCoroutine(GameOverRoutine());
        }

        IEnumerator GameOverRoutine()
        {
            yield return new WaitForSeconds(1.5f);
            hud.ShowGameOver(false, Wave, TotalKills, Scrap);
        }

        public void SetPaused(bool paused)
        {
            if (paused && State != GameState.Combat) return;
            TimeControl.SetPaused(paused);
            hud.SetPaused(paused);
        }

        public void Restart()
        {
            skipTitleOnce = true;
            ReloadScene();
        }

        public void ReturnToTitle()
        {
            skipTitleOnce = false;
            ReloadScene();
        }

        static void ReloadScene()
        {
            TimeControl.SetPaused(false);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        void Unlock(string achievement)
        {
            if (SteamService.Unlock(achievement)) hud.ShowToast($"업적 달성 · {SteamService.DisplayName(achievement)}");
        }

        public void RegisterKill(Enemy enemy)
        {
            waveKills++;
            TotalKills++;
            if (!enemy.IsBoss || State != GameState.Combat) return;

            CameraShake.Add(0.8f);
            FxSystem.Pulse(enemy.Position, enemy.Data.color, 0.5f, 14f, 0.8f);
            AudioManager.Play(SfxId.Explosion, 1f, 0f);
            if (enemy.Data.id == "juggernaut") Unlock(Achievements.Juggernaut);
            else if (enemy.Data.id == "overseer") Unlock(Achievements.Overseer);
            hud.ShowBanner($"{enemy.Data.displayName} 격파!", Palette.Scrap, 2f);
            // Wave 20: destroying the boss core ends the run early (기획서 4.3).
            if (currentWave.endOnBossKill && Boss == null) EndWave();
        }

        /// <summary>First living boss on the field (for the HUD bar).</summary>
        public Enemy Boss
        {
            get
            {
                foreach (var e in Enemy.Active) if (e.IsBoss && e.IsAlive) return e;
                return null;
            }
        }

        public void AddScrap(int amount) => Scrap += amount;

        static int WaveClearBonus(int wave) => 5 + wave * 2;

        /// <summary>Scrap picked up from the field; applies the scrap-gain chip bonus.</summary>
        public void CollectScrap(int amount)
        {
            // Capped at +100% so stacking scrap chips can't break the economy.
            scrapRemainder += amount * (1f + Mathf.Min(Loadout.Stats.ScrapGainPct, 1f));
            int whole = Mathf.FloorToInt(scrapRemainder);
            scrapRemainder -= whole;
            Scrap += whole;
        }

        public bool TrySpendScrap(int amount)
        {
            if (amount > Scrap) return false;
            Scrap -= amount;
            return true;
        }

        void OnHeatStateChanged(HeatState s)
        {
            if (DevCommandLine.Enabled) Debug.Log($"[Dev] Heat -> {s} at t={Time.time:F1} hp={Player.HP} weapons={Loadout.Weapons.Count}");
            if (State != GameState.Combat) return;
            if (s == HeatState.Overclock && Time.time - lastOverclockBanner > 4f)
            {
                lastOverclockBanner = Time.time;
                hud.ShowBanner("OVERCLOCK", Palette.Overclock, 1f);
                AudioManager.Play(SfxId.Overclock, 0.5f, 0f);
            }
            else if (s == HeatState.Meltdown)
            {
                hud.ShowBanner("MELTDOWN", Palette.Danger, 1.5f);
                AudioManager.Play(SfxId.Meltdown, 0.7f, 0f);
                Unlock(Achievements.FirstMeltdown);
            }
        }
    }
}
