using System;
using System.Collections.Generic;
using UnityEngine;

namespace CoreOverclock
{
    [Serializable]
    public class WaveSpawnEntry
    {
        public EnemyData enemy;
        [Min(0f)] public float weight = 1f;
    }

    [Serializable]
    public class WaveDefinition
    {
        public float duration = 30f;
        public float spawnInterval = 1.2f;
        [Min(1)] public int spawnPerTick = 1;
        public float hpMultiplier = 1f;
        public float speedMultiplier = 1f;
        [Range(0f, 1f), Tooltip("Chance for each point of an enemy's scrap to drop (bosses always drop in full)")]
        public float scrapDropChance = 1f;
        public List<WaveSpawnEntry> enemies = new();
        [Tooltip("Spawned once at wave start")] public List<EnemyData> bosses = new();
        [Tooltip("Wave also ends (cleared) when every boss is destroyed")] public bool endOnBossKill;

        public EnemyData PickEnemy()
        {
            float total = 0f;
            foreach (var e in enemies) if (e.enemy) total += e.weight;
            float r = UnityEngine.Random.value * total;
            foreach (var e in enemies)
            {
                if (!e.enemy) continue;
                r -= e.weight;
                if (r <= 0f) return e.enemy;
            }
            return enemies.Count > 0 ? enemies[0].enemy : null;
        }
    }

    [CreateAssetMenu(menuName = "Core Overclock/Wave Table", fileName = "WaveTable")]

    public class WaveTable : ScriptableObject
    {
        public List<WaveDefinition> waves = new();

        public int Count => waves.Count;

        /// <param name="wave">1-based wave number</param>
        public WaveDefinition Get(int wave) => waves[Mathf.Clamp(wave - 1, 0, waves.Count - 1)];
    }
}
