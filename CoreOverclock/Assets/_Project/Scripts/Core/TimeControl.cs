using UnityEngine;

namespace CoreOverclock
{
    /// <summary>Owns Time.timeScale: pause and hit-stop (기획서 7.3: 0.05 배속으로 0.05초 정지).</summary>
    public class TimeControl : MonoBehaviour
    {
        const float HitStopScale = 0.05f;
        const float HitStopCooldown = 0.3f;

        static TimeControl instance;
        bool paused;
        float hitStopUntil;
        float lastHitStop = -10f;

        public static bool Paused => instance && instance.paused;
        static float BaseScale => DevCommandLine.TimeScale;

        void Awake()
        {
            instance = this;
            Time.timeScale = BaseScale;
        }

        void OnDestroy()
        {
            if (instance == this) instance = null;
            Time.timeScale = 1f;
        }

        public static void HitStop(float duration = 0.05f)
        {
            if (!instance || instance.paused) return;
            float now = Time.unscaledTime;
            if (now - instance.lastHitStop < HitStopCooldown) return;
            instance.lastHitStop = now;
            instance.hitStopUntil = now + duration;
            Time.timeScale = HitStopScale * BaseScale;
        }

        public static void SetPaused(bool value)
        {
            if (!instance) return;
            instance.paused = value;
            instance.hitStopUntil = 0f;
            Time.timeScale = value ? 0f : BaseScale;
        }

        void Update()
        {
            if (paused || hitStopUntil <= 0f || Time.unscaledTime < hitStopUntil) return;
            hitStopUntil = 0f;
            Time.timeScale = BaseScale;
        }
    }
}
