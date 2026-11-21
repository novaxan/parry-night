using UnityEngine;
using UnityEngine.UI;

namespace NightShift
{
    /// <summary>
    /// Big, punchy parry feedback in the spirit of stylish shooters: freeze frame (from Shotgun's hit-stop),
    /// a white screen flash, a "PARRY" slam, a spinning burst behind the crosshair, an FOV punch and a
    /// spray of sparks off the robot. Everything runs on unscaled time so it plays through the freeze.
    /// </summary>
    public class ParryFX : MonoBehaviour
    {
        [SerializeField] Shotgun shotgun;
        [SerializeField] Camera cam;
        [SerializeField] Material sparkMaterial;
        [SerializeField] float fovPunch = -12f;

        Image whiteFlash, burst, burst2;
        Text slam, slamShadow;
        ParticleSystem sparks;
        float fx = 0f;      // 1 at the parry, decays to 0
        float baseFov;
        bool built;

        public void Build(Transform canvasRoot, Font font)
        {
            whiteFlash = FullScreen(canvasRoot, "ParryFlash");
            burst = Img(canvasRoot, "ParryBurst", UiSprites.Burst, 700f, Color.white);
            burst2 = Img(canvasRoot, "ParryBurst2", UiSprites.Burst, 500f, new Color(0.4f, 0.95f, 1f));
            slamShadow = Label(canvasRoot, "ParryShadow", font, new Color(0f, 0f, 0f, 0.9f), new Vector2(8, 112));
            slam = Label(canvasRoot, "ParrySlam", font, Color.white, new Vector2(0, 120));
            SetVisible(false);
            built = true;
        }

        void Awake()
        {
            if (cam) baseFov = cam.fieldOfView;
            BuildSparks();
        }

        void OnEnable() { if (shotgun) shotgun.Parried += OnParried; }
        void OnDisable() { if (shotgun) shotgun.Parried -= OnParried; }

        void OnParried(Robot robot)
        {
            fx = 1f;
            if (sparks)
            {
                sparks.transform.position = robot.EyePosition + (cam ? (cam.transform.position - robot.EyePosition).normalized * 0.4f : Vector3.zero);
                sparks.Emit(70);
            }
        }

        void LateUpdate()
        {
            if (!built) return;
            float dt = Time.unscaledDeltaTime;
            if (fx <= 0f)
            {
                SetVisible(false);
                if (cam) cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, baseFov, 1f - Mathf.Exp(-12f * dt));
                return;
            }

            SetVisible(true);
            float age = 1f - fx;                    // 0 → 1 over ~0.6 s
            fx = Mathf.Max(0f, fx - dt / 0.6f);

            // Hard white frame, then a fast fade with a couple of strobe pops.
            float flashA = age < 0.06f ? 0.95f : Mathf.Clamp01(0.7f - age * 1.6f);
            if (age > 0.12f && age < 0.16f) flashA = 0.5f;
            whiteFlash.color = new Color(1f, 1f, 1f, flashA);

            // Spinning bursts behind the crosshair.
            float bs = Mathf.Lerp(0.3f, 1.4f, Mathf.Sqrt(age));
            burst.rectTransform.localScale = Vector3.one * bs;
            burst.rectTransform.localRotation = Quaternion.Euler(0, 0, age * 220f);
            burst.color = new Color(1f, 1f, 1f, Mathf.Clamp01(1f - age * 1.4f));
            burst2.rectTransform.localScale = Vector3.one * bs * 0.8f;
            burst2.rectTransform.localRotation = Quaternion.Euler(0, 0, -age * 300f);
            burst2.color = new Color(0.4f, 0.95f, 1f, Mathf.Clamp01(0.9f - age * 1.3f));

            // "PARRY" slams in big, settles, shakes, fades.
            float slamScale = age < 0.07f ? Mathf.Lerp(3f, 1f, age / 0.07f) : 1f + Mathf.Max(0f, 0.08f - (age - 0.07f)) * 2f;
            Vector2 jitter = age < 0.3f ? Random.insideUnitCircle * 6f : Vector2.zero;
            float slamA = Mathf.Clamp01((1f - age) * 2.2f);
            slam.rectTransform.localScale = slamShadow.rectTransform.localScale = Vector3.one * slamScale;
            slam.rectTransform.anchoredPosition = new Vector2(0, 120) + jitter;
            slamShadow.rectTransform.anchoredPosition = new Vector2(8, 112) + jitter;
            slam.color = Color.Lerp(new Color(0.4f, 0.95f, 1f), Color.white, Mathf.PingPong(age * 12f, 1f)) * new Color(1, 1, 1, slamA);
            slamShadow.color = new Color(0, 0, 0, 0.9f * slamA);

            // FOV punch in, ease back out.
            if (cam) cam.fieldOfView = baseFov + fovPunch * Mathf.Sin(Mathf.Clamp01(age * 2.5f) * Mathf.PI) * (1f - age);
        }

        void SetVisible(bool v)
        {
            whiteFlash.enabled = burst.enabled = burst2.enabled = slam.enabled = slamShadow.enabled = v;
        }

        void BuildSparks()
        {
            var go = new GameObject("Parry Sparks", typeof(ParticleSystem));
            go.transform.SetParent(transform, false);
            sparks = go.GetComponent<ParticleSystem>();
            sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = sparks.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 1f;
            main.useUnscaledTime = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 10f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.07f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.95f, 0.6f), new Color(0.5f, 0.95f, 1f));
            main.gravityModifier = 1.2f;
            main.maxParticles = 300;

            var emission = sparks.emission;
            emission.enabled = false;

            var shape = sparks.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.08f;

            var col = sparks.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.6f, 0.2f), 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = g;

            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.velocityScale = 0.04f;
            r.lengthScale = 1.5f;
            if (sparkMaterial) r.sharedMaterial = sparkMaterial;
        }

        // ---- UI helpers -------------------------------------------------------------------

        static Image FullScreen(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return img;
        }

        static Image Img(Transform parent, string name, Sprite sprite, float size, Color c)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.color = c;
            img.raycastTarget = false;
            ((RectTransform)go.transform).sizeDelta = Vector2.one * size;
            return img;
        }

        static Text Label(Transform parent, string name, Font font, Color c, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = font;
            t.text = "PARRY";
            t.fontSize = 140;
            t.fontStyle = FontStyle.BoldAndItalic;
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.color = c;
            t.raycastTarget = false;
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(1000, 200);
            rt.anchoredPosition = pos;
            return t;
        }
    }
}
