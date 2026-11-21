using System.Collections;
using UnityEngine;

namespace NightShift
{
    /// <summary>The player's shotgun: fire, pump, reload (with a visible hand + shell), and the parry shove.</summary>
    public class Shotgun : MonoBehaviour
    {
        [SerializeField] OfficeView view;
        [SerializeField] Transform gunRoot;
        [SerializeField] Transform pumpSlide;
        [SerializeField] Light muzzleLight;
        [SerializeField] AudioSource sfx;

        [Header("Reload rig")]
        [Tooltip("Left hand, parented to the camera. Hidden when not reloading.")]
        [SerializeField] Transform reloadHand;
        [Tooltip("Shell held by the hand.")]
        [SerializeField] Transform heldShell;
        [Tooltip("Point on the gun where shells go in (under the receiver).")]
        [SerializeField] Transform loadingPort;
        [Tooltip("Left hand on the pump. Hidden while the reload hand is busy.")]
        [SerializeField] Transform pumpHand;

        [Header("Rules")]
        [SerializeField] int tubeCapacity = 2;
        [SerializeField] int startReserve = 6;
        [SerializeField] float pumpTime = 0.5f;
        [SerializeField] float reloadPerShell = 0.9f;
        [SerializeField] float parryCooldown = 0.45f;
        [Tooltip("A parry pressed slightly early stays armed this long, so it still lands when the window opens.")]
        [SerializeField] float parryBuffer = 0.25f;
        [SerializeField] float baseDamage = 1f;

        public ShotgunModel Model { get; private set; }

        /// <summary>Raised after every shot or parry attempt: (hit something, was a parry).</summary>
        public event System.Action<bool, bool> Acted;
        /// <summary>Raised on a successful parry, with the robot that got parried.</summary>
        public event System.Action<Robot> Parried;

        Vector3 restPos;
        Quaternion restRot;
        float recoil, thrust, lowered, reloadBlend, insertKick;
        float muzzle;
        Vector3 pumpRest;
        float parryArmedUntil = -1f;
        float lastReloadPhase;
        Vector3 handHidden = new Vector3(-0.05f, -0.6f, 0.3f);

        void Awake()
        {
            Model = new ShotgunModel(tubeCapacity, tubeCapacity, startReserve, pumpTime, reloadPerShell, parryCooldown);
            if (gunRoot) { restPos = gunRoot.localPosition; restRot = gunRoot.localRotation; }
            if (pumpSlide) pumpRest = pumpSlide.localPosition;
            if (reloadHand) reloadHand.gameObject.SetActive(false);
        }

        void Update()
        {
            Model.Tick(Time.deltaTime);

            var nm = NightManager.Instance;
            var input = GameInput.Instance;
            bool tabletUp = CameraTablet.Instance && CameraTablet.Instance.IsUp;
            bool active = nm && nm.State == GameState.Playing && !tabletUp;

            if (active)
            {
                if (input)
                {
                    if (input.Fire.WasPressedThisFrame()) Fire();
                    if (input.Parry.WasPressedThisFrame()) Parry();
                    if (input.Reload.WasPressedThisFrame()) Reload();
                }
                ResolveArmedParry();
            }
            else parryArmedUntil = -1f;

            AnimateGun(tabletUp || (nm && nm.State is GameState.Jumpscare or GameState.Lost));
            AnimateReloadHand();
        }

        /// <summary>The live robot standing in the given opening, if any.</summary>
        public static Robot RobotAt(Opening o)
        {
            var nm = NightManager.Instance;
            if (o == Opening.None || !nm) return null;
            foreach (var r in nm.Robots)
                if (r.Brain.IsAlive && r.Brain.IsAtOpening && r.TargetOpening == o) return r;
            return null;
        }

        /// <summary>True while a buffered parry is waiting for the window to open.</summary>
        public bool ParryArmed => Time.time < parryArmedUntil;

