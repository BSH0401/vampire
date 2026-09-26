using System;
using System.Collections.Generic;
using UnityEngine;

namespace CoreOverclock
{
    [CreateAssetMenu(menuName = "Core Overclock/Enemy Data", fileName = "E_NewEnemy")]
    public class EnemyData : ScriptableObject
    {
        public string id;
        public string displayName;
        public EnemyShape shape;
        public EnemyBehaviour behaviour;
        public Color color = Color.white;
        public float scale = 0.7f;

        [Header("Stats")]
        public float maxHP = 10f;
        public float moveSpeed = 2.5f;
        public float contactDamage = 3f;
        [Range(0f, 1f)] public float knockbackResistance;
        [Min(0)] public int scrapDrop = 1;
        [Tooltip("Shows a boss HP bar and is placed by the wave's boss list")] public bool isBoss;

        [Header("Charger (차지 러너)")]
        public float attackRange = 5.5f;
        public float windupTime = 1f;
        public float chargeSpeed = 11f;
        public float chargeDuration = 0.55f;
        public float recoverTime = 0.8f;

        [Header("Ranged (터렛 드론 / 보스)")]
        public float preferredDistance = 6.5f;
        public float attackInterval = 3f;
        public float projectileSpeed = 6f;
        public float projectileDamage = 4f;

        [Header("Bomber (마그마 버스트)")]
        public float fuseTime = 1.2f;
        public float triggerRange = 1.8f;
        public float explosionRadius = 2.2f;
        public float explosionDamage = 8f;
        [Tooltip("Damage dealt to other enemies caught in the blast (chain reactions)")] public float explosionEnemyDamage = 40f;

        [Header("Golem (코어 골렘)")]
        public float shieldRadius = 4f;

        [Header("Boss")]
        [Tooltip("Minion spawned by summon patterns")] public EnemyData summon;
    }
}
