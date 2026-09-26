using System;
using UnityEngine;

namespace CoreOverclock
{
    /// <summary>
    /// Procedural placeholder audio (Phase 4): every effect and music loop is synthesized at startup,
    /// so the project ships with zero audio assets or licensing concerns. Swap in real clips later
    /// by assigning them in <see cref="AudioManager"/>.
    /// </summary>
    public static class SoundSynth
    {
        public const int Rate = 44100;
        enum Wave { Sine, Square, Saw, Triangle, Noise }

        // Per-thread RNG: music renders on a worker thread while SFX render on the main thread.
        [ThreadStatic] static System.Random rngPerThread;
        static System.Random rng => rngPerThread ??= new System.Random(1234);

        // ───────────────────────────── Effects ─────────────────────────────

        public static AudioClip Make(SfxId id) => id switch
        {
            SfxId.ShootBallistic => Clip(id, 0.08f, (b, n) =>
            {
                Tone(b, Wave.Square, 900f, 380f, 0f, 0.06f, 0.35f);
                Tone(b, Wave.Noise, 0f, 0f, 0f, 0.025f, 0.25f);
            }),
            SfxId.ShootEnergy => Clip(id, 0.1f, (b, n) => Tone(b, Wave.Sine, 1500f, 800f, 0f, 0.09f, 0.4f, vibrato: 40f)),
            SfxId.ShootCryo => Clip(id, 0.1f, (b, n) =>
            {
                Tone(b, Wave.Triangle, 2100f, 1500f, 0f, 0.08f, 0.35f);
                Tone(b, Wave.Sine, 3200f, 3000f, 0.01f, 0.05f, 0.12f);
            }),
            SfxId.ShootExplosive => Clip(id, 0.14f, (b, n) =>
            {
                Tone(b, Wave.Saw, 260f, 90f, 0f, 0.12f, 0.35f);
                Tone(b, Wave.Noise, 0f, 0f, 0f, 0.05f, 0.2f);
            }),
            SfxId.Hit => Clip(id, 0.05f, (b, n) => Tone(b, Wave.Noise, 0f, 0f, 0f, 0.04f, 0.4f), lowpass: 0.35f),
            SfxId.EnemyDie => Clip(id, 0.16f, (b, n) =>
            {
                Tone(b, Wave.Square, 520f, 70f, 0f, 0.14f, 0.3f);
                Tone(b, Wave.Noise, 0f, 0f, 0f, 0.08f, 0.2f);
            }, lowpass: 0.5f),
            SfxId.Explosion => Clip(id, 0.6f, (b, n) =>
            {
                Tone(b, Wave.Noise, 0f, 0f, 0f, 0.55f, 0.8f, decayPower: 2.5f);
                Tone(b, Wave.Sine, 110f, 35f, 0f, 0.45f, 0.8f);
            }, lowpass: 0.18f),
            SfxId.PlayerHurt => Clip(id, 0.25f, (b, n) =>
            {
                Tone(b, Wave.Saw, 320f, 90f, 0f, 0.22f, 0.5f);
                Tone(b, Wave.Square, 160f, 60f, 0f, 0.2f, 0.25f);
            }, lowpass: 0.4f),
            SfxId.Zap => Clip(id, 0.18f, (b, n) =>
            {
                for (int i = 0; i < n; i++)
                    b[i] += (float)(rng.NextDouble() * 2 - 1) * (Mathf.Sin(i * 0.05f) > 0.3f ? 0.5f : 0.1f) * Env(i, n, 0.005f, 1.5f);
            }),
            SfxId.Pickup => Clip(id, 0.07f, (b, n) => Tone(b, Wave.Sine, 1250f, 1650f, 0f, 0.06f, 0.35f)),
            SfxId.Vent => Clip(id, 0.9f, (b, n) =>
            {
                Tone(b, Wave.Noise, 0f, 0f, 0f, 0.85f, 0.55f, decayPower: 1.2f);
                Tone(b, Wave.Sine, 240f, 55f, 0f, 0.5f, 0.6f);
            }, lowpass: 0.25f),
            SfxId.Overclock => Clip(id, 0.42f, (b, n) =>
            {
                Tone(b, Wave.Square, 880f, 880f, 0f, 0.1f, 0.25f);
                Tone(b, Wave.Square, 1175f, 1175f, 0.12f, 0.1f, 0.25f);
                Tone(b, Wave.Square, 1568f, 1568f, 0.24f, 0.16f, 0.25f);
            }),
            SfxId.Meltdown => Clip(id, 1.1f, (b, n) =>
            {
                for (int k = 0; k < 4; k++)
                    Tone(b, Wave.Saw, k % 2 == 0 ? 440f : 330f, k % 2 == 0 ? 440f : 330f, k * 0.25f, 0.22f, 0.3f);
                Tone(b, Wave.Noise, 0f, 0f, 0f, 1f, 0.15f);
            }, lowpass: 0.6f),
            SfxId.BossSpawn => Clip(id, 1.8f, (b, n) =>
            {
                Tone(b, Wave.Saw, 55f, 45f, 0f, 1.7f, 0.6f, attack: 0.4f);
                Tone(b, Wave.Saw, 82.5f, 70f, 0.1f, 1.6f, 0.4f, attack: 0.5f);
                Tone(b, Wave.Noise, 0f, 0f, 0f, 1.7f, 0.2f, attack: 0.8f);
            }, lowpass: 0.12f),
            SfxId.EnemyShoot => Clip(id, 0.09f, (b, n) => Tone(b, Wave.Sine, 620f, 300f, 0f, 0.08f, 0.3f)),
            SfxId.ChargeWindup => Clip(id, 0.5f, (b, n) => Tone(b, Wave.Saw, 180f, 720f, 0f, 0.48f, 0.25f, attack: 0.3f), lowpass: 0.3f),
            SfxId.WaveClear => Clip(id, 0.8f, (b, n) => Arp(b, new[] { 523.25f, 659.25f, 783.99f, 1046.5f }, 0.11f, 0.3f, Wave.Square)),
            SfxId.Victory => Clip(id, 1.6f, (b, n) =>
            {
                Arp(b, new[] { 523.25f, 659.25f, 783.99f, 1046.5f, 783.99f, 1046.5f, 1318.5f }, 0.13f, 0.3f, Wave.Square);
                Tone(b, Wave.Triangle, 261.6f, 261.6f, 0.9f, 0.7f, 0.3f);
            }),
            SfxId.GameOver => Clip(id, 1.4f, (b, n) =>
                Arp(b, new[] { 392f, 349.2f, 311.1f, 261.6f }, 0.28f, 0.35f, Wave.Saw), lowpass: 0.35f),
            SfxId.UIClick => Clip(id, 0.04f, (b, n) => Tone(b, Wave.Sine, 1100f, 900f, 0f, 0.035f, 0.35f)),
            SfxId.Buy => Clip(id, 0.22f, (b, n) =>
            {
                Tone(b, Wave.Square, 1320f, 1320f, 0f, 0.07f, 0.22f);
                Tone(b, Wave.Square, 1760f, 1760f, 0.07f, 0.13f, 0.22f);
            }),
            SfxId.Deny => Clip(id, 0.18f, (b, n) => Tone(b, Wave.Square, 150f, 130f, 0f, 0.16f, 0.3f), lowpass: 0.3f),
            _ => Clip(id, 0.05f, (b, n) => { }),
        };

