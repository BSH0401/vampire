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
    }
}
