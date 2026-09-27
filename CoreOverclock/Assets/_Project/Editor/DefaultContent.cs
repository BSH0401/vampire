using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CoreOverclock.EditorTools
{
    /// <summary>
    /// Phase 3 content database (기획서 8장): 5 enemies + 3 bosses, 15 weapons, 25 chipsets, waves 1-20.
    /// Setup re-applies these values every run, so this file is the balancing source of truth.
    /// Heat rule of thumb: ~3 heat/s per weapon, so 4-5 weapons reach overclock and more melt down without cooling.
    /// </summary>
    public static class DefaultContent
    {
        // ─────────────────────────────── Weapons ───────────────────────────────

        public static List<WeaponData> Weapons(string root) => new()
        {
            // 탄도 (Ballistic): pierce / knockback
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
            Weapon(root, "W_Minigun", w =>
            {
                w.id = "minigun"; w.displayName = "미니건"; w.tag = WeaponTag.Ballistic; w.tier = 2; w.price = 32;
                w.description = "탄막을 쏟아붓는다. 열이 빠르게 오른다.";
                w.damage = 3.5f; w.fireInterval = 0.1f; w.range = 6.5f; w.knockback = 0.8f; w.critChance = 0.05f;
                w.projectileSpeed = 20f; w.projectileRadius = 0.08f; w.projectileLifetime = 0.5f;
                w.projectileColor = new Color(1f, 0.9f, 0.5f); w.heatPerShot = 0.55f;
            }),
            Weapon(root, "W_FlakCannon", w =>
            {
                w.id = "flak"; w.displayName = "플랙 캐논"; w.tag = WeaponTag.Ballistic; w.tier = 3; w.price = 48;
                w.description = "관통 파편을 흩뿌리는 중화기.";
                w.damage = 9f; w.projectileCount = 6; w.spreadAngle = 20f; w.fireInterval = 1.1f; w.range = 6f; w.pierce = 1;
                w.knockback = 5f; w.critChance = 0.08f; w.projectileSpeed = 16f; w.projectileRadius = 0.12f; w.projectileLifetime = 0.45f;
                w.projectileColor = new Color(1f, 0.7f, 0.25f); w.heatPerShot = 5f;
            }),

            // 레이저 (Energy): fire rate / burn
            Weapon(root, "W_Laser", w =>
            {
                w.id = "laser"; w.displayName = "펄스 레이저"; w.tag = WeaponTag.Energy; w.tier = 1; w.price = 24;
                w.description = "초고속 연사. 맞은 적에게 도트 피해.";
                w.damage = 2.5f; w.fireInterval = 0.18f; w.range = 6.5f; w.knockback = 0.5f; w.critChance = 0.05f;
                w.projectileSpeed = 24f; w.projectileRadius = 0.08f; w.projectileLifetime = 0.6f;
                w.projectileColor = new Color(1f, 0.35f, 0.9f); w.heatPerShot = 0.6f;
                w.burnDamagePerSecond = 3f; w.burnDuration = 2f;
            }),
            Weapon(root, "W_ArcBolt", w =>
            {
                w.id = "arc_bolt"; w.displayName = "아크 볼트"; w.tag = WeaponTag.Energy; w.tier = 2; w.price = 30;
                w.description = "적 두 명을 꿰뚫는 전격탄.";
                w.damage = 8f; w.fireInterval = 0.45f; w.range = 7.5f; w.pierce = 2; w.knockback = 1f; w.critChance = 0.08f;
                w.projectileSpeed = 20f; w.projectileRadius = 0.1f; w.projectileLifetime = 0.6f;
                w.projectileColor = new Color(0.8f, 0.5f, 1f); w.heatPerShot = 2f;
                w.burnDamagePerSecond = 4f; w.burnDuration = 2f;
            }),
            Weapon(root, "W_PlasmaLance", w =>
            {
                w.id = "plasma_lance"; w.displayName = "플라즈마 랜스"; w.tag = WeaponTag.Energy; w.tier = 2; w.price = 38;
                w.description = "일직선의 적을 모두 태운다.";
                w.damage = 16f; w.fireInterval = 1f; w.range = 9f; w.pierce = 6; w.knockback = 1.5f; w.critChance = 0.1f;
                w.projectileSpeed = 26f; w.projectileRadius = 0.14f; w.projectileLifetime = 0.6f;
                w.projectileColor = new Color(1f, 0.4f, 0.75f); w.heatPerShot = 4.5f;
                w.burnDamagePerSecond = 6f; w.burnDuration = 2.5f;
            }),
            Weapon(root, "W_PrismBeam", w =>
            {
                w.id = "prism"; w.displayName = "프리즘 빔"; w.tag = WeaponTag.Energy; w.tier = 3; w.price = 50;
                w.description = "다섯 갈래로 갈라지는 광선.";
                w.damage = 5f; w.projectileCount = 5; w.spreadAngle = 22f; w.fireInterval = 0.35f; w.range = 7f;
                w.knockback = 0.5f; w.critChance = 0.06f; w.projectileSpeed = 22f; w.projectileRadius = 0.09f; w.projectileLifetime = 0.5f;
                w.projectileColor = new Color(0.95f, 0.55f, 1f); w.heatPerShot = 2.2f;
                w.burnDamagePerSecond = 5f; w.burnDuration = 2f;
            }),

            // 냉각 (Cryo): slow, lowers global heat
            Weapon(root, "W_CryoShard", w =>
            {
                w.id = "cryo_shard"; w.displayName = "크라이오 샤드"; w.tag = WeaponTag.Cryo; w.tier = 1; w.price = 20;
                w.description = "적을 둔화시킨다. 장착 시 코어 발열 -8%.";
                w.damage = 5f; w.fireInterval = 0.7f; w.range = 7f; w.knockback = 1f; w.critChance = 0.05f;
                w.projectileSpeed = 14f; w.projectileRadius = 0.14f;
                w.projectileColor = new Color(0.55f, 0.9f, 1f); w.heatPerShot = 1.2f;
                w.slowAmount = 0.4f; w.slowDuration = 1.5f;
            }),
            Weapon(root, "W_FrostNeedler", w =>
            {
                w.id = "frost_needler"; w.displayName = "프로스트 니들러"; w.tag = WeaponTag.Cryo; w.tier = 2; w.price = 30;
                w.description = "빙결 침을 연사해 강하게 둔화시킨다.";
                w.damage = 3.5f; w.fireInterval = 0.15f; w.range = 7f; w.knockback = 0.3f; w.critChance = 0.05f;
                w.projectileSpeed = 22f; w.projectileRadius = 0.07f; w.projectileLifetime = 0.5f;
                w.projectileColor = new Color(0.7f, 0.95f, 1f); w.heatPerShot = 0.35f;
                w.slowAmount = 0.5f; w.slowDuration = 1.2f;
            }),
            Weapon(root, "W_GlacierCannon", w =>
            {
                w.id = "glacier"; w.displayName = "글레이셔 캐논"; w.tag = WeaponTag.Cryo; w.tier = 3; w.price = 46;
                w.description = "착탄 지점을 얼려 광역 둔화.";
                w.damage = 22f; w.fireInterval = 1.3f; w.range = 8f; w.knockback = 3f; w.critChance = 0.08f;
                w.projectileSpeed = 11f; w.projectileRadius = 0.22f; w.projectileLifetime = 0.75f;
                w.projectileColor = new Color(0.5f, 0.85f, 1f); w.heatPerShot = 3.5f;
                w.slowAmount = 0.6f; w.slowDuration = 2.5f; w.explosionRadius = 2f;
            }),

            // 폭발 (Explosive): area damage
            Weapon(root, "W_Grenade", w =>
            {
                w.id = "grenade"; w.displayName = "그레네이드 런처"; w.tag = WeaponTag.Explosive; w.tier = 1; w.price = 28;
                w.description = "착탄 시 광역 폭발.";
                w.damage = 9f; w.fireInterval = 1.2f; w.range = 6.5f; w.knockback = 5f; w.critChance = 0.05f;
                w.projectileSpeed = 10f; w.projectileRadius = 0.18f; w.projectileLifetime = 0.65f;
                w.projectileColor = new Color(1f, 0.5f, 0.2f); w.heatPerShot = 4.8f;
                w.explosionRadius = 1.6f;
            }),
            Weapon(root, "W_RocketPod", w =>
            {
                w.id = "rocket_pod"; w.displayName = "로켓 포드"; w.tag = WeaponTag.Explosive; w.tier = 2; w.price = 36;
                w.description = "소형 로켓 두 발을 연속 발사.";
                w.damage = 11f; w.projectileCount = 2; w.spreadAngle = 8f; w.fireInterval = 0.9f; w.range = 8f; w.knockback = 4f;
                w.critChance = 0.06f; w.projectileSpeed = 16f; w.projectileRadius = 0.14f; w.projectileLifetime = 0.6f;
                w.projectileColor = new Color(1f, 0.6f, 0.3f); w.heatPerShot = 4.5f;
                w.explosionRadius = 1.3f;
            }),
            Weapon(root, "W_ClusterMortar", w =>
            {
                w.id = "cluster_mortar"; w.displayName = "클러스터 박격포"; w.tag = WeaponTag.Explosive; w.tier = 3; w.price = 52;
                w.description = "거대한 폭발로 무리를 쓸어버린다.";
                w.damage = 26f; w.fireInterval = 1.6f; w.range = 9f; w.knockback = 7f; w.critChance = 0.08f;
                w.projectileSpeed = 9f; w.projectileRadius = 0.26f; w.projectileLifetime = 1f;
                w.projectileColor = new Color(1f, 0.4f, 0.15f); w.heatPerShot = 7f;
                w.explosionRadius = 2.6f;
            }),
        };

        // ─────────────────────────────── Chipsets ───────────────────────────────

        public static List<ChipData> Chips(string root) => new()
        {
            // Tier 1
            Chip(root, "C_HeatSink", "heat_sink", "히트싱크", 1, 18, "기본 냉각 성능 강화.", (StatType.CoolingFlat, 3f)),
            Chip(root, "C_Coolant", "coolant", "냉각수 순환기", 1, 20, "발열 자체를 줄인다.", (StatType.HeatGenPct, -0.15f)),
            Chip(root, "C_ArmorPlate", "armor_plate", "장갑판", 1, 15, null, (StatType.MaxHP, 8f)),
            Chip(root, "C_Servo", "servo", "서보 모터", 1, 15, null, (StatType.MoveSpeedPct, 0.12f)),
            Chip(root, "C_Amplifier", "amplifier", "출력 증폭기", 1, 22, "화력과 발열을 함께 올린다.", (StatType.DamagePct, 0.2f), (StatType.HeatGenPct, 0.1f)),
            Chip(root, "C_Magnet", "magnet", "자석 코일", 1, 12, null, (StatType.PickupRange, 1.5f)),
            Chip(root, "C_CritLens", "crit_lens", "조준 렌즈", 1, 18, null, (StatType.CritChance, 0.08f)),
            Chip(root, "C_RepairBot", "repair_bot", "나노 수리봇", 1, 20, "전투 중 체력을 서서히 회복.", (StatType.RegenPerSec, 0.4f)),
            Chip(root, "C_VentFan", "vent_fan", "방열 팬", 1, 16, "긴급 방열을 더 자주 쓸 수 있다.", (StatType.VentCooldownPct, -0.25f)),
            Chip(root, "C_Scope", "scope", "광학 조준기", 1, 16, null, (StatType.RangePct, 0.15f)),
            Chip(root, "C_Scavenger", "scavenger", "스크랩 수집기", 1, 18, "경제 빌드의 시작.", (StatType.ScrapGainPct, 0.2f)),
            Chip(root, "C_Hydraulics", "hydraulics", "강화 유압", 1, 15, null, (StatType.KnockbackPct, 0.3f), (StatType.DamagePct, 0.05f)),
            Chip(root, "C_Actuator", "actuator", "고속 구동기", 1, 20, null, (StatType.FireRatePct, 0.12f)),

            // Tier 2
            Chip(root, "C_Overclocker", "overclocker", "오버클럭 칩", 2, 26, "위험한 연사 강화.", (StatType.FireRatePct, 0.2f), (StatType.HeatGenPct, 0.15f)),
            Chip(root, "C_ReactiveArmor", "reactive_armor", "반응 장갑", 2, 26, null, (StatType.ArmorPct, 0.1f)),
            Chip(root, "C_LN2Tank", "ln2_tank", "액체질소 탱크", 2, 28, "강력한 냉각, 대신 무겁다.", (StatType.CoolingFlat, 6f), (StatType.MoveSpeedPct, -0.05f)),
            Chip(root, "C_OverdriveCoil", "overdrive_coil", "오버드라이브 코일", 2, 28, "오버클럭 구간의 화력을 더 끌어올린다.", (StatType.OverclockDamagePct, 0.25f)),
            Chip(root, "C_HEWarhead", "he_warhead", "고폭 탄두", 2, 24, null, (StatType.ExplosionRadiusPct, 0.2f), (StatType.DamagePct, 0.05f)),
            Chip(root, "C_ThermalCore", "thermal_core", "열전도 코어", 2, 24, null, (StatType.BurnDamagePct, 0.3f)),
            Chip(root, "C_TitaniumFrame", "titanium_frame", "티타늄 프레임", 2, 26, null, (StatType.MaxHP, 15f), (StatType.MoveSpeedPct, -0.05f)),
            Chip(root, "C_MeltdownFuse", "meltdown_fuse", "멜트다운 퓨즈", 2, 24, "과열되어도 금방 복구된다.", (StatType.MeltdownDurationPct, -0.4f)),

            // Tier 3
            Chip(root, "C_GlassCannon", "glass_cannon", "유리 대포", 3, 36, "모 아니면 도.", (StatType.DamagePct, 0.4f), (StatType.MaxHP, -10f)),
            Chip(root, "C_QuantumSight", "quantum_sight", "양자 조준", 3, 40, null, (StatType.CritChance, 0.15f), (StatType.DamagePct, 0.1f)),
            Chip(root, "C_CryoSystem", "cryo_system", "극저온 시스템", 3, 42, "발열 걱정을 덜어준다.", (StatType.HeatGenPct, -0.25f), (StatType.CoolingFlat, 3f)),
            Chip(root, "C_OverloadAmp", "overload_amp", "과부하 증폭기", 3, 40, "오버클럭에 모든 것을 건다.", (StatType.OverclockDamagePct, 0.5f), (StatType.HeatGenPct, 0.2f)),
        };

        // ─────────────────────────────── Enemies ───────────────────────────────

        public class EnemySet
        {
            public EnemyData ScrapBit, ChargeRunner, TurretDrone, MagmaBurst, CoreGolem;
            public EnemyData Juggernaut, Overseer, LegionCore;
        }

        public static EnemySet Enemies(string root)
        {
            var set = new EnemySet();
            set.ScrapBit = Enemy(root, "E_ScrapBit", e =>
            {
                e.id = "scrap_bit"; e.displayName = "스크랩 비트"; e.shape = EnemyShape.Triangle; e.behaviour = EnemyBehaviour.Chaser;
                e.color = new Color(0.92f, 0.95f, 1f); e.scale = 0.7f;
                e.maxHP = 10f; e.moveSpeed = 2.6f; e.contactDamage = 3f; e.scrapDrop = 1;
            });
            set.ChargeRunner = Enemy(root, "E_ChargeRunner", e =>
            {
                e.id = "charge_runner"; e.displayName = "차지 러너"; e.shape = EnemyShape.Arrow; e.behaviour = EnemyBehaviour.Charger;
                e.color = new Color(1f, 0.85f, 0.25f); e.scale = 0.8f;
                e.maxHP = 16f; e.moveSpeed = 2.3f; e.contactDamage = 5f; e.knockbackResistance = 0.2f; e.scrapDrop = 2;
                e.attackRange = 5.5f; e.windupTime = 1f; e.chargeSpeed = 12f; e.chargeDuration = 0.5f; e.recoverTime = 0.8f;
            });
            set.TurretDrone = Enemy(root, "E_TurretDrone", e =>
            {
                e.id = "turret_drone"; e.displayName = "터렛 드론"; e.shape = EnemyShape.Octagon; e.behaviour = EnemyBehaviour.Turret;
                e.color = new Color(0.75f, 0.4f, 1f); e.scale = 0.85f;
                e.maxHP = 20f; e.moveSpeed = 1.8f; e.contactDamage = 3f; e.scrapDrop = 2;
                e.preferredDistance = 6.5f; e.attackInterval = 3f; e.projectileSpeed = 6f; e.projectileDamage = 4f;
            });
            set.MagmaBurst = Enemy(root, "E_MagmaBurst", e =>
            {
                e.id = "magma_burst"; e.displayName = "마그마 버스트"; e.shape = EnemyShape.Circle; e.behaviour = EnemyBehaviour.Bomber;
                e.color = new Color(1f, 0.3f, 0.2f); e.scale = 0.75f;
                e.maxHP = 12f; e.moveSpeed = 2.5f; e.contactDamage = 4f; e.scrapDrop = 1;
                e.fuseTime = 1.1f; e.triggerRange = 1.8f; e.explosionRadius = 2.2f; e.explosionDamage = 8f; e.explosionEnemyDamage = 40f;
            });
            set.CoreGolem = Enemy(root, "E_CoreGolem", e =>
            {
                e.id = "core_golem"; e.displayName = "코어 골렘"; e.shape = EnemyShape.Square; e.behaviour = EnemyBehaviour.Golem;
                e.color = new Color(0.5f, 0.7f, 1f); e.scale = 1.8f;
                e.maxHP = 140f; e.moveSpeed = 1.3f; e.contactDamage = 7f; e.knockbackResistance = 0.85f; e.scrapDrop = 6;
                e.shieldRadius = 4f;
            });
            set.Juggernaut = Enemy(root, "B_Juggernaut", e =>
            {
                e.id = "juggernaut"; e.displayName = "저거너트"; e.shape = EnemyShape.Arrow; e.behaviour = EnemyBehaviour.Charger; e.isBoss = true;
                e.color = new Color(1f, 0.6f, 0.2f); e.scale = 2.4f;
                e.maxHP = 300f; e.moveSpeed = 1.9f; e.contactDamage = 5f; e.knockbackResistance = 1f; e.scrapDrop = 25;
                e.attackRange = 9f; e.windupTime = 1.25f; e.chargeSpeed = 13f; e.chargeDuration = 0.6f; e.recoverTime = 1.2f;
                e.projectileSpeed = 5.5f; e.projectileDamage = 3f;
            });
            set.Overseer = Enemy(root, "B_Overseer", e =>
            {
                e.id = "overseer"; e.displayName = "오버시어"; e.shape = EnemyShape.Octagon; e.behaviour = EnemyBehaviour.BossOverseer; e.isBoss = true;
                e.color = new Color(1f, 0.3f, 0.8f); e.scale = 2.8f;
                e.maxHP = 600f; e.moveSpeed = 1.6f; e.contactDamage = 6f; e.knockbackResistance = 1f; e.scrapDrop = 40;
                e.projectileSpeed = 5.5f; e.projectileDamage = 4f;
            });
            set.LegionCore = Enemy(root, "B_LegionCore", e =>
            {
                e.id = "legion_core"; e.displayName = "스크랩 레기온 코어"; e.shape = EnemyShape.Square; e.behaviour = EnemyBehaviour.BossLegion; e.isBoss = true;
                e.color = new Color(1f, 0.25f, 0.25f); e.scale = 3.2f;
                e.maxHP = 1300f; e.moveSpeed = 1.4f; e.contactDamage = 8f; e.knockbackResistance = 1f; e.scrapDrop = 80;
                e.projectileSpeed = 5.5f; e.projectileDamage = 5f;
                e.windupTime = 1.1f; e.chargeSpeed = 13f; e.chargeDuration = 0.7f;
                e.summon = set.ScrapBit;
            });
            return set;
        }

        // ─────────────────────────────── Waves (기획서 4.3) ───────────────────────────────

        public static void FillWaves(WaveTable table, EnemySet e)
        {
            table.waves.Clear();
            for (int w = 1; w <= 20; w++)
            {
                int n = w - 1;
                var def = new WaveDefinition
                {
                    duration = w switch { <= 4 => 30f, 5 => 40f, 15 => 60f, 20 => 90f, _ => 45f },
                    spawnInterval = Mathf.Max(0.3f, 1f - 0.035f * n),
                    spawnPerTick = 1 + w / 3,
                    hpMultiplier = 1f + 0.18f * n + 0.012f * n * n,
                    speedMultiplier = 1f + 0.015f * n,
                    // Kill counts grow faster than prices, so later kills drop scrap less often.
                    scrapDropChance = Mathf.Max(0.3f, 1f - 0.04f * n),
                };

                bool swarm = w >= 16 && w <= 19; // 극한의 시너지: mass spawns
                Add(def, e.ScrapBit, swarm ? 1.2f : 1f);
                if (w >= 3) Add(def, e.ChargeRunner, Mathf.Min(0.2f + 0.04f * (w - 3), 0.5f));
                if (w >= 4) Add(def, e.MagmaBurst, Mathf.Min(0.15f + 0.03f * (w - 4), 0.45f));
                if (w >= 6) Add(def, e.TurretDrone, Mathf.Min(0.2f + 0.03f * (w - 6), 0.45f));
                if (w >= 10) Add(def, e.CoreGolem, Mathf.Min(0.04f + 0.01f * (w - 10), 0.1f));

                if (swarm) def.spawnPerTick += 2;
                switch (w)
                {
                    case 5:
                        def.bosses.Add(e.Juggernaut);
                        break;
                    case 15:
                        def.bosses.Add(e.Overseer);
                        def.spawnInterval *= 2.2f;
                        break;
                    case 20:
                        def.bosses.Add(e.LegionCore);
                        def.spawnInterval *= 2.5f;
                        def.endOnBossKill = true;
                        break;
                }
                table.waves.Add(def);
            }
        }

        static void Add(WaveDefinition def, EnemyData enemy, float weight) =>
            def.enemies.Add(new WaveSpawnEntry { enemy = enemy, weight = weight });

        // ─────────────────────────────── Helpers ───────────────────────────────

        static WeaponData Weapon(string root, string file, Action<WeaponData> apply) =>
            Upsert($"{root}/Data/Weapons/{file}.asset", apply);

        static EnemyData Enemy(string root, string file, Action<EnemyData> apply) =>
            Upsert($"{root}/Data/Enemies/{file}.asset", apply);

        static ChipData Chip(string root, string file, string id, string name, int tier, int price, string desc,
            params (StatType stat, float value)[] mods) =>
            Upsert<ChipData>($"{root}/Data/Chips/{file}.asset", c =>
            {
                c.id = id; c.displayName = name; c.tier = tier; c.price = price; c.description = desc ?? "";
                c.modifiers = new List<StatModifier>();
                foreach (var (stat, value) in mods) c.modifiers.Add(new StatModifier { stat = stat, value = value });
            });

        public static T Upsert<T>(string path, Action<T> apply) where T : ScriptableObject
        {
            var asset = ProjectSetup.LoadOrCreate<T>(path, _ => { });
            apply(asset);
            EditorUtility.SetDirty(asset);
            return asset;
        }
    }
}
