using System;
using System.Collections.Generic;
using UnityEngine;

namespace CoreOverclock
{
    [CreateAssetMenu(menuName = "Core Overclock/Weapon Data", fileName = "W_NewWeapon")]
    public class WeaponData : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        public WeaponTag tag;
        [Min(1)] public int tier = 1;
        [Min(0)] public int price = 20;

        [Header("Combat")]
        public float damage = 8f;
        [Tooltip("Seconds between shots")] public float fireInterval = 0.6f;
        public float range = 7f;
        [Min(1)] public int projectileCount = 1;
        public float spreadAngle = 8f;
        [Min(0)] public int pierce;
        public float knockback = 2f;
        [Range(0f, 1f)] public float critChance = 0.05f;
        public float critMultiplier = 2f;

        [Header("Projectile")]
        public float projectileSpeed = 16f;
        public float projectileRadius = 0.12f;
        public float projectileLifetime = 1.2f;
        public Color projectileColor = new Color(1f, 0.9f, 0.4f);

        [Header("Heat (Phase 2)")]
        public float heatPerShot = 1f;
    }
}
