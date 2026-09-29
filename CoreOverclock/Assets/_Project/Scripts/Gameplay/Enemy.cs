using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CoreOverclock
{
    /// <summary>
    /// Pooled enemy body: physics, health, status effects and visuals.
    /// Behaviour (기획서 5장) lives in an <see cref="EnemyBrain"/> chosen by <see cref="EnemyData.behaviour"/>.
    /// </summary>
    public class Enemy : MonoBehaviour, IDamageable
    {
        public static readonly List<Enemy> Active = new();

        const float FlashTime = 0.05f; // 기획서 7.3 Sprite Flash
        const float ShieldDamageMultiplier = 0.5f;

        Rigidbody2D rb;
        CircleCollider2D col;
        SpriteRenderer body, glow, telegraph, shieldRing, hpBack, hpFill;
        Transform bodyTransform;
        Action<Enemy> release;
        readonly Dictionary<EnemyBehaviour, EnemyBrain> brains = new();
        EnemyBrain brain;
        Vector2 knock;
        float hp, flashTimer, shieldTimer;
        float slowAmount, slowTimer, burnDps, burnTimer, burnTick;
        bool frozen, spawned;

        public EnemyData Data { get; private set; }
        public bool IsAlive => spawned && !frozen && hp > 0f;
        public bool IsBoss => Data && Data.isBoss;
        public Vector2 Position => rb.position;
        public float ContactDamage => Data.contactDamage * brain.ContactDamageMultiplier;
        public float SpeedMultiplier { get; private set; }
        public float HPMultiplier { get; private set; }
        public float MaxHP { get; private set; }
        public float HP => hp;
        public bool Shielded => shieldTimer > 0f;
        public bool Slowed => slowTimer > 0f;
        bool ShowsHPBar => !Data.isBoss && Data.scale >= 1.5f;

        public static Enemy CreateInstance(Transform parent)
        {
            var go = new GameObject("Enemy") { layer = GameLayers.Enemy };
            go.transform.SetParent(parent, false);
            var e = go.AddComponent<Enemy>();
            e.rb = go.AddComponent<Rigidbody2D>();
            e.rb.gravityScale = 0f;
            e.rb.mass = 1f;
            e.rb.freezeRotation = true;
            e.col = go.AddComponent<CircleCollider2D>();
            e.col.radius = 0.42f;
            e.glow = Visuals.Glow(go.transform, Color.white, 2.2f, 0.2f, 9);
            e.body = Visuals.Sprite("Body", go.transform, ShapeSprites.NeonCircle, Color.white, 10);
            e.bodyTransform = e.body.transform;
            e.shieldRing = Visuals.Sprite("Shield", go.transform, ShapeSprites.Ring, new Color(0.6f, 0.8f, 1f, 0.7f), 11, 1.35f);
            e.shieldRing.enabled = false;
            // Mini HP bar for elites (코어 골렘); bosses use the HUD bar instead.
            e.hpBack = Visuals.Sprite("HPBack", go.transform, ShapeSprites.White, new Color(0f, 0f, 0f, 0.6f), 12);
            e.hpBack.transform.localPosition = new Vector3(0f, 0.72f, 0f);
            e.hpBack.transform.localScale = new Vector3(0.95f, 0.1f, 1f);
            e.hpFill = Visuals.Sprite("HPFill", go.transform, ShapeSprites.White, new Color(1f, 0.35f, 0.4f), 13);
            e.hpBack.enabled = e.hpFill.enabled = false;
            // Telegraph is parented to the pool root so enemy scale/rotation don't affect it.
            e.telegraph = Visuals.Sprite("Telegraph", parent, ShapeSprites.White, Color.white, 2);
            e.telegraph.enabled = false;
            return e;
        }

        public void Spawn(EnemyData data, Vector2 position, float hpMultiplier, float speedMul, Action<Enemy> onRelease)
        {
            Data = data;
            release = onRelease;
            HPMultiplier = hpMultiplier;
            MaxHP = hp = data.maxHP * hpMultiplier;
            SpeedMultiplier = speedMul;
            knock = Vector2.zero;
            flashTimer = shieldTimer = 0f;
            slowAmount = slowTimer = burnDps = burnTimer = burnTick = 0f;
            frozen = false;
            spawned = true;

            transform.position = position;
            transform.localScale = Vector3.one * data.scale;
            rb.position = position;
            rb.linearVelocity = Vector2.zero;
            rb.mass = data.isBoss ? 50f : 1f + data.knockbackResistance * 4f;
            rb.simulated = true;
            col.enabled = true;

            body.sprite = ShapeSprites.ForEnemy(data.shape);
            body.color = data.color;
            glow.color = new Color(data.color.r, data.color.g, data.color.b, data.isBoss ? 0.35f : 0.2f);
            bodyTransform.localScale = Vector3.one;
            bodyTransform.rotation = Quaternion.identity;
            shieldRing.enabled = false;
            telegraph.enabled = false;
            hpBack.enabled = hpFill.enabled = false;

            if (!brains.TryGetValue(data.behaviour, out brain))
            {
                brain = EnemyBrain.Create(data.behaviour);
                brains[data.behaviour] = brain;
            }
            brain.Bind(this);
            brain.Reset();
            Active.Add(this);
        }

        void FixedUpdate()
        {
            if (!spawned || frozen) return;
            float dt = Time.fixedDeltaTime;
            Vector2 velocity = brain.Tick(dt);
            if (!spawned) return; // brain may self-destruct (bomber)

            float slow = slowTimer > 0f ? 1f - slowAmount * (IsBoss ? 0.5f : 1f) : 1f;
            rb.linearVelocity = velocity * slow + knock;
            knock = Vector2.MoveTowards(knock, Vector2.zero, 25f * dt);

            Vector2 face = brain.Facing != Vector2.zero ? brain.Facing : velocity;
            if (brain.Spins) bodyTransform.Rotate(0f, 0f, 90f * dt);
            else if (face.sqrMagnitude > 0.0001f)
                bodyTransform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(face.y, face.x) * Mathf.Rad2Deg);
        }

        void Update()
        {
            if (!spawned || frozen) return;
            float dt = Time.deltaTime;
            flashTimer -= dt;
            slowTimer -= dt;
            shieldTimer -= dt;
            shieldRing.enabled = shieldTimer > 0f;
            if (ShowsHPBar && hp < MaxHP)
            {
                float r = Mathf.Clamp01(hp / MaxHP);
                hpBack.enabled = hpFill.enabled = true;
                hpFill.transform.localScale = new Vector3(0.9f * r, 0.07f, 1f);
                hpFill.transform.localPosition = new Vector3(-0.45f * (1f - r), 0.72f, 0f);
            }

            if (burnTimer > 0f)
            {
                burnTimer -= dt;
                burnTick -= dt;
                if (burnTick <= 0f)
                {
                    burnTick += 0.5f;
                    TakeDamage(new DamageInfo { Amount = burnDps * 0.5f, IsDot = true });
                    if (!spawned) return;
                }
            }

            var c = brain.TintColor(Data.color);
            if (slowTimer > 0f) c = Color.Lerp(c, WeaponTags.ColorOf(WeaponTag.Cryo), 0.6f);
            if (burnTimer > 0f) c = Color.Lerp(c, WeaponTags.ColorOf(WeaponTag.Energy), 0.5f);
            body.color = flashTimer > 0f ? Color.white : c;
        }

        public void TakeDamage(in DamageInfo info)
        {
            if (!IsAlive) return;
            float amount = info.Amount * (Shielded ? ShieldDamageMultiplier : 1f);
            hp -= amount;
            if (info.IsDot)
            {
                DamagePopups.Show(Position, amount, WeaponTags.ColorOf(WeaponTag.Energy), false);
                if (hp <= 0f) Die();
                return;
            }

            flashTimer = FlashTime;
            knock += info.Direction * (info.Knockback * (1f - Data.knockbackResistance));
            if (info.SlowAmount > 0f)
            {
                slowAmount = slowTimer > 0f ? Mathf.Max(slowAmount, info.SlowAmount) : info.SlowAmount;
                slowTimer = Mathf.Max(slowTimer, info.SlowDuration);
            }
            if (info.BurnDps > 0f)
            {
                if (burnTimer <= 0f) burnTick = 0.5f;
                burnDps = Mathf.Max(burnTimer > 0f ? burnDps : 0f, info.BurnDps);
                burnTimer = Mathf.Max(burnTimer, info.BurnDuration);
            }

            var popupColor = Shielded ? new Color(0.6f, 0.8f, 1f) : info.Crit ? new Color(1f, 0.85f, 0.2f) : Color.white;
            DamagePopups.Show(Position, amount, popupColor, info.Crit);
            if (info.Crit) FxSystem.Burst(Position, new Color(1f, 0.9f, 0.4f), 4, 6f, 0.08f, 0.25f);
            AudioManager.Play(SfxId.Hit, 0.22f, 0.15f);

            if (hp <= 0f) Die();
        }

        int RollScrap()
        {
            if (IsBoss) return Data.scrapDrop;
            float chance = GameManager.Instance.CurrentWave.scrapDropChance;
            int n = 0;
            for (int i = 0; i < Data.scrapDrop; i++) if (UnityEngine.Random.value < chance) n++;
            return n;
        }

        public void ApplyShield(float duration) => shieldTimer = Mathf.Max(shieldTimer, duration);

        void Die()
        {
            FxSystem.Pulse(Position, Data.color, 0.2f, 1.6f * Data.scale, 0.2f);
            AudioManager.Play(SfxId.EnemyDie, 0.3f, 0.12f);
            FxSystem.Burst(Position, Data.color, IsBoss ? 40 : Data.scale >= 1.5f ? 14 : 6, IsBoss ? 14f : 7f, 0.1f * Mathf.Max(1f, Data.scale * 0.8f));
            ScrapSystem.Drop(Position, RollScrap());
            GameManager.Instance.RegisterKill(this);
            var b = brain;
            var at = Position;
            Despawn();
            b.OnDeath(at);
        }

        /// <summary>Removes the enemy without reward (e.g. a bomber that detonated itself).</summary>
        public void SelfDestruct()
        {
            FxSystem.Pulse(Position, Data.color, 0.2f, 1.6f * Data.scale, 0.2f);
            Despawn();
        }

        /// <summary>Wave end: stop in place, then dissolve (기획서 Phase 1: 몬스터 정지 및 소멸).</summary>
        public void FreezeAndDissolve(float delay)
        {
            if (!spawned) return;
            frozen = true;
            col.enabled = false;
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
            telegraph.enabled = false;
            shieldRing.enabled = false;
            hpBack.enabled = hpFill.enabled = false;
            StartCoroutine(DissolveRoutine(delay));
        }

        IEnumerator DissolveRoutine(float delay)
        {
            body.color = Color.white;
            yield return new WaitForSeconds(delay);
            const float duration = 0.35f;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = 1f - t / duration;
                bodyTransform.localScale = new Vector3(1f + (1f - k) * 0.6f, k, 1f);
                var c = Data.color;
                c.a = k;
                body.color = c;
                yield return null;
            }
            FxSystem.Pulse(Position, Data.color, 0.1f, 1f, 0.15f);
            Despawn();
        }

        void Despawn()
        {
            if (!spawned) return;
            spawned = false;
            telegraph.enabled = false;
            shieldRing.enabled = false;
            hpBack.enabled = hpFill.enabled = false;
            Active.Remove(this);
            StopAllCoroutines();
            release?.Invoke(this);
        }

        /// <summary>Draws a warning line (charge path / aim) in world space.</summary>
        public void ShowTelegraph(Vector2 from, Vector2 dir, float length, float width, Color color)
        {
            telegraph.enabled = true;
            telegraph.color = color;
            telegraph.transform.SetPositionAndRotation(from + dir * (length * 0.5f),
                Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg));
            telegraph.transform.localScale = new Vector3(length, width, 1f);
        }

        public void HideTelegraph() => telegraph.enabled = false;

        /// <summary>
        /// Area blast used by 마그마 버스트: hurts the player and every other enemy in range,
        /// so knocking bombers into crowds sets off chain reactions.
        /// </summary>
        public static void Blast(Vector2 center, float radius, float playerDamage, float enemyDamage, Color color, Enemy source)
        {
            FxSystem.Pulse(center, color, 0.3f, radius * 2.4f, 0.35f);
            FxSystem.Pulse(center, Color.white, 0.2f, radius * 1.4f, 0.2f);
            CameraShake.Add(0.15f, minor: true);
            AudioManager.Play(SfxId.Explosion, 0.6f);

            var player = GameManager.Instance.Player;
            Vector2 toPlayer = player.Position - center;
            if (toPlayer.magnitude < radius + 0.35f)
                player.TakeDamage(playerDamage, toPlayer.normalized * 10f);

            foreach (var e in Active.ToArray())
            {
                if (e == source || !e.IsAlive) continue;
                Vector2 to = e.Position - center;
                if (to.sqrMagnitude > radius * radius) continue;
                e.TakeDamage(new DamageInfo
                {
                    Amount = enemyDamage, Knockback = 8f,
                    Direction = to.sqrMagnitude > 0.001f ? to.normalized : Vector2.up,
                });
            }
        }
    }
}
