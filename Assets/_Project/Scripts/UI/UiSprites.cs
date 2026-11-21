using UnityEngine;

namespace NightShift
{
    /// <summary>Tiny procedurally drawn sprites for the HUD (rings, arrows, bursts). Point-filtered for a chunky retro look.</summary>
    public static class UiSprites
    {
        static Sprite ring, thinRing, disc, triangle, burst, corner, softEdge;

        public static Sprite Ring => ring ??= Make("Ring", 64, (x, y, r) => r > 0.78f && r < 0.98f ? 1f : 0f);
        public static Sprite ThinRing => thinRing ??= Make("ThinRing", 64, (x, y, r) => r > 0.88f && r < 0.99f ? 1f : 0f);
        public static Sprite Disc => disc ??= Make("Disc", 32, (x, y, r) => r < 0.98f ? 1f : 0f);

        /// <summary>Solid triangle pointing right (+X).</summary>
        public static Sprite Triangle => triangle ??= Make("Triangle", 32, (x, y, r) =>
        {
            float u = (x + 1f) * 0.5f;          // 0..1 left to right
            return Mathf.Abs(y) < (1f - u) * 0.95f ? 1f : 0f;
        });

        /// <summary>Radial spikes, for the parry burst.</summary>
        public static Sprite Burst => burst ??= Make("Burst", 128, (x, y, r) =>
        {
            float a = Mathf.Atan2(y, x);
            float spikes = Mathf.Abs(Mathf.Sin(a * 8f)) * 0.55f + Mathf.Abs(Mathf.Sin(a * 13f + 1f)) * 0.35f;
            return r > 0.25f && r < 0.25f + spikes * 0.75f ? 1f - r * 0.6f : 0f;
        });

        /// <summary>L-shaped corner bracket (top-left).</summary>
        public static Sprite Corner => corner ??= Make("Corner", 32, (x, y, r) =>
        {
            float u = (x + 1f) * 0.5f, v = (y + 1f) * 0.5f;
            bool top = v > 0.8f && u < 0.75f;
            bool left = u < 0.2f && v > 0.25f;
            return top || left ? 1f : 0f;
        });

        /// <summary>Horizontal gradient: opaque on the left edge, clear on the right.</summary>
        public static Sprite SoftEdge => softEdge ??= Make("SoftEdge", 64, (x, y, r) => Mathf.Pow(1f - (x + 1f) * 0.5f, 2f), bilinear: true);

        static Sprite Make(string name, int size, System.Func<float, float, float, float> alpha, bool bilinear = false)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = bilinear ? FilterMode.Bilinear : FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            var px = new Color32[size * size];
            for (int j = 0; j < size; j++)
            for (int i = 0; i < size; i++)
            {
                float x = (i + 0.5f) / size * 2f - 1f;
                float y = (j + 0.5f) / size * 2f - 1f;
                float r = Mathf.Sqrt(x * x + y * y);
                byte a = (byte)(Mathf.Clamp01(alpha(x, y, r)) * 255f);
                px[j * size + i] = new Color32(255, 255, 255, a);
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
