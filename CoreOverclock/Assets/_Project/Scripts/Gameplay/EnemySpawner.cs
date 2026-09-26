using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CoreOverclock
{
    /// <summary>Spawns enemies for the current wave. Each spawn is telegraphed by a blinking marker.</summary>
    public class EnemySpawner : MonoBehaviour
    {
        const float TelegraphTime = 0.8f;
        const float MinPlayerDistance = 4f;
        const int MaxAlive = 400;

        Pool<Enemy> enemyPool;
        Pool<SpriteRenderer> markerPool;
        readonly List<SpriteRenderer> activeMarkers = new();
        WaveDefinition wave;
        float spawnTimer;
        bool running;
        int pending;

        public static EnemySpawner Create(Transform parent)
        {
            var go = new GameObject("EnemySpawner");
            go.transform.SetParent(parent, false);
            var s = go.AddComponent<EnemySpawner>();
            s.enemyPool = new Pool<Enemy>(() => Enemy.CreateInstance(go.transform), 64);
            s.markerPool = new Pool<SpriteRenderer>(
                () => Visuals.Sprite("SpawnMarker", go.transform, ShapeSprites.Cross, Palette.Danger, 1, 0.6f), 16);
            return s;
        }

        public void Begin(WaveDefinition definition)
        {
            wave = definition;
            spawnTimer = 0.4f;
            running = true;
        }

        public void StopAndDissolveAll()
        {
            running = false;
            StopAllCoroutines();
            pending = 0;
            foreach (var m in activeMarkers) markerPool.Release(m);
            activeMarkers.Clear();

            var snapshot = Enemy.Active.ToArray();
            foreach (var e in snapshot) e.FreezeAndDissolve(Random.Range(0.1f, 0.5f));
        }

        public void StopAll()
        {
            running = false;
            StopAllCoroutines();
            pending = 0;
            foreach (var m in activeMarkers) markerPool.Release(m);
            activeMarkers.Clear();
        }

        void Update()
        {
            if (!running) return;
            spawnTimer -= Time.deltaTime;
            if (spawnTimer > 0f) return;
            spawnTimer += wave.spawnInterval;

            for (int i = 0; i < wave.spawnPerTick; i++)
            {
                if (Enemy.Active.Count + pending >= MaxAlive) break;
                var data = wave.PickEnemy();
                if (data) StartCoroutine(SpawnRoutine(data, PickPosition()));
            }
        }

        Vector2 PickPosition()
        {
            Vector2 player = GameManager.Instance.Player.Position;
            Vector2 p = Vector2.zero;
            for (int i = 0; i < 12; i++)
            {
                p = Arena.RandomPoint(1.2f);
                if ((p - player).sqrMagnitude >= MinPlayerDistance * MinPlayerDistance) break;
            }
            return p;
        }

        IEnumerator SpawnRoutine(EnemyData data, Vector2 position)
        {
            pending++;
            var marker = markerPool.Get();
            marker.transform.position = position;
            activeMarkers.Add(marker);
            for (float t = 0f; t < TelegraphTime; t += Time.deltaTime)
            {
                var c = Palette.Danger;
                c.a = Mathf.Repeat(t * 6f, 1f) > 0.5f ? 1f : 0.35f;
                marker.color = c;
                yield return null;
            }
            activeMarkers.Remove(marker);
            markerPool.Release(marker);
            pending--;

            var enemy = enemyPool.Get();
            enemy.Spawn(data, position, wave.hpMultiplier, wave.speedMultiplier, enemyPool.Release);
        }
    }
}
