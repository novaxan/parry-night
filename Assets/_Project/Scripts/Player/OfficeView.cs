using UnityEngine;

namespace NightShift
{
    /// <summary>Turns the seated player between the left door, front vent and right door.</summary>
    public class OfficeView : MonoBehaviour
    {
        [SerializeField] float turnSpeed = 14f;
        [SerializeField] float leftYaw = -90f, frontYaw = 0f, rightYaw = 90f;
        [SerializeField] float doorPitch = 4f, ventPitch = 16f;
        [Tooltip("How close (degrees) the view must be to count as covering an opening.")]
        [SerializeField] float coverTolerance = 20f;

        public Opening Facing { get; private set; } = Opening.Front;
        /// <summary>The opening currently covered, or None while mid-turn.</summary>
        public Opening Covered => Facing != Opening.None && Mathf.Abs(Mathf.DeltaAngle(yaw, TargetYaw)) <= coverTolerance ? Facing : Opening.None;
        public bool Locked { get; set; }
        public event System.Action<Opening> Turned;

        float yaw, pitch;
        Vector3 shakeEuler;

        float TargetYaw => Facing switch { Opening.Left => leftYaw, Opening.Right => rightYaw, _ => frontYaw };
        float TargetPitch => Facing == Opening.Front ? ventPitch : doorPitch;

        void Start()
        {
            yaw = TargetYaw;
            pitch = TargetPitch;
            Apply();
        }

        void Update()
        {
            var nm = NightManager.Instance;
            var input = GameInput.Instance;
            bool canTurn = !Locked && nm && nm.State == GameState.Playing && !(CameraTablet.Instance && CameraTablet.Instance.IsUp);
            if (canTurn && input)
            {
                if (input.TurnLeft.WasPressedThisFrame()) Turn(-1);
                if (input.TurnRight.WasPressedThisFrame()) Turn(+1);
            }

            float k = 1f - Mathf.Exp(-turnSpeed * Time.unscaledDeltaTime);
            if (Facing != Opening.None)
            {
                yaw = Mathf.LerpAngle(yaw, TargetYaw, k);
                pitch = Mathf.Lerp(pitch, TargetPitch, k);
            }
            shakeEuler = Vector3.Lerp(shakeEuler, Vector3.zero, 1f - Mathf.Exp(-10f * Time.unscaledDeltaTime));
            Apply();
        }

        void Turn(int dir)
        {
            int idx = Facing switch { Opening.Left => -1, Opening.Right => 1, _ => 0 };
            idx = Mathf.Clamp(idx + dir, -1, 1);
            var next = idx < 0 ? Opening.Left : idx > 0 ? Opening.Right : Opening.Front;
            if (next == Facing) return;
            Facing = next;
            Turned?.Invoke(Facing);
        }

        /// <summary>Snap the view toward a world point (used by the jumpscare).</summary>
        public void LookAtInstant(Vector3 worldPoint)
        {
            var dir = worldPoint - transform.position;
            var e = Quaternion.LookRotation(dir).eulerAngles;
            yaw = e.y;
            pitch = e.x > 180f ? e.x - 360f : e.x;
            Facing = Opening.None;
            Apply();
        }

        public void Shake(float strength) => shakeEuler += Random.insideUnitSphere * strength;

        void Apply()
        {
            transform.localRotation = Quaternion.Euler(pitch + shakeEuler.x, yaw + shakeEuler.y, shakeEuler.z);
        }
    }
}
