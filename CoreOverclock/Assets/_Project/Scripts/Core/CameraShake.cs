using UnityEngine;

namespace CoreOverclock
{
    /// <summary>
    /// Fixed single-screen arena camera (기획서 4.2) with trauma-based shake.
    /// Stand-in for Cinemachine Impulse during the graybox phase.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraShake : MonoBehaviour
    {
        const float MaxOffset = 0.3f;
        const float MaxAngle = 0.4f;   // rotation is the main motion-sickness trigger: keep it tiny
        const float Decay = 2.2f;
        /// <summary>Frequent events (explosions, bomber blasts) may only push trauma up to this.</summary>
        const float MinorCap = 0.2f;

        static CameraShake instance;
        Camera cam;
        Vector3 basePosition;
        float trauma;

        /// <param name="minor">Frequent, small events: they never build up past <see cref="MinorCap"/>.</param>
        public static void Add(float amount, bool minor = false)
        {
            if (!instance) return;
            float scale = GameSettings.ShakeStrength;
            if (scale <= 0f) return;
            amount *= scale;
            float cap = minor ? MinorCap * scale : 1f;
            if (instance.trauma >= cap) return;
            instance.trauma = Mathf.Min(cap, instance.trauma + amount);
        }

        void Awake()
        {
            instance = this;
            cam = GetComponent<Camera>();
            basePosition = new Vector3(0f, 0f, -10f);
        }

        void LateUpdate()
        {
            // Keep the whole arena (plus walls) visible for any aspect ratio.
            // Extra top/bottom margin keeps the HUD off the arena.
            float halfW = Arena.HalfSize.x + 1f, halfH = Arena.HalfSize.y + 2f;
            cam.orthographicSize = Mathf.Max(halfH, halfW / Mathf.Max(0.1f, cam.aspect));

            trauma = Mathf.Max(0f, trauma - Decay * Time.unscaledDeltaTime);
            float s = trauma * trauma;
            float t = Time.unscaledTime * 30f;
            var offset = new Vector3(Mathf.PerlinNoise(t, 0.1f) * 2f - 1f, Mathf.PerlinNoise(0.7f, t) * 2f - 1f) * (MaxOffset * s);
            transform.SetPositionAndRotation(basePosition + offset,
                Quaternion.Euler(0f, 0f, (Mathf.PerlinNoise(t, t) * 2f - 1f) * MaxAngle * s));
        }
    }
}
