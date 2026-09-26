using UnityEngine;

namespace CoreOverclock
{
    /// <summary>
    /// Per-archetype enemy AI (기획서 5장). A brain returns the desired velocity each physics step;
    /// the <see cref="Enemy"/> body applies slow and knockback on top.
    /// </summary>
    public abstract class EnemyBrain
    {
        protected Enemy E;
        protected EnemyData D => E.Data;
        protected static Player Player => GameManager.Instance.Player;
        protected Vector2 ToPlayer => Player.Position - E.Position;
        protected float Speed => D.moveSpeed * E.SpeedMultiplier;
        protected Color Warning => new(1f, 0.35f, 0.3f, 0.35f);

        /// <summary>Direction the body sprite should face; zero = face velocity.</summary>
        public Vector2 Facing { get; protected set; }
        public virtual bool Spins => false;
        public virtual float ContactDamageMultiplier => 1f;

        public static EnemyBrain Create(EnemyBehaviour behaviour) => behaviour switch
        {
            EnemyBehaviour.Charger => new ChargerBrain(),
            EnemyBehaviour.Turret => new TurretBrain(),
            EnemyBehaviour.Bomber => new BomberBrain(),
            EnemyBehaviour.Golem => new GolemBrain(),
            EnemyBehaviour.BossOverseer => new OverseerBrain(),
            EnemyBehaviour.BossLegion => new LegionBrain(),
            _ => new ChaserBrain(),
        };

        public void Bind(Enemy enemy) => E = enemy;
        public virtual void Reset() => Facing = Vector2.zero;
        public abstract Vector2 Tick(float dt);
        public virtual void OnDeath(Vector2 at) { }
        public virtual Color TintColor(Color c) => c;

        protected Vector2 Chase(float speedScale = 1f)
        {
            var to = ToPlayer;
            return to.sqrMagnitude > 0.0001f ? to.normalized * (Speed * speedScale) : Vector2.zero;
        }

        protected Vector2 MuzzleToward(Vector2 dir) => E.Position + dir * (0.45f * D.scale);

        protected void Shoot(Vector2 dir, float speed, float damage, float radius = 0.16f) =>
            EnemyProjectileSystem.Fire(MuzzleToward(dir), dir * speed, damage, D.color, radius);

        protected void Ring(int count, float offsetDeg, float speed, float damage, float radius = 0.16f)
        {
            for (int i = 0; i < count; i++)
            {
                float a = (offsetDeg + 360f / count * i) * Mathf.Deg2Rad;
                Shoot(new Vector2(Mathf.Cos(a), Mathf.Sin(a)), speed, damage, radius);
            }
        }

        protected void Fan(Vector2 dir, int count, float spreadDeg, float speed, float damage)
        {
            float baseAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            for (int i = 0; i < count; i++)
            {
                float a = (baseAngle + (count == 1 ? 0f : Mathf.Lerp(-spreadDeg, spreadDeg, i / (count - 1f)))) * Mathf.Deg2Rad;
                Shoot(new Vector2(Mathf.Cos(a), Mathf.Sin(a)), speed, damage);
            }
        }

        protected static Vector2 Dir(float deg) => new(Mathf.Cos(deg * Mathf.Deg2Rad), Mathf.Sin(deg * Mathf.Deg2Rad));
    }

    /// <summary>스크랩 비트: straight chase.</summary>
    public class ChaserBrain : EnemyBrain
    {
        public override Vector2 Tick(float dt) => Chase();
    }

    /// <summary>차지 러너 / 저거너트: approach, stop and telegraph for ~1s, then dash in a straight line.</summary>
    public class ChargerBrain : EnemyBrain
    {
        enum State { Approach, Windup, Dash, Recover }

        State state;
        float timer;
        Vector2 dashDir;

        public override float ContactDamageMultiplier => state == State.Dash ? 1.5f : 1f;

        public override void Reset()
        {
            base.Reset();
            state = State.Approach;
            timer = Random.Range(0f, 0.5f);
        }

