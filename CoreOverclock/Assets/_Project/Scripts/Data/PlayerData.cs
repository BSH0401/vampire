using System;
using System.Collections.Generic;
using UnityEngine;

namespace CoreOverclock
{
    [CreateAssetMenu(menuName = "Core Overclock/Player Data", fileName = "PlayerData")]
    public class PlayerData : ScriptableObject
    {
        public float maxHP = 30f;
        public float moveSpeed = 6.5f;
        public float invulnerableTime = 0.6f;
        public float pickupRange = 2.5f;

        [Header("Electric Wall")]
        public float wallDamage = 1f;
        public float wallKnockback = 10f;
    }
}
