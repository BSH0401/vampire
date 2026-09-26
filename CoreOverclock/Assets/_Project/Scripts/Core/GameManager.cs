using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CoreOverclock
{
    public enum GameState { Combat, Resolving, Intermission, GameOver }

    /// <summary>
    /// Entry point and wave flow: 아레나 전투 → 결산 → (코어 작업실: Phase 2) → 다음 웨이브.
    /// Everything else in the scene is constructed at runtime from the data assets below.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        const int TowerCount = 2;

        [SerializeField] Material spriteMaterial;
        [SerializeField] PlayerData playerData;
        [SerializeField] WeaponData startingWeapon;
        [SerializeField] WaveTable waveTable;

        EnemySpawner spawner;
        ScrapSystem scrap;
        HUD hud;
        DataTower[] towers;
        int waveKills, waveScrapStart;

        public static GameManager Instance { get; private set; }
        public GameState State { get; private set; }
        public Player Player { get; private set; }
        public int Wave { get; private set; }
        public int TotalWaves => waveTable.Count;
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
            var cam = Camera.main;
            if (cam)
            {
                cam.backgroundColor = Palette.Background;
                if (!cam.GetComponent<CameraShake>()) cam.gameObject.AddComponent<CameraShake>();
            }

            var world = new GameObject("World").transform;
            Arena.Create(world);
            Player = Player.Create(playerData, world);
            Player.TryAddWeapon(startingWeapon);
            Player.Died += OnPlayerDied;

            ProjectileSystem.Create(world);
            FxSystem.Create(world);
            scrap = ScrapSystem.Create(world);
            spawner = EnemySpawner.Create(world);
            towers = new DataTower[TowerCount];
            for (int i = 0; i < TowerCount; i++) towers[i] = DataTower.Create(world);

            hud = HUD.Create(this);
            DamagePopups.Create(hud.Canvas);

            if (DevCommandLine.Enabled) gameObject.AddComponent<DevCommandLine>();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Start() => StartWave(Mathf.Clamp(DevCommandLine.StartWave, 1, TotalWaves));

        void Update()
        {
            if (GameInput.PausePressed && (State == GameState.Combat || TimeControl.Paused))
                SetPaused(!TimeControl.Paused);

            if (State != GameState.Combat || TimeControl.Paused) return;
            TimeLeft -= Time.deltaTime;
            if (TimeLeft <= 0f) EndWave();
        }

        void StartWave(int wave)
        {
            Wave = wave;
            var def = waveTable.Get(wave);
            TimeLeft = DevCommandLine.WaveTimeOverride > 0f ? DevCommandLine.WaveTimeOverride : def.duration;
            waveKills = 0;
            waveScrapStart = Scrap;

            Player.ResetForWave();
            scrap.ResetForWave();
            PlaceTowers();
            hud.HidePanels();
            State = GameState.Combat;
            spawner.Begin(def);

            bool boss = wave == 5 || wave == 15 || wave == 20;
            hud.ShowBanner(boss ? $"WAVE {wave} · 경고" : $"WAVE {wave}", boss ? Palette.Danger : Palette.Player);
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
            TimeLeft = 0f;
            State = GameState.Resolving;
            spawner.StopAndDissolveAll();
            ProjectileSystem.ClearAll();
            scrap.CollectAll();
            CameraShake.Add(0.3f);
            hud.ShowBanner("WAVE CLEAR", Palette.Scrap);
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
                hud.ShowGameOver(true, Wave, TotalKills, Scrap);
                yield break;
            }

            State = GameState.Intermission;
            hud.ShowWaveClear(Wave, waveKills, Scrap - waveScrapStart, Scrap);
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
            spawner.StopAll();
            foreach (var e in Enemy.Active.ToArray()) e.FreezeAndDissolve(Random.Range(0.3f, 1f));
            hud.ShowBanner("CORE DESTROYED", Palette.Danger, 2f);
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
            TimeControl.SetPaused(false);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void RegisterKill()
        {
            waveKills++;
            TotalKills++;
        }

        public void AddScrap(int amount) => Scrap += amount;
    }
}