        public override Vector2 Tick(float dt)
        {
            timer -= dt;
            switch (state)
            {
                case State.Approach:
                    Facing = Vector2.zero;
                    if (timer <= 0f && ToPlayer.magnitude < D.attackRange)
                    {
                        state = State.Windup;
                        timer = D.windupTime;
                        dashDir = ToPlayer.normalized;
                    }
                    return Chase();

                case State.Windup:
                    // Tracks the player for the first half, then locks the lane so it can be dodged sideways.
                    if (timer > D.windupTime * 0.5f && ToPlayer.sqrMagnitude > 0.01f) dashDir = ToPlayer.normalized;
                    Facing = dashDir;
                    var c = Warning;
                    c.a = 0.2f + 0.3f * Mathf.PingPong(Time.time * 6f, 1f);
                    E.ShowTelegraph(E.Position, dashDir, D.chargeSpeed * D.chargeDuration, 0.12f * D.scale, c);
                    if (timer <= 0f)
                    {
                        state = State.Dash;
                        timer = D.chargeDuration;
                        E.HideTelegraph();
                    }
                    return Vector2.zero;

                case State.Dash:
                    if (timer <= 0f)
                    {
                        state = State.Recover;
                        timer = D.recoverTime;
                        if (D.isBoss)
                        {
                            // Juggernaut: slam at the end of each charge.
                            Ring(14, Random.Range(0f, 30f), D.projectileSpeed, D.projectileDamage, 0.2f);
                            CameraShake.Add(0.3f);
                        }
                    }
                    return dashDir * (D.chargeSpeed * E.SpeedMultiplier);

                default:
                    if (timer <= 0f) { state = State.Approach; timer = 0f; }
                    return Chase(0.3f);
            }
        }

        public override Color TintColor(Color c) =>
            state == State.Windup && Mathf.Repeat(Time.time * 10f, 1f) > 0.5f ? Color.white : c;
    }

    /// <summary>터렛 드론: keeps its distance and fires a single aimed shot every few seconds.</summary>
    public class TurretBrain : EnemyBrain
    {
        const float AimWarning = 0.4f;
        float fireTimer, strafeSign;

        public override void Reset()
        {
            base.Reset();
            fireTimer = D.attackInterval * Random.Range(0.4f, 1f);
            strafeSign = Random.value < 0.5f ? -1f : 1f;
        }

        public override Vector2 Tick(float dt)
        {
            var to = ToPlayer;
            float dist = to.magnitude;
            var dir = dist > 0.001f ? to / dist : Vector2.right;
            Facing = dir;

            fireTimer -= dt;
            if (fireTimer <= AimWarning && dist < D.preferredDistance + 4f)
            {
                var c = Warning;
                c.a = 0.15f + 0.2f * (1f - fireTimer / AimWarning);
                E.ShowTelegraph(MuzzleToward(dir), dir, Mathf.Min(dist, 9f), 0.05f, c);
            }
            if (fireTimer <= 0f)
            {
                E.HideTelegraph();
                fireTimer = D.attackInterval;
                if (dist < D.preferredDistance + 4f) Shoot(dir, D.projectileSpeed, D.projectileDamage, 0.18f);
            }

            if (dist > D.preferredDistance + 1f) return dir * Speed;
            if (dist < D.preferredDistance - 1f) return -dir * Speed;
            return new Vector2(-dir.y, dir.x) * (strafeSign * Speed * 0.5f);
        }
    }

    /// <summary>마그마 버스트: arms a fuse near the player and explodes, damaging everything around it.</summary>
    public class BomberBrain : EnemyBrain
    {
        float fuse;
        bool exploded;

        public override void Reset()
        {
            base.Reset();
            fuse = -1f;
            exploded = false;
        }

        public override Vector2 Tick(float dt)
        {
            if (fuse < 0f)
            {
                if (ToPlayer.magnitude < D.triggerRange) fuse = D.fuseTime;
                return Chase();
            }

            fuse -= dt;
            if (fuse <= 0f)
            {
                Explode(E.Position);
                E.SelfDestruct();
                return Vector2.zero;
            }
            return Chase(0.25f);
        }

