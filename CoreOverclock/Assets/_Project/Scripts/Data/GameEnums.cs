using System;
using System.Collections.Generic;
using UnityEngine;

namespace CoreOverclock
{
    public enum WeaponTag { Ballistic, Energy, Cryo, Explosive }

    public enum EnemyShape { Triangle, Arrow, Octagon, Circle, Square }

    /// <summary>AI archetype (기획서 5장). Values are serialized as ints: append only.</summary>
    public enum EnemyBehaviour { Chaser, Charger, Turret, Bomber, Golem, BossOverseer, BossLegion }

    public static class WeaponTags
    {
        public static Color ColorOf(WeaponTag tag) => tag switch
        {
            WeaponTag.Ballistic => new Color(1f, 0.85f, 0.35f),
            WeaponTag.Energy => new Color(1f, 0.3f, 0.85f),
            WeaponTag.Cryo => new Color(0.4f, 0.85f, 1f),
            WeaponTag.Explosive => new Color(1f, 0.45f, 0.2f),
            _ => Color.white,
        };

        public static string KoreanName(WeaponTag tag) => tag switch
        {
            WeaponTag.Ballistic => "탄도",
            WeaponTag.Energy => "레이저",
            WeaponTag.Cryo => "냉각",
            WeaponTag.Explosive => "폭발",
            _ => tag.ToString(),
        };
    }
}
