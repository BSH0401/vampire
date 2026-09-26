using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CoreOverclock
{
    public class Enemy : MonoBehaviour, IDamageable
    {
        public static readonly List<Enemy> Active = new();

        const float FlashTime = 0.05f; // 기획서 7.3 Sprite Flash

        Rigidbody2D rb;
        CircleCollider2D col;
        SpriteRenderer body, glow;
        Transform bodyTransform;
        Action<Enemy> release;
        Vector2 knock;
        float hp, speedMultiplier, flashTimer;
        float slowAmount, slowTimer, burnDps, burnTimer, burnTick;
        bool frozen, spawned;

        public EnemyData Data { get; private set; }
        public bool IsAlive => spawned && !frozen && hp > 0f;
        public Vector2 Position => rb.position;
        public float ContactDamage => Data.contactDamage;

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
            return e;
        }

        public void Spawn(EnemyData data, Vector2 position, float hpMultiplier, float speedMul, Action<Enemy> onRelease)
        {
            Data = data;
            release = onRelease;
            hp = data.maxHP * hpMultiplier;
            speedMultiplier = speedMul;
            knock = Vector2.zero;
            flashTimer = 0f;
            slowAmount = slowTimer = burnDps = burnTimer = burnTick = 0f;
            frozen = false;
            spawned = true;

            transform.position = position;
            transform.localScale = Vector3.one * data.scale;
            rb.position = position;
            rb.linearVelocity = Vector2.zero;
            rb.simulated = true;
            col.enabled = true;

            body.sprite = ShapeSprites.ForEnemy(data.shape);
            body.color = data.color;
            glow.color = new Color(data.color.r, data.color.g, data.color.b, 0.2f);
            bodyTransform.localScale = Vector3.one;
            Active.Add(this);
        }

        void FixedUpdate()
        {
            if (!spawned || frozen) return;
            var player = GameManager.Instance.Player;
            Vector2 to = player.Position - rb.position;
            Vector2 dir = to.sqrMagnitude > 0.0001f ? to.normalized : Vector2.zero;

            // Phase 1: every behaviour falls back to the straight chaser AI (스크랩 비트).
            float slow = slowTimer > 0f ? 1f - slowAmount : 1f;
            rb.linearVelocity = dir * (Data.moveSpeed * speedMultiplier * slow) + knock;
            knock = Vector2.MoveTowards(knock, Vector2.zero, 25f * Time.fixedDeltaTime);
            if (dir != Vector2.zero)
                bodyTransform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
        }

        void Update()
        {
            if (!spawned || frozen) return;
            float dt = Time.deltaTime;
            flashTimer -= dt;
            slowTimer -= dt;

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

            var c = Data.color;
            if (slowTimer > 0f) c = Color.Lerp(c, WeaponTags.ColorOf(WeaponTag.Cryo), 0.6f);
            if (burnTimer > 0f) c = Color.Lerp(c, WeaponTags.ColorOf(WeaponTag.Energy), 0.5f);
            body.color = flashTimer > 0f ? Color.white : c;
        }

        public void TakeDamage(in DamageInfo info)
        {
            if (!IsAlive) return;
            hp -= info.Amount;
            if (info.IsDot)
            {
                DamagePopups.Show(Position, info.Amount, WeaponTags.ColorOf(WeaponTag.Energy), false);
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

            DamagePopups.Show(Position, info.Amount, info.Crit ? new Color(1f, 0.85f, 0.2f) : Color.white, info.Crit);
            if (info.Crit)
            {
                CameraShake.Add(0.15f);
                TimeControl.HitStop();
            }

            if (hp <= 0f) Die();
        }

        void Die()
        {
            FxSystem.Pulse(Position, Data.color, 0.2f, 1.6f * Data.scale, 0.2f);
            ScrapSystem.Drop(Position, Data.scrapDrop);
            GameManager.Instance.RegisterKill();
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
            Active.Remove(this);
            StopAllCoroutines();
            release?.Invoke(this);
        }
    }
}