        public override void OnDeath(Vector2 at) => Explode(at);

        void Explode(Vector2 at)
        {
            if (exploded) return;
            exploded = true;
            Enemy.Blast(at, D.explosionRadius, D.explosionDamage, D.explosionEnemyDamage * E.HPMultiplier, D.color, E);
        }

        public override Color TintColor(Color c)
        {
            float rate = fuse >= 0f ? 14f : 2.5f;
            return Mathf.Repeat(Time.time * rate, 1f) > 0.5f ? c : Color.Lerp(c, Color.white, fuse >= 0f ? 0.9f : 0.35f);
        }
    }

    /// <summary>코어 골렘: slow tank that shields nearby enemies (50% damage reduction).</summary>
    public class GolemBrain : EnemyBrain
    {
        float auraTimer, pulseTimer;

        public override void Reset()
        {
            base.Reset();
            auraTimer = pulseTimer = 0f;
        }

        public override Vector2 Tick(float dt)
        {
            auraTimer -= dt;
            pulseTimer -= dt;
            if (auraTimer <= 0f)
            {
                auraTimer = 0.5f;
                float r2 = D.shieldRadius * D.shieldRadius;
                foreach (var e in Enemy.Active)
                {
                    if (e == E || !e.IsAlive || e.IsBoss || e.Data.behaviour == EnemyBehaviour.Golem) continue;
                    if ((e.Position - E.Position).sqrMagnitude <= r2) e.ApplyShield(0.8f);
                }
            }
            if (pulseTimer <= 0f)
            {
                pulseTimer = 1.2f;
                FxSystem.Pulse(E.Position, new Color(0.6f, 0.8f, 1f, 0.35f), 0.5f, D.shieldRadius * 2.4f, 0.9f);
            }
            return Chase();
        }
    }

    /// <summary>Shared pattern runner for the two bosses.</summary>
    public abstract class BossBrain : EnemyBrain
    {
        protected float patternTimer, shotTimer, angle, moveTimer;
        protected int pattern, volley;
        protected Vector2 waypoint;

        public override bool Spins => true;
        protected float HPRatio => E.MaxHP > 0f ? E.HP / E.MaxHP : 1f;

        public override void Reset()
        {
            base.Reset();
            patternTimer = 2f;
            shotTimer = 0f;
            angle = 0f;
            pattern = -1;
            volley = 0;
            moveTimer = 0f;
            waypoint = E.Position;
        }

        protected Vector2 Wander(float dt, float inset)
        {
            moveTimer -= dt;
            if (moveTimer <= 0f)
            {
                moveTimer = 3.5f;
                waypoint = Vector2.Lerp(Arena.RandomPoint(inset), Player.Position, 0.35f);
            }
            var to = waypoint - E.Position;
            return to.magnitude > 0.3f ? to.normalized * Speed : Vector2.zero;
        }
    }

    /// <summary>
    /// 오버시어 (Wave 15 중간 보스): cycles radial bursts, a spiral stream and aimed fans.
    /// Below 50% HP every pattern fires faster.
    /// </summary>
    public class OverseerBrain : BossBrain
    {
        public override Vector2 Tick(float dt)
        {
            float rate = HPRatio < 0.5f ? 1.4f : 1f;
            patternTimer -= dt * rate;
            shotTimer -= dt * rate;
            float spd = D.projectileSpeed, dmg = D.projectileDamage;

            if (patternTimer <= 0f)
            {
                pattern = (pattern + 1) % 3;
                patternTimer = pattern switch { 0 => 1.8f, 1 => 3.2f, _ => 2.2f } + 1f; // +1s breather
                shotTimer = 0f;
                volley = 0;
            }

            if (shotTimer <= 0f && patternTimer > 1f)
            {
                switch (pattern)
                {
                    case 0: // radial bursts
                        Ring(18, angle, spd, dmg);
                        angle += 10f;
                        shotTimer = 0.55f;
                        break;
                    case 1: // spiral
                        Shoot(Dir(angle), spd * 0.9f, dmg);
                        Shoot(Dir(angle + 180f), spd * 0.9f, dmg);
                        angle += 13f;
                        shotTimer = 0.07f;
                        break;
                    default: // aimed fans
                        Fan(ToPlayer.normalized, 5, 25f, spd * 1.2f, dmg);
                        shotTimer = 0.45f;
                        break;
                }
            }
            return Wander(dt, 4f) * (pattern == 1 ? 0.3f : 1f);
        }
    }