        void Fire()
        {
            if (!Model.TryFire())
            {
                if (Model.Loaded == 0)
                {
                    Play(ProceduralSfx.DryClick, 1f);
                    NightManager.Instance.ShowMessage(Model.Reserve > 0 ? "EMPTY. Press R to reload" : "OUT OF SHELLS");
                }
                return;
            }

            Play(ProceduralSfx.Shot, 1f);
            StartCoroutine(PumpSoundAfter(0.18f));
            recoil = 1f;
            muzzle = 1f;
            view.Shake(3.5f);

            var target = RobotAt(view.Covered);
            bool hit = false;
            if (target)
            {
                bool wasStunned = target.Brain.State == RobotState.Stunned;
                float dmg = target.Brain.TakeShot(baseDamage);
                hit = dmg > 0f;
                if (hit)
                {
                    target.OnShot();
                    if (!target.Brain.IsAlive) NightManager.Instance.HitStop(0.15f);
                    else if (wasStunned) NightManager.Instance.ShowMessage("CRITICAL HIT");
                }
            }
            Acted?.Invoke(hit, false);
        }

        IEnumerator PumpSoundAfter(float delay)
        {
            yield return new WaitForSeconds(delay);
            Play(ProceduralSfx.Pump, 0.9f);
        }

        void Parry()
        {
            if (!Model.TryParry()) return;
            thrust = 1f;
            parryArmedUntil = Time.time + parryBuffer;
            if (!ResolveArmedParry())
                Play(ProceduralSfx.Whoosh, 0.5f);
        }

        /// <summary>Lands a buffered parry the moment the robot in front enters its window.</summary>
        bool ResolveArmedParry()
        {
            if (!ParryArmed) return false;
            var target = RobotAt(view.Covered);
            if (!target || !target.Brain.TryParry()) return false;

            parryArmedUntil = -1f;
            thrust = Mathf.Max(thrust, 0.6f);
            Play(ProceduralSfx.Clang, 1f);
            view.Shake(7f);
            target.OnParried();
            NightManager.Instance.HitStop(0.22f, 0f);
            NightManager.Instance.ShowMessage("SHOOT IT NOW");
            Parried?.Invoke(target);
            Acted?.Invoke(true, true);
            return true;
        }

        void Reload()
        {
            if (Model.TryStartReload())
            {
                lastReloadPhase = 0f;
                Play(ProceduralSfx.DryClick, 0.4f);
            }
        }

        void Play(AudioClip clip, float vol)
        {
            if (sfx) sfx.PlayOneShot(clip, vol);
        }

        // ---- animation --------------------------------------------------------------------

        static float Smooth(float t) => t * t * (3f - 2f * t);

        void AnimateGun(bool hide)
        {
            float dt = Time.unscaledDeltaTime;
            bool reloading = Model.Current == ShotgunModel.GunState.Reloading;
            recoil = Mathf.MoveTowards(recoil, 0f, dt * 5f);
            thrust = Mathf.MoveTowards(thrust, 0f, dt * 3.5f);
            lowered = Mathf.MoveTowards(lowered, hide ? 1f : 0f, dt * 5f);
            muzzle = Mathf.MoveTowards(muzzle, 0f, dt * 20f);
            reloadBlend = Mathf.MoveTowards(reloadBlend, reloading ? 1f : 0f, dt * 6f);
            insertKick = Mathf.MoveTowards(insertKick, 0f, dt * 6f);

            if (muzzleLight) { muzzleLight.intensity = muzzle * 25f; muzzleLight.enabled = muzzle > 0.01f; }
            if (!gunRoot) return;

            float rb = Smooth(reloadBlend);
            float thrustCurve = Mathf.Sin(thrust * Mathf.PI);   // out and back
            float idleBob = Mathf.Sin(Time.time * 1.6f) * 0.004f;

            // Reload pose: gun swings toward the centre, rolls onto its side and tips up so the loading
            // port faces the camera. A small kick each time a shell is pushed home.
            Vector3 reloadOffset = new Vector3(-0.15f, 0.1f, -0.02f) * rb + new Vector3(0f, insertKick * 0.015f, -insertKick * 0.02f);
            Vector3 thrustOffset = new Vector3(-thrustCurve * 0.12f, thrustCurve * 0.05f, thrustCurve * 0.3f);

            Vector3 pos = restPos
                + new Vector3(0f, idleBob - lowered * 0.5f, -recoil * 0.12f)
                + reloadOffset + thrustOffset;
            Quaternion rot = restRot * Quaternion.Euler(
                -recoil * 14f - rb * 16f - insertKick * 4f,
                thrustCurve * -25f - rb * 6f,
                -rb * 72f + thrustCurve * 30f);   // roll the belly (loading port) toward the camera

            gunRoot.localPosition = pos;
            gunRoot.localRotation = rot;

            if (pumpSlide)
            {
                float pump = 0f;
                if (Model.Current == ShotgunModel.GunState.Pumping)
                    pump = Mathf.Sin((1f - Model.ActionTimer / Model.PumpTime) * Mathf.PI);
                pumpSlide.localPosition = pumpRest + new Vector3(0f, 0f, -0.14f * pump);
            }
        }