        // ───────────────────────────── Music ─────────────────────────────

        /// <summary>
        /// Renders an 8-bar synthwave loop (tempo, key and density differ per track).
        /// Pure math, safe to call off the main thread; wrap the result with <see cref="ToClip"/>.
        /// </summary>
        public static float[] RenderMusic(MusicId id)
        {
            var spec = id switch
            {
                MusicId.Combat => new MusicSpec(118f, new[] { 57, 53, 48, 55 }, drums: 2, arp: true, leadVol: 0.07f),
                MusicId.Boss => new MusicSpec(132f, new[] { 50, 46, 53, 52 }, drums: 3, arp: true, leadVol: 0.09f),
                MusicId.Shop => new MusicSpec(92f, new[] { 48, 57, 53, 55 }, drums: 1, arp: true, leadVol: 0.05f),
                _ => new MusicSpec(84f, new[] { 57, 53, 60, 55 }, drums: 0, arp: false, leadVol: 0.05f),
            };

            float beat = 60f / spec.Bpm;
            int bars = 8;
            int n = Mathf.RoundToInt(bars * 4 * beat * Rate);
            var b = new float[n];
            float step = beat / 4f; // 16th notes

            for (int bar = 0; bar < bars; bar++)
            {
                int root = spec.Roots[bar % spec.Roots.Length];
                float barStart = bar * 4 * beat;

                // Pad chord (root, 3rd, 5th) — minor on the first chord of the pair, major otherwise.
                bool minor = bar % 2 == 0;
                foreach (int semis in new[] { 0, minor ? 3 : 4, 7, 12 })
                    Tone(b, Wave.Saw, Midi(root + semis), Midi(root + semis), barStart, 4 * beat, 0.035f, attack: 0.3f, release: true);

                for (int s = 0; s < 16; s++)
                {
                    float t = barStart + s * step;
                    // Bass: driving 8ths
                    if (s % 2 == 0) Tone(b, Wave.Saw, Midi(root - 12), Midi(root - 12), t, step * 1.8f, 0.16f);
                    // Arp
                    if (spec.Arp)
                    {
                        int[] pattern = { 0, 7, 12, minor ? 15 : 16, 12, 7, 0, 7 };
                        float f = Midi(root + 12 + pattern[s % pattern.Length]);
                        Tone(b, Wave.Square, f, f, t, step * 0.9f, spec.LeadVol);
                    }
                    // Drums
                    if (spec.Drums >= 1 && s % 4 == 0) Kick(b, t);
                    if (spec.Drums >= 2 && (s == 4 || s == 12)) Tone(b, Wave.Noise, 0f, 0f, t, 0.12f, 0.18f);
                    if (spec.Drums >= 2 && s % 2 == 1) Tone(b, Wave.Noise, 0f, 0f, t, 0.025f, 0.05f);
                    if (spec.Drums >= 3 && s % 4 == 2) Kick(b, t, 0.5f);
                }
            }

            LowPass(b, 0.45f);
            SoftClip(b, 1.2f);
            return b;
        }

