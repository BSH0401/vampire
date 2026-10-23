using UnityEngine;

namespace CoreOverclock
{
    /// <summary>
    /// 무기 등급 I~IV: two copies of the same weapon at the same grade merge into the next grade,
    /// or a weapon can be upgraded for scrap in the 코어 작업실. Later waves also sell higher grades.
    /// </summary>
    public static class WeaponGrades
    {
        public const int Max = 4;

        static readonly float[] Damage = { 1f, 1.4f, 1.9f, 2.6f };
        static readonly float[] FireRate = { 0f, 0.08f, 0.16f, 0.25f };
        static readonly float[] Heat = { 1f, 0.95f, 0.9f, 0.85f };
        /// <summary>Shop price of an offer at this grade, relative to grade I (a little under 2^(g-1) copies).</summary>
        static readonly float[] OfferPrice = { 1f, 1.9f, 3.6f, 6.8f };
        /// <summary>Scrap cost to raise a weapon from grade g to g+1, relative to its grade I price.</summary>
        static readonly float[] UpgradePrice = { 1.3f, 2.4f, 4.5f };

        static int Index(int grade) => Mathf.Clamp(grade, 1, Max) - 1;

        public static float DamageMultiplier(int grade) => Damage[Index(grade)];
        public static float FireRateBonus(int grade) => FireRate[Index(grade)];
        public static float HeatMultiplier(int grade) => Heat[Index(grade)];
        public static float OfferPriceMultiplier(int grade) => OfferPrice[Index(grade)];
        public static float UpgradePriceMultiplier(int grade) => UpgradePrice[Mathf.Clamp(grade, 1, Max - 1) - 1];

        public static string Roman(int grade) => grade switch { 1 => "I", 2 => "II", 3 => "III", _ => "IV" };

        public static Color ColorOf(int grade) => grade switch
        {
            1 => new Color(0.72f, 0.77f, 0.88f),
            2 => new Color(0.4f, 1f, 0.55f),
            3 => new Color(0.4f, 0.7f, 1f),
            _ => new Color(1f, 0.72f, 0.2f),
        };

        public static string Hex(int grade) => ColorUtility.ToHtmlStringRGB(ColorOf(grade));

        /// <summary>"블래스터 <color>II</color>"; grade I shows the plain name.</summary>
        public static string Name(WeaponData w, int grade) => grade <= 1 ? w.displayName : $"{w.displayName} <color=#{Hex(grade)}>{Roman(grade)}</color>";

        /// <summary>Short effect summary, e.g. "피해 x1.4 · 연사 +8%".</summary>
        public static string Describe(int grade) =>
            grade <= 1 ? "기본 성능" : $"피해 x{DamageMultiplier(grade):0.0#} · 연사 +{FireRateBonus(grade) * 100f:0}% · 발열 -{(1f - HeatMultiplier(grade)) * 100f:0}%";
    }
}
