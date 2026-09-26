using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CoreOverclock.EditorTools
{
    /// <summary>
    /// Phase 2 default content: 6 weapons covering all four tags and 8 passive chipsets.
    /// Setup re-applies these values every run (source of truth until Phase 3 balancing moves to the assets).
    /// Heat rule of thumb: ~3 heat/s per weapon, so 4-5 weapons reach overclock and more melt down without cooling.
    /// </summary>
    public static class DefaultContent
    {
        public static List<WeaponData> Weapons(string root) => new()
        {
            Weapon(root, "W_Blaster", w =>
            {
                w.id = "blaster"; w.displayName = "펄스 블래스터"; w.tag = WeaponTag.Ballistic; w.tier = 1; w.price = 15;
                w.description = "기본 단발 탄도 무기.";
                w.damage = 7f; w.fireInterval = 0.5f; w.range = 7.5f; w.knockback = 3f; w.critChance = 0.1f;
                w.projectileSpeed = 17f; w.projectileRadius = 0.13f; w.projectileColor = new Color(1f, 0.85f, 0.35f);
                w.heatPerShot = 1.5f;
            }),
            Weapon(root, "W_Scattergun", w =>
            {
                w.id = "scattergun"; w.displayName = "스캐터건"; w.tag = WeaponTag.Ballistic; w.tier = 1; w.price = 22;
                w.description = "근거리 산탄. 강한 넉백.";
                w.damage = 4f; w.projectileCount = 4; w.spreadAngle = 14f; w.fireInterval = 0.9f; w.range = 5f; w.knockback = 4.5f;
                w.critChance = 0.05f; w.projectileSpeed = 15f; w.projectileRadius = 0.1f; w.projectileLifetime = 0.5f;
                w.projectileColor = new Color(1f, 0.75f, 0.3f); w.heatPerShot = 3f;
            }),
            Weapon(root, "W_Railgun", w =>
            {
                w.id = "railgun"; w.displayName = "레일건"; w.tag = WeaponTag.Ballistic; w.tier = 2; w.price = 35;
                w.description = "적을 꿰뚫는 고속 관통탄. 발열이 크다.";
                w.damage = 18f; w.fireInterval = 1.4f; w.range = 10f; w.pierce = 3; w.knockback = 2f; w.critChance = 0.15f;
                w.projectileSpeed = 32f; w.projectileRadius = 0.11f; w.projectileLifetime = 0.8f;
                w.projectileColor = new Color(1f, 1f, 0.6f); w.heatPerShot = 6f;
            }),
            Weapon(root, "W_Laser", w =>
            {
                w.id = "laser"; w.displayName = "펄스 레이저"; w.tag = WeaponTag.Energy; w.tier = 1; w.price = 24;
                w.description = "초고속 연사. 맞은 적에게 도트 피해.";
                w.damage = 2.5f; w.fireInterval = 0.18f; w.range = 6.5f; w.knockback = 0.5f; w.critChance = 0.05f;
                w.projectileSpeed = 24f; w.projectileRadius = 0.08f; w.projectileLifetime = 0.6f;
                w.projectileColor = new Color(1f, 0.35f, 0.9f); w.heatPerShot = 0.6f;
                w.burnDamagePerSecond = 3f; w.burnDuration = 2f;
            }),
            Weapon(root, "W_CryoShard", w =>
            {
                w.id = "cryo_shard"; w.displayName = "크라이오 샤드"; w.tag = WeaponTag.Cryo; w.tier = 1; w.price = 20;
                w.description = "적을 둔화시킨다. 장착 시 코어 발열 -8%.";
                w.damage = 5f; w.fireInterval = 0.7f; w.range = 7f; w.knockback = 1f; w.critChance = 0.05f;
                w.projectileSpeed = 14f; w.projectileRadius = 0.14f;
                w.projectileColor = new Color(0.55f, 0.9f, 1f); w.heatPerShot = 1.2f;
                w.slowAmount = 0.4f; w.slowDuration = 1.5f;
            }),
            Weapon(root, "W_Grenade", w =>
            {
                w.id = "grenade"; w.displayName = "그레네이드 런처"; w.tag = WeaponTag.Explosive; w.tier = 1; w.price = 28;
                w.description = "착탄 시 광역 폭발.";
                w.damage = 9f; w.fireInterval = 1.2f; w.range = 6.5f; w.knockback = 5f; w.critChance = 0.05f;
                w.projectileSpeed = 10f; w.projectileRadius = 0.18f; w.projectileLifetime = 0.65f;
                w.projectileColor = new Color(1f, 0.5f, 0.2f); w.heatPerShot = 4.8f;
                w.explosionRadius = 1.6f;
            }),
        };

        public static List<ChipData> Chips(string root) => new()
        {
            Chip(root, "C_HeatSink", "heat_sink", "히트싱크", 1, 18, "기본 냉각 성능 강화.", (StatType.CoolingFlat, 3f)),
            Chip(root, "C_Coolant", "coolant", "냉각수 순환기", 1, 20, "발열 자체를 줄인다.", (StatType.HeatGenPct, -0.15f)),
            Chip(root, "C_ArmorPlate", "armor_plate", "장갑판", 1, 15, null, (StatType.MaxHP, 8f)),
            Chip(root, "C_Servo", "servo", "서보 모터", 1, 15, null, (StatType.MoveSpeedPct, 0.12f)),
            Chip(root, "C_Amplifier", "amplifier", "출력 증폭기", 1, 22, "화력과 발열을 함께 올린다.", (StatType.DamagePct, 0.2f), (StatType.HeatGenPct, 0.1f)),
            Chip(root, "C_Overclocker", "overclocker", "오버클럭 칩", 2, 26, "위험한 연사 강화.", (StatType.FireRatePct, 0.2f), (StatType.HeatGenPct, 0.15f)),
            Chip(root, "C_Magnet", "magnet", "자석 코일", 1, 12, null, (StatType.PickupRange, 1.5f)),
            Chip(root, "C_CritLens", "crit_lens", "조준 렌즈", 1, 18, null, (StatType.CritChance, 0.08f)),
        };

        static WeaponData Weapon(string root, string file, Action<WeaponData> apply) =>
            Upsert($"{root}/Data/Weapons/{file}.asset", apply);

        static ChipData Chip(string root, string file, string id, string name, int tier, int price, string desc,
            params (StatType stat, float value)[] mods) =>
            Upsert<ChipData>($"{root}/Data/Chips/{file}.asset", c =>
            {
                c.id = id; c.displayName = name; c.tier = tier; c.price = price; c.description = desc ?? "";
                c.modifiers = new List<StatModifier>();
                foreach (var (stat, value) in mods) c.modifiers.Add(new StatModifier { stat = stat, value = value });
            });

        static T Upsert<T>(string path, Action<T> apply) where T : ScriptableObject
        {
            var asset = ProjectSetup.LoadOrCreate<T>(path, _ => { });
            apply(asset);
            EditorUtility.SetDirty(asset);
            return asset;
        }
    }
}