        /// <summary>Main thread only.</summary>
        public static AudioClip ToClip(string name, float[] samples)
        {
            var clip = AudioClip.Create(name, samples.Length, 1, Rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        readonly struct MusicSpec
        {
            public readonly float Bpm, LeadVol;
            public readonly int[] Roots;
            public readonly int Drums;
            public readonly bool Arp;

            public MusicSpec(float bpm, int[] roots, int drums, bool arp, float leadVol)
            {
                Bpm = bpm; Roots = roots; Drums = drums; Arp = arp; LeadVol = leadVol;
            }
        }

        // ───────────────────────────── Primitives ─────────────────────────────

        static AudioClip Clip(SfxId id, float seconds, Action<float[], int> build, float lowpass = 1f)
        {
            int n = Mathf.CeilToInt(seconds * Rate);
            var b = new float[n];
            build(b, n);
            if (lowpass < 1f) LowPass(b, lowpass);
            SoftClip(b, 1f);
            var clip = AudioClip.Create($"sfx_{id}", n, 1, Rate, false);
            clip.SetData(b, 0);
            return clip;
        }

        static void Tone(float[] b, Wave wave, float f0, float f1, float start, float dur, float vol,
            float attack = 0.004f, float decayPower = 1.5f, float vibrato = 0f, bool release = false)
        {
            int s0 = Mathf.RoundToInt(start * Rate);
            int len = Mathf.RoundToInt(dur * Rate);
            double phase = 0;
            for (int i = 0; i < len; i++)
            {
                int idx = s0 + i;
                if (idx < 0) continue;
                if (idx >= b.Length) break;
                float k = (float)i / len;
                float f = Mathf.Lerp(f0, f1, k) + (vibrato > 0f ? Mathf.Sin(i * 0.003f) * vibrato : 0f);
                phase += f / Rate;
                float p = (float)(phase - Math.Floor(phase));
                float v = wave switch
                {
                    Wave.Sine => Mathf.Sin(p * 2f * Mathf.PI),
                    Wave.Square => p < 0.5f ? 0.8f : -0.8f,
                    Wave.Saw => p * 2f - 1f,
                    Wave.Triangle => 1f - 4f * Mathf.Abs(p - 0.5f),
                    _ => (float)(rng.NextDouble() * 2 - 1),
                };
                float env = release ? Mathf.Min(1f, i / (attack * Rate)) * Mathf.Min(1f, (len - i) / (0.2f * Rate)) : Env(i, len, attack, decayPower);
                b[idx] += v * env * vol;
            }
        }

        static float Env(int i, int len, float attack, float decayPower)
        {
            float a = attack > 0f ? Mathf.Min(1f, i / (attack * Rate)) : 1f;
            return a * Mathf.Pow(1f - (float)i / len, decayPower);
        }

        static void Kick(float[] b, float t, float vol = 0.7f) => Tone(b, Wave.Sine, 130f, 38f, t, 0.18f, vol, decayPower: 2f);

        static void Arp(float[] b, float[] notes, float stepSec, float vol, Wave wave)
        {
            for (int i = 0; i < notes.Length; i++)
                Tone(b, wave, notes[i], notes[i], i * stepSec, stepSec * 1.6f, vol);
        }

        static float Midi(int note) => 440f * Mathf.Pow(2f, (note - 69) / 12f);

        /// <summary>One-pole low-pass; alpha in (0,1], smaller = darker.</summary>
        static void LowPass(float[] b, float alpha)
        {
            float y = 0f;
            for (int i = 0; i < b.Length; i++) b[i] = y += alpha * (b[i] - y);
        }

        static void SoftClip(float[] b, float drive)
        {
            for (int i = 0; i < b.Length; i++) b[i] = (float)Math.Tanh(b[i] * drive);
        }
    }
}
