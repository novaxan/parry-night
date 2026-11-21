using UnityEngine;

namespace NightShift
{
    /// <summary>Unreliable office light: mostly steady with occasional stutters.</summary>
    [RequireComponent(typeof(Light))]
    public class FlickerLight : MonoBehaviour
    {
        [SerializeField] float flickerChancePerSecond = 0.25f;
        Light lt;
        float baseIntensity;
        float flickerUntil;

        void Awake()
        {
            lt = GetComponent<Light>();
            baseIntensity = lt.intensity;
        }

        void Update()
        {
            if (Time.time > flickerUntil && Random.value < flickerChancePerSecond * Time.deltaTime)
                flickerUntil = Time.time + Random.Range(0.1f, 0.5f);

            float n = Mathf.PerlinNoise(Time.time * 3f, 0f) * 0.15f + 0.9f;
            lt.intensity = Time.time < flickerUntil
                ? (Random.value > 0.5f ? baseIntensity * 0.2f : baseIntensity)
                : baseIntensity * n;
        }
    }
}
