using UnityEngine;

namespace NightShift
{
    /// <summary>Scene-side robot: moves between rooms, animates its states, and reports attacks and deaths.</summary>
    public class Robot : MonoBehaviour
    {
        [SerializeField] RobotData data;
        [Tooltip("Rooms from start to the office opening. The last entry must be an opening.")]
        [SerializeField] Room[] route;
        [Tooltip("Sideways slot when sharing a room with other robots (0..2).")]
        [SerializeField] int slot = 1;
        [SerializeField] Transform visual;
        [SerializeField] Transform eyes;
        [SerializeField] Renderer[] eyeRenderers;
        [SerializeField] Light eyeLight;
        [SerializeField] AudioSource voice;
        [Tooltip("How far it lunges toward the office, in metres.")]
        [SerializeField] float lungeDistance = 1.6f;
        [Tooltip("Optional hinged jaw (or lid). Rotates open around its local X axis when attacking.")]
        [SerializeField] Transform jaw;
        [SerializeField] float jawOpenAngle = 35f;

        public RobotData Data => data;
        public RobotBrain Brain { get; private set; }
        public Opening TargetOpening => route[^1].opening;
        public Room CurrentRoom => route[Brain.RouteIndex];
        public Vector3 EyePosition => eyes ? eyes.position : transform.position + Vector3.up * 1.6f;

        MaterialPropertyBlock mpb;
        bool jumpscaring;
        float hitFlash;
        float deathTime;
        float parryFlash;
        Quaternion jawRest = Quaternion.identity;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        void Awake()
        {
            Brain = new RobotBrain(data, route.Length, () => Random.value);
            Brain.Moved += OnMoved;
            Brain.StateChanged += OnStateChanged;
            mpb = new MaterialPropertyBlock();
            if (jaw) jawRest = jaw.localRotation;
        }

        void Start() => Brain.Place(0);

        void Update()
        {
            var nm = NightManager.Instance;
            if (!nm || nm.State != GameState.Playing || nm.DebugFreezeRobots) { Animate(); return; }

            Brain.Aggression = data.startAggression + nm.Hour * data.aggressionPerHour;
            Brain.Tick(Time.deltaTime);
            Animate();
        }

        void OnMoved(int index)
        {
            var room = route[index];
            transform.SetPositionAndRotation(room.GetStandPosition(slot), room.GetStandRotation());
            if (visual) { visual.localPosition = Vector3.zero; visual.localRotation = Quaternion.identity; }

            // Directional audio hints: footsteps near the opening as it gets close.
            if (index >= Brain.OpeningIndex - 1 && Brain.State != RobotState.Dead)
            {
                var opening = route[^1];
                float vol = index == Brain.OpeningIndex ? 1f : 0.55f;
                AudioSource.PlayClipAtPoint(ProceduralSfx.Footstep, opening.transform.position, vol);
            }
        }

        void OnStateChanged(RobotState s)
        {
            var nm = NightManager.Instance;
            switch (s)
            {
                case RobotState.Telegraph:
                    PlayVoice(ProceduralSfx.Screech, 1f);
                    break;
                case RobotState.Lunge:
                    PlayVoice(ProceduralSfx.Whoosh, 1f);
                    break;
                case RobotState.Attacked:
                    if (nm) nm.OnRobotAttack(this);
                    break;
                case RobotState.Dead:
                    deathTime = Time.time;
                    if (voice) voice.Stop();
                    PlayVoice(ProceduralSfx.Crunch, 1f);
                    if (nm) nm.OnRobotKilled(this);
                    break;
            }
        }

        /// <summary>Visual reaction to taking a shot.</summary>
        public void OnShot() => hitFlash = 1f;

        /// <summary>Visual reaction to being parried.</summary>
        public void OnParried() => parryFlash = 1f;

        void PlayVoice(AudioClip clip, float vol)
        {
            if (voice) voice.PlayOneShot(clip, vol);
        }

