using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CoreOverclock
{
    /// <summary>
    /// Runtime URP post-processing (기획서 Phase 4: 블룸 효과): neon bloom always on, plus
    /// heat-driven feedback — orange vignette in overclock, red pulsing vignette + chromatic
    /// aberration in meltdown, and a red flash when the player is hit.
    /// </summary>
    public class PostFx : MonoBehaviour
    {
        static PostFx instance;

        Bloom bloom;
        Vignette vignette;
        ChromaticAberration chroma;
        float hitFlash;

        public static void Create(Camera cam)
        {
            if (!cam) return;
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            cam.allowHDR = true;

            var go = new GameObject("PostFx");
            var fx = go.AddComponent<PostFx>();
            instance = fx;

            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10f;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            volume.profile = profile;

            fx.bloom = profile.Add<Bloom>(true);
            fx.bloom.threshold.Override(0.72f);
            fx.bloom.intensity.Override(1.1f);
            fx.bloom.scatter.Override(0.65f);

            fx.vignette = profile.Add<Vignette>(true);
            fx.vignette.intensity.Override(0.22f);
            fx.vignette.smoothness.Override(0.5f);
            fx.vignette.color.Override(Color.black);

            fx.chroma = profile.Add<ChromaticAberration>(true);
            fx.chroma.intensity.Override(0f);
        }

        public static void PlayerHit() { if (instance) instance.hitFlash = 1f; }

        void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        void Update()
        {
            var gm = GameManager.Instance;
            var state = gm && gm.State == GameState.Combat ? gm.Heat.State : HeatState.Safe;
            hitFlash = Mathf.MoveTowards(hitFlash, 0f, Time.unscaledDeltaTime * 3f);

            float vig = 0.22f;
            Color vigColor = Color.black;
            float ca = 0f;
            float bloomIntensity = 1.1f;

            if (state == HeatState.Overclock)
            {
                vig = 0.32f;
                vigColor = new Color(0.45f, 0.18f, 0f);
                bloomIntensity = 1.6f;
            }
            else if (state == HeatState.Meltdown)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 10f);
                vig = 0.38f + 0.1f * pulse;
                vigColor = new Color(0.6f, 0f, 0.05f);
                ca = 0.5f + 0.3f * pulse;
                bloomIntensity = 1.4f;
            }

            // Low HP: slow red heartbeat at the screen edges.
            if (gm && gm.State == GameState.Combat && gm.Player.IsAlive && gm.Player.HP < gm.Player.MaxHP * 0.3f)
            {
                float beat = Mathf.Pow(Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3f)), 4f);
                vig = Mathf.Max(vig, 0.3f + 0.12f * beat);
                vigColor = Color.Lerp(vigColor, new Color(0.55f, 0f, 0f), 0.8f);
            }

            if (hitFlash > 0f)
            {
                vig = Mathf.Max(vig, 0.22f + 0.28f * hitFlash);
                vigColor = Color.Lerp(vigColor, new Color(0.7f, 0f, 0f), hitFlash);
                ca = Mathf.Max(ca, 0.4f * hitFlash);
            }

            vignette.intensity.value = Mathf.Lerp(vignette.intensity.value, vig, Time.unscaledDeltaTime * 10f);
            vignette.color.value = Color.Lerp(vignette.color.value, vigColor, Time.unscaledDeltaTime * 10f);
            chroma.intensity.value = Mathf.Lerp(chroma.intensity.value, ca, Time.unscaledDeltaTime * 12f);
            bloom.intensity.value = Mathf.Lerp(bloom.intensity.value, bloomIntensity, Time.unscaledDeltaTime * 4f);
        }
    }
}
