using System;
using System.Collections.Generic;
using UnityEngine;

namespace CoreOverclock
{
    public class Player : MonoBehaviour
    {
        public const int MaxWeapons = 6;
        const float WeaponOrbitRadius = 0.75f;

        PlayerData data;
        Rigidbody2D rb;
        SpriteRenderer body, core, glow;
        Vector2 knock;
        float invulnerableTimer, flashTimer, wallZapCooldown;
        readonly List<Weapon> weapons = new();

        public float MaxHP => data.maxHP + Stats.MaxHP;
        public float HP { get; private set; }
        public bool IsAlive => HP > 0f;
        public float PickupRange => data.pickupRange + Stats.PickupRange;
        static PlayerStats Stats => GameManager.Instance.Loadout.Stats;
        public Vector2 Position => rb.position;
        public IReadOnlyList<Weapon> Weapons => weapons;

        public event Action Died;

        /// <summary>Damage taken this wave after armor (tracked even in -god test runs).</summary>
        public float DamageTakenThisWave { get; private set; }

        public static Player Create(PlayerData data, Transform parent)
        {
            var go = new GameObject("Player") { layer = GameLayers.Player };
            go.transform.SetParent(parent, false);
            var p = go.AddComponent<Player>();
            p.data = data;

            p.rb = go.AddComponent<Rigidbody2D>();
            p.rb.gravityScale = 0f;
            p.rb.mass = 50f;
            p.rb.freezeRotation = true;
            p.rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            p.rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            go.AddComponent<CircleCollider2D>().radius = 0.38f;

            p.glow = Visuals.Glow(go.transform, Palette.Player, 2.6f, 0.22f, 15);
            p.body = Visuals.Sprite("Body", go.transform, ShapeSprites.NeonCircle, Palette.Player, 20, 0.95f);
            p.core = Visuals.Sprite("Core", go.transform, ShapeSprites.Circle, Palette.Player, 21, 0.35f);
            p.HP = data.maxHP;
            return p;
        }

        /// <summary>Rebuilds the weapon mounts to match the loadout.</summary>
        public void SyncWeapons(Loadout loadout)
        {
            foreach (var w in weapons) Destroy(w.gameObject);
            weapons.Clear();
            foreach (var owned in loadout.Weapons) weapons.Add(Weapon.Create(owned.Data, transform));
            LayoutWeapons();
            HP = Mathf.Min(HP, MaxHP);
        }

        void LayoutWeapons()
        {
            for (int i = 0; i < weapons.Count; i++)
            {
                float angle = (90f + 360f / Mathf.Max(weapons.Count, 1) * i) * Mathf.Deg2Rad;
                weapons[i].transform.localPosition = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * WeaponOrbitRadius;
            }
        }

        public void ResetForWave()
        {
            HP = MaxHP;
            DamageTakenThisWave = 0f;
            knock = Vector2.zero;
            invulnerableTimer = 0f;
            rb.position = Vector2.zero;
            transform.position = Vector3.zero;
            rb.linearVelocity = Vector2.zero;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            invulnerableTimer -= dt;
            var gm = GameManager.Instance;
            if (IsAlive && gm && gm.State == GameState.Combat && Stats.RegenPerSec > 0f)
                HP = Mathf.Min(MaxHP, HP + Stats.RegenPerSec * dt);
            flashTimer -= dt;
            wallZapCooldown -= dt;

            bool blink = invulnerableTimer > 0f && Mathf.Repeat(Time.time * 16f, 1f) > 0.5f;
            body.color = flashTimer > 0f ? Color.white : Palette.Player;
            body.enabled = !blink;

            // The core glows hotter with heat: cyan → orange (overclock) → blinking red (meltdown).
            var heat = GameManager.Instance ? GameManager.Instance.Heat : null;
            Color coreColor = Palette.Player;
            float pulseSpeed = 6f;
            if (heat)
            {
                if (heat.State == HeatState.Meltdown)
                {
                    coreColor = Mathf.Repeat(Time.time * 6f, 1f) > 0.5f ? Palette.Danger : Color.white;
                    pulseSpeed = 20f;
                }
                else
                {
                    coreColor = Color.Lerp(Palette.Player, Palette.Overclock, Mathf.InverseLerp(30f, HeatSystem.OverclockThreshold, heat.Value));
                    if (heat.State == HeatState.Overclock) pulseSpeed = 14f;
                }
            }
            core.color = coreColor;
            glow.color = new Color(coreColor.r, coreColor.g, coreColor.b, 0.22f);
            float pulse = 0.32f + 0.06f * Mathf.Sin(Time.time * pulseSpeed);
            core.transform.localScale = Vector3.one * pulse;
        }

        void FixedUpdate()
        {
            var gm = GameManager.Instance;
            bool canMove = IsAlive && gm && gm.PlayerCanMove;
            Vector2 input = canMove ? GameInput.Move : Vector2.zero;
            rb.linearVelocity = input * (data.moveSpeed * Mathf.Max(0.3f, 1f + Stats.MoveSpeedPct)) + knock;
            knock = Vector2.MoveTowards(knock, Vector2.zero, 40f * Time.fixedDeltaTime);
        }

        void OnCollisionStay2D(Collision2D c)
        {
            var gm = GameManager.Instance;
            if (!IsAlive || !gm || gm.State != GameState.Combat) return;

            if (c.collider.TryGetComponent(out Enemy enemy) && enemy.IsAlive)
            {
                TakeDamage(enemy.ContactDamage, (Position - enemy.Position).normalized * 6f);
            }
            else if (c.gameObject.layer == GameLayers.Wall && wallZapCooldown <= 0f)
            {
                wallZapCooldown = 0.5f;
                var push = WallNormal(Position) * data.wallKnockback;
                FxSystem.Pulse(Position, Palette.Wall, 0.3f, 1.8f, 0.25f);
                CameraShake.Add(0.25f);
                TakeDamage(data.wallDamage, push);
            }
        }

        static Vector2 WallNormal(Vector2 p)
        {
            float dx = Arena.HalfSize.x - Mathf.Abs(p.x);
            float dy = Arena.HalfSize.y - Mathf.Abs(p.y);
            return dx < dy ? new Vector2(-Mathf.Sign(p.x), 0f) : new Vector2(0f, -Mathf.Sign(p.y));
        }

        public void TakeDamage(float amount, Vector2 push)
        {
            knock += push;
            if (invulnerableTimer > 0f || !IsAlive) return;

            amount *= 1f - Mathf.Clamp(Stats.ArmorPct, 0f, 0.7f);
            DamageTakenThisWave += amount;
            invulnerableTimer = data.invulnerableTime;
            if (DevCommandLine.God) return;
            HP = Mathf.Max(0f, HP - amount);
            flashTimer = 0.08f;
            DamagePopups.Show(Position, amount, Palette.Danger, false);
            CameraShake.Add(0.35f);
            TimeControl.HitStop();

            if (HP <= 0f) Die();
        }

        /// <summary>Meltdown damage: ignores invulnerability, no knockback or hit-stop.</summary>
        public void TakeTrueDamage(float amount)
        {
            if (!IsAlive) return;
            DamageTakenThisWave += amount;
            if (DevCommandLine.God) return;
            HP = Mathf.Max(0f, HP - amount);
            flashTimer = 0.08f;
            DamagePopups.Show(Position, amount, Palette.Overclock, false);
            if (HP <= 0f) Die();
        }

        void Die()
        {
            body.enabled = true;
            FxSystem.Pulse(Position, Palette.Player, 0.5f, 4f, 0.6f);
            Died?.Invoke();
        }
    }
}
