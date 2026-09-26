using System;
using UnityEngine;

namespace CoreOverclock
{
    public enum HeatState { Safe, Overclock, Meltdown }

    /// <summary>
    /// 과열 &amp; 오버클럭 (기획서 2장).
    ///  0~70%: 안전 / 71~99%: 공격력 +50%, 탄속 x2 / 100%: 5초 무기 락다운 + 지속 피해.
    /// Cooling = base + chips + 10% of current heat, so each loadout settles at an equilibrium:
    /// light builds stay cool, heavy builds sit in overclock or melt down unless they vent or cool.
    /// </summary>
    public class HeatSystem : MonoBehaviour
    {
        public const float Max = 100f;
        public const float OverclockThreshold = 70f;
        const float OverclockExitThreshold = 65f; // hysteresis so the state doesn't flicker at the line
        const float BaseCooling = 4f;
        const float ProportionalCooling = 0.1f;

        public const float MeltdownDuration = 5f;
        const float MeltdownTick = 0.6f;
        const float MeltdownTickDamage = 1f;
        const float MeltdownRecoverTo = 40f;

        public const float VentCooldown = 15f;
        const float VentAmount = 50f;
        const float VentRadius = 4.5f;
        const float VentKnockback = 16f;
        const float VentDamage = 5f;
        const float VentInputGrace = 0.3f;

        float dotTimer, inputGrace;

        public float Value { get; private set; }
        public HeatState State { get; private set; }
        public float MeltdownTimeLeft { get; private set; }
        public float VentCooldownLeft { get; private set; }

        public float Ratio => Value / Max;
        public bool WeaponsEnabled => State != HeatState.Meltdown;
        public bool VentReady => VentCooldownLeft <= 0f && State != HeatState.Meltdown;
        public float DamageMultiplier => State == HeatState.Overclock ? 1.5f + Stats.OverclockDamagePct : 1f;
        public float CurrentVentCooldown => VentCooldown * Mathf.Max(0.3f, 1f + Stats.VentCooldownPct);
        float CurrentMeltdownDuration => MeltdownDuration * Mathf.Max(0.3f, 1f + Stats.MeltdownDurationPct);
        static PlayerStats Stats => GameManager.Instance.Loadout.Stats;
        public float ProjectileSpeedMultiplier => State == HeatState.Overclock ? 2f : 1f;

        public event Action<HeatState> StateChanged;

        public void ResetForWave()
        {
            Value = 0f;
            MeltdownTimeLeft = 0f;
            VentCooldownLeft = 0f;
            inputGrace = VentInputGrace;
            SetState(HeatState.Safe);
        }

        public void AddShotHeat(float amount)
        {
            if (State == HeatState.Meltdown) return;
            var loadout = GameManager.Instance.Loadout;
            Value += amount * loadout.HeatGenMultiplier;
            if (Value >= Max)
            {
                if (loadout.Synergy.MeltdownImmune) Value = Max - 1f;
                else { EnterMeltdown(); return; }
            }
            RefreshState();
        }

        void EnterMeltdown()
        {
            Value = Max;
            MeltdownTimeLeft = CurrentMeltdownDuration;
            dotTimer = MeltdownTick;
            CameraShake.Add(0.5f);
            SetState(HeatState.Meltdown);
        }

        void Update()
        {
            var gm = GameManager.Instance;
            if (!gm || gm.State != GameState.Combat) return;
            float dt = Time.deltaTime;
            inputGrace -= Time.unscaledDeltaTime;
            VentCooldownLeft = Mathf.Max(0f, VentCooldownLeft - dt);

            if (State == HeatState.Meltdown)
            {
                MeltdownTimeLeft -= dt;
                Value = Mathf.Lerp(MeltdownRecoverTo, Max, Mathf.Clamp01(MeltdownTimeLeft / CurrentMeltdownDuration));
                dotTimer -= dt;
                if (dotTimer <= 0f)
                {
                    dotTimer += MeltdownTick;
                    gm.Player.TakeTrueDamage(MeltdownTickDamage);
                }
                if (MeltdownTimeLeft <= 0f)
                {
                    MeltdownTimeLeft = 0f;
                    SetState(HeatState.Safe);
                }
                return;
            }

            float cooling = BaseCooling + gm.Loadout.Stats.CoolingFlat + ProportionalCooling * Value;
            Value = Mathf.Max(0f, Value - cooling * dt);
            RefreshState();

            bool wantVent = GameInput.VentPressed && inputGrace <= 0f;
            if (DevCommandLine.Autopilot && !DevCommandLine.NoVent && Value > 85f) wantVent = true;
            if (wantVent) TryVent();
        }

        /// <summary>긴급 방열 (Vent Out): heat -50%, knockback shockwave. 15s cooldown.</summary>
        public bool TryVent()
        {
            var gm = GameManager.Instance;
            if (!VentReady || gm.State != GameState.Combat) return false;

            Value = Mathf.Max(0f, Value - VentAmount);
            VentCooldownLeft = CurrentVentCooldown;
            Vector2 origin = gm.Player.Position;
            float damage = VentDamage * (1f + gm.Loadout.Stats.DamagePct);

            foreach (var e in Enemy.Active.ToArray())
            {
                if (!e.IsAlive) continue;
                Vector2 to = e.Position - origin;
                if (to.sqrMagnitude > VentRadius * VentRadius) continue;
                e.TakeDamage(new DamageInfo
                {
                    Amount = damage,
                    Direction = to.sqrMagnitude > 0.001f ? to.normalized : UnityEngine.Random.insideUnitCircle.normalized,
                    Knockback = VentKnockback,
                });
            }

            EnemyProjectileSystem.ClearInRadius(origin, VentRadius);
            var cool = new Color(0.5f, 0.9f, 1f);
            FxSystem.Pulse(origin, cool, 0.5f, VentRadius * 2.4f, 0.4f);
            FxSystem.Pulse(origin, Color.white, 0.3f, VentRadius * 1.6f, 0.25f);
            CameraShake.Add(0.4f);
            RefreshState();
            return true;
        }

        void RefreshState()
        {
            float threshold = State == HeatState.Overclock ? OverclockExitThreshold : OverclockThreshold;
            SetState(Value > threshold ? HeatState.Overclock : HeatState.Safe);
        }

        void SetState(HeatState s)
        {
            if (State == s) return;
            State = s;
            StateChanged?.Invoke(s);
        }
    }
}