    /// <summary>
    /// 스크랩 레기온 코어 (Wave 20 최종 보스). Three phases by HP:
    ///  1) radial bursts + minion rings, 2) triple spiral + telegraphed charges, 3) double spiral + fans + minions.
    /// </summary>
    public class LegionBrain : BossBrain
    {
        float summonTimer, chargeTimer, dashTimer;
        Vector2 dashDir;
        bool winding;

        int Phase => HPRatio > 0.6f ? 1 : HPRatio > 0.25f ? 2 : 3;
        public override float ContactDamageMultiplier => dashTimer > 0f ? 1.5f : 1f;

        public override void Reset()
        {
            base.Reset();
            summonTimer = 4f;
            chargeTimer = 5f;
            dashTimer = 0f;
            winding = false;
        }

        public override Vector2 Tick(float dt)
        {
            float spd = D.projectileSpeed, dmg = D.projectileDamage;
            int phase = Phase;
            shotTimer -= dt;
            summonTimer -= dt;

            if (phase != 2 && summonTimer <= 0f && D.summon)
            {
                summonTimer = phase == 1 ? 7f : 8f;
                for (int i = 0; i < 6; i++)
                    GameManager.Instance.Spawner.SpawnNow(D.summon, E.Position + Dir(60f * i) * (2.2f * D.scale * 0.5f + 1.2f));
            }

            switch (phase)
            {
                case 1:
                    if (shotTimer <= 0f)
                    {
                        Ring(22, angle, spd, dmg);
                        angle += 8f;
                        shotTimer = 2.2f;
                    }
                    return Wander(dt, 4.5f) * 0.6f;

                case 2:
                    if (dashTimer > 0f)
                    {
                        dashTimer -= dt;
                        return dashDir * (D.chargeSpeed * E.SpeedMultiplier);
                    }
                    chargeTimer -= dt;
                    if (!winding && chargeTimer <= D.windupTime)
                    {
                        winding = true;
                        dashDir = ToPlayer.normalized;
                    }
                    if (winding)
                    {
                        var c = Warning;
                        c.a = 0.25f + 0.3f * Mathf.PingPong(Time.time * 6f, 1f);
                        E.ShowTelegraph(E.Position, dashDir, D.chargeSpeed * D.chargeDuration, 0.5f, c);
                        if (chargeTimer <= 0f)
                        {
                            winding = false;
                            E.HideTelegraph();
                            dashTimer = D.chargeDuration;
                            chargeTimer = 6f;
                        }
                        return Vector2.zero;
                    }
                    if (shotTimer <= 0f)
                    {
                        for (int k = 0; k < 3; k++) Shoot(Dir(angle + 120f * k), spd * 0.85f, dmg);
                        angle += 11f;
                        shotTimer = 0.1f;
                    }
                    return Wander(dt, 4.5f) * 0.4f;

                default:
                    patternTimer -= dt;
                    if (shotTimer <= 0f)
                    {
                        Shoot(Dir(angle), spd, dmg);
                        Shoot(Dir(angle + 180f), spd, dmg);
                        Shoot(Dir(-angle * 1.3f + 90f), spd * 0.8f, dmg);
                        angle += 12f;
                        shotTimer = 0.09f;
                    }
                    if (patternTimer <= 0f)
                    {
                        patternTimer = 1.5f;
                        Fan(ToPlayer.normalized, 7, 35f, spd * 1.25f, dmg);
                    }
                    return Wander(dt, 4f) * 0.8f;
            }
        }

        public override Color TintColor(Color c) =>
            Phase == 3 ? Color.Lerp(c, Color.white, 0.3f * Mathf.PingPong(Time.time * 4f, 1f)) : c;
    }
}
