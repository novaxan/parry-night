using System;
using UnityEngine;

namespace NightShift
{
    /// <summary>
    /// Generates every sound in the prototype at runtime, so no audio assets are needed.
    /// Clips are built once on first use and cached.
    /// </summary>
    public static class ProceduralSfx
    {
        const int Rate = 22050;
        static readonly System.Random rng = new System.Random(1337);

        static AudioClip shot, pump, shellIn, dryClick, screech, scream, clang, whoosh, footstep, chime, staticNoise, hum, crunch, beep, ding;

        public static AudioClip Shot => shot ??= Make("Shot", 0.9f, (t, i) =>
        {
            float env = Mathf.Exp(-t * 7f);
            float boom = Mathf.Sin(2 * Mathf.PI * (55f + 90f * Mathf.Exp(-t * 20f)) * t) * Mathf.Exp(-t * 9f);
            return (Noise() * 0.8f * env + boom * 0.9f) * (t < 0.004f ? t / 0.004f : 1f);
        });

        public static AudioClip Pump => pump ??= Make("Pump", 0.45f, (t, i) =>
            Click(t, 0.0f, 900f) + Click(t, 0.22f, 700f) + Noise() * 0.15f * Win(t, 0.02f, 0.18f));

        public static AudioClip ShellIn => shellIn ??= Make("ShellIn", 0.2f, (t, i) => Click(t, 0.0f, 1400f) * 0.8f + Click(t, 0.06f, 500f) * 0.5f);

        public static AudioClip DryClick => dryClick ??= Make("DryClick", 0.15f, (t, i) => Click(t, 0f, 2200f) * 0.7f);

        /// <summary>Rising metallic screech: the lunge warning.</summary>
        public static AudioClip Screech => screech ??= Make("Screech", 0.9f, (t, i) =>
        {
            float f = 380f + 1400f * t * t;
            float saw = 2f * ((t * f) % 1f) - 1f;
            float sq = Mathf.Sign(Mathf.Sin(2 * Mathf.PI * f * 1.51f * t));
            return (saw * 0.45f + sq * 0.2f + Noise() * 0.15f) * Mathf.Clamp01(t * 8f) * 0.8f;
        });

        public static AudioClip Scream => scream ??= Make("Scream", 1.6f, (t, i) =>
        {
            float f = 220f + 60f * Mathf.Sin(t * 40f);
            float saw = 2f * ((t * f) % 1f) - 1f;
            float saw2 = 2f * ((t * f * 1.06f) % 1f) - 1f;
            return Mathf.Clamp((saw + saw2) * 0.5f + Noise() * 0.6f, -1f, 1f) * Mathf.Exp(-t * 1.2f);
        });

        public static AudioClip Clang => clang ??= Make("Clang", 1.0f, (t, i) =>
        {
            float s = Mathf.Sin(2 * Mathf.PI * 523f * t) + 0.6f * Mathf.Sin(2 * Mathf.PI * 1307f * t) + 0.4f * Mathf.Sin(2 * Mathf.PI * 2211f * t);
            return (s * 0.4f * Mathf.Exp(-t * 5f) + Noise() * Mathf.Exp(-t * 60f));
        });

        public static AudioClip Whoosh => whoosh ??= Make("Whoosh", 0.3f, (t, i) => Noise() * 0.35f * Win(t, 0f, 0.3f));

        public static AudioClip Footstep => footstep ??= Make("Footstep", 0.5f, (t, i) =>
            (Mathf.Sin(2 * Mathf.PI * 60f * t) * 0.9f + Noise() * 0.3f) * Mathf.Exp(-t * 14f) +
            Click(t, 0.02f, 300f) * 0.6f);

        public static AudioClip Chime => chime ??= Make("Chime", 2.5f, (t, i) =>
            (Mathf.Sin(2 * Mathf.PI * 659f * t) * Mathf.Exp(-t * 2f) + Mathf.Sin(2 * Mathf.PI * 880f * Mathf.Max(0, t - 0.5f)) * (t > 0.5f ? Mathf.Exp(-(t - 0.5f) * 2f) : 0f)) * 0.35f);

        public static AudioClip Static => staticNoise ??= Make("Static", 0.35f, (t, i) => Noise() * 0.25f * Mathf.Exp(-t * 6f));

        public static AudioClip Hum => hum ??= Make("Hum", 2f, (t, i) =>
            (Mathf.Sin(2 * Mathf.PI * 60f * t) * 0.5f + Mathf.Sin(2 * Mathf.PI * 120f * t) * 0.25f + Noise() * 0.04f) * 0.12f);

        public static AudioClip Crunch => crunch ??= Make("Crunch", 1.2f, (t, i) =>
        {
            float crackle = (rng.NextDouble() < 0.02 ? 1f : 0f) * (float)(rng.NextDouble() * 2 - 1);
            return (Noise() * 0.5f * Mathf.Exp(-t * 4f) + crackle * 0.8f + Mathf.Sin(2 * Mathf.PI * 90f * t) * Mathf.Exp(-t * 6f) * 0.6f);
        });

        /// <summary>Bright two-tone ping: the parry window just opened.</summary>
        public static AudioClip Ding => ding ??= Make("Ding", 0.35f, (t, i) =>
            (Mathf.Sin(2 * Mathf.PI * 1568f * t) * 0.5f + Mathf.Sin(2 * Mathf.PI * 2349f * t) * 0.3f) * Mathf.Exp(-t * 9f) * Mathf.Clamp01(t * 400f));

        public static AudioClip Beep => beep ??= Make("Beep", 0.12f, (t, i) => Mathf.Sign(Mathf.Sin(2 * Mathf.PI * 1200f * t)) * 0.15f * Win(t, 0f, 0.12f));

        // ---- helpers -------------------------------------------------------------------------

        static float Noise() => (float)(rng.NextDouble() * 2.0 - 1.0);

        static float Click(float t, float at, float freq)
        {
            float d = t - at;
            if (d < 0f) return 0f;
            return (Mathf.Sin(2 * Mathf.PI * freq * d) * 0.7f + Noise() * 0.5f) * Mathf.Exp(-d * 60f);
        }

        /// <summary>Smooth window between a and b.</summary>
        static float Win(float t, float a, float b)
        {
            if (t < a || t > b) return 0f;
            return Mathf.Sin(Mathf.PI * (t - a) / (b - a));
        }

        static AudioClip Make(string name, float seconds, Func<float, int, float> gen)
        {
            int n = Mathf.CeilToInt(seconds * Rate);
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(gen(i / (float)Rate, i), -1f, 1f);
            // Short fade-out to avoid clicks.
            int fade = Mathf.Min(200, n);
            for (int i = 0; i < fade; i++) data[n - 1 - i] *= i / (float)fade;
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
