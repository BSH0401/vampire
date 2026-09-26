using System.Collections.Generic;
using UnityEngine;

namespace CoreOverclock
{
    /// <summary>Everything the shop may offer.</summary>
    [CreateAssetMenu(menuName = "Core Overclock/Shop Database", fileName = "ShopDatabase")]
    public class ShopDatabase : ScriptableObject
    {
        public List<WeaponData> weapons = new();
        public List<ChipData> chips = new();
        [Range(0f, 1f)] public float weaponChance = 0.6f;
    }
}