        /// <summary>
        /// One cycle per shell: hand rises holding a shell, lines it up under the port, pushes it in,
        /// then drops out of view for the next one.
        /// </summary>
        void AnimateReloadHand()
        {
            if (!reloadHand || !loadingPort) return;
            bool reloading = Model.Current == ShotgunModel.GunState.Reloading;
            if (pumpHand) pumpHand.gameObject.SetActive(!reloadHand.gameObject.activeSelf);
            var parent = reloadHand.parent;

            if (!reloading)
            {
                // Drop out of view quickly, then hide.
                reloadHand.localPosition = Vector3.MoveTowards(reloadHand.localPosition, handHidden, Time.unscaledDeltaTime * 2.5f);
                if ((reloadHand.localPosition - handHidden).sqrMagnitude < 0.0001f) reloadHand.gameObject.SetActive(false);
                return;
            }

            if (!reloadHand.gameObject.activeSelf)
            {
                reloadHand.gameObject.SetActive(true);
                reloadHand.localPosition = handHidden;
            }

            float p = 1f - Mathf.Clamp01(Model.ActionTimer / Model.ReloadPerShell);   // 0..1 within this shell
            if (p < lastReloadPhase - 0.5f) lastReloadPhase = 0f;   // wrapped to the next shell

            Vector3 port = parent.InverseTransformPoint(loadingPort.position);
            Vector3 down = parent.InverseTransformDirection(-loadingPort.up);      // out of the port
            Vector3 fwd = parent.InverseTransformDirection(loadingPort.forward);   // along the barrel
            Vector3 below = port + down * 0.13f - fwd * 0.05f;   // shell lined up just outside the port
            Vector3 home = port + down * 0.045f + fwd * 0.02f;    // shell pushed into the tube
            Vector3 pos;
            bool shellVisible = true;

            if (p < 0.3f)            // rise from below with a fresh shell
                pos = Vector3.Lerp(handHidden, below, Smooth(p / 0.3f));
            else if (p < 0.55f)      // line it up (small wiggle so the shell reads clearly)
                pos = below + new Vector3(Mathf.Sin(p * 40f) * 0.004f, 0f, 0f);
            else if (p < 0.72f)      // thumb it into the port
                pos = Vector3.Lerp(below, home, Smooth((p - 0.55f) / 0.17f));
            else                     // shell is in; hand falls away for the next one
            {
                shellVisible = false;
                pos = Vector3.Lerp(home, handHidden, Smooth((p - 0.72f) / 0.28f));
            }

            if (lastReloadPhase < 0.72f && p >= 0.72f)
            {
                insertKick = 1f;
                Play(ProceduralSfx.ShellIn, 0.9f);
            }
            lastReloadPhase = p;

            reloadHand.localPosition = pos;
            // Palm faces the port with the shell on top, pointing along the barrel.
            reloadHand.localRotation = Quaternion.LookRotation(fwd, -down);
            if (heldShell) heldShell.gameObject.SetActive(shellVisible);
        }
    }
}