        void Animate()
        {
            if (!visual) return;
            float t = Time.time;
            Vector3 pos = Vector3.zero;
            Quaternion rot = Quaternion.identity;
            Color eye = data.eyeColor;
            float eyeGlow = 1.5f;

            hitFlash = Mathf.MoveTowards(hitFlash, 0f, Time.unscaledDeltaTime * 4f);
            parryFlash = Mathf.MoveTowards(parryFlash, 0f, Time.unscaledDeltaTime * 2.5f);
            float jawOpen = 0.04f + 0.04f * Mathf.Sin(t * 2f + slot);

            if (jumpscaring)
            {
                pos = Random.insideUnitSphere * 0.04f;
                rot = Quaternion.Euler(Random.insideUnitSphere * 4f);
                eye = Color.red;
                eyeGlow = 8f;
                jawOpen = 0.85f + Random.value * 0.15f;
            }
            else switch (Brain.State)
            {
                case RobotState.Roaming:
                    rot = Quaternion.Euler(0f, Mathf.Sin(t * 0.7f + slot) * 6f, 0f);
                    break;
                case RobotState.AtOpening:
                    rot = Quaternion.Euler(Mathf.Sin(t * 1.3f) * 2f, Mathf.Sin(t * 0.9f + slot) * 8f, Mathf.Sin(t * 1.7f) * 3f);
                    break;
                case RobotState.Telegraph:
                {
                    float p = 1f - Brain.StateTimer / data.telegraphTime;
                    pos = Random.insideUnitSphere * (0.01f + 0.04f * p);
                    rot = Quaternion.Euler(-8f * p, 0f, 0f);
                    eye = Color.Lerp(data.eyeColor, Color.red, Mathf.PingPong(t * 12f, 1f) > 0.4f ? 1f : p);
                    eyeGlow = 2f + 6f * p;
                    jawOpen = 0.15f + 0.5f * p + (Random.value - 0.5f) * 0.1f;
                    break;
                }
                case RobotState.Lunge:
                {
                    float p = 1f - Brain.StateTimer / data.lungeTime;
                    pos = new Vector3(0f, 0f, lungeDistance * p * p);
                    rot = Quaternion.Euler(18f * p, 0f, 0f);
                    eye = Color.red;
                    eyeGlow = 9f;
                    jawOpen = 1f;
                    break;
                }
                case RobotState.Stunned:
                    rot = Quaternion.Euler(-22f, Mathf.Sin(t * 25f) * 4f, Mathf.Sin(t * 19f) * 6f);
                    pos = new Vector3(0f, 0f, -0.3f);
                    eye = data.eyeColor;
                    eyeGlow = Random.value > 0.5f ? 0.2f : 2.5f;
                    jawOpen = 0.3f + Mathf.Sin(t * 30f) * 0.1f;
                    break;
                case RobotState.Dead:
                {
                    float p = Mathf.Clamp01((Time.time - deathTime) * 2.5f);
                    rot = Quaternion.Euler(-85f * p * p, 0f, 10f * p);
                    pos = new Vector3(0f, 0f, -0.4f * p);
                    eyeGlow = Mathf.Lerp(3f, 0f, p);
                    jawOpen = 0.6f * p;
                    break;
                }
            }

            if (hitFlash > 0f) { eye = Color.Lerp(eye, Color.white, hitFlash); pos += Random.insideUnitSphere * 0.06f * hitFlash; }
            if (parryFlash > 0f)
            {
                eye = Color.Lerp(eye, new Color(0.4f, 0.9f, 1f), parryFlash);
                eyeGlow = Mathf.Max(eyeGlow, 10f * parryFlash);
                pos += new Vector3(0f, 0f, -0.5f * parryFlash) + Random.insideUnitSphere * 0.05f * parryFlash;
                rot *= Quaternion.Euler(-25f * parryFlash, 0f, 0f);
            }
            if (jaw) jaw.localRotation = jawRest * Quaternion.Euler(Mathf.Clamp01(jawOpen) * jawOpenAngle, 0f, 0f);

            visual.localPosition = pos;
            visual.localRotation = rot;

            if (eyeRenderers != null)
            {
                foreach (var r in eyeRenderers)
                {
                    if (!r) continue;
                    r.GetPropertyBlock(mpb);
                    mpb.SetColor(BaseColorId, eye);
                    mpb.SetColor(EmissionId, eye * eyeGlow);
                    r.SetPropertyBlock(mpb);
                }
            }
            if (eyeLight)
            {
                eyeLight.color = eye;
                eyeLight.intensity = jumpscaring ? 0.5f : eyeGlow * 0.25f;
                eyeLight.enabled = eyeGlow > 0.05f;
            }
        }

        /// <summary>Teleports the robot into the player's face.</summary>
        public void BeginJumpscare(Transform cam)
        {
            jumpscaring = true;
            var fwd = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
            if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
            transform.rotation = Quaternion.LookRotation(-fwd, Vector3.up);
            if (visual) { visual.localPosition = Vector3.zero; visual.localRotation = Quaternion.identity; }
            Vector3 eyeOffset = (eyes ? eyes.position : transform.position + Vector3.up * 1.6f) - transform.position;
            transform.position = cam.position + fwd * 0.95f - eyeOffset;
            PlayVoice(ProceduralSfx.Scream, 1f);
        }
    }
}
