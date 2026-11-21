using UnityEngine;

namespace NightShift
{
    public enum Opening { None, Left, Front, Right }

    /// <summary>A node on a robot route: a camera room, or one of the three office openings.</summary>
    public class Room : MonoBehaviour
    {
        public string displayName = "Room";
        [Tooltip("Set on the three nodes directly outside the office.")]
        public Opening opening = Opening.None;
        [Tooltip("Where the security camera for this room sits. Null for openings.")]
        public Transform cameraAnchor;
        [Tooltip("Where robots stand. Several robots in one room spread out sideways.")]
        public Transform standPoint;

        public Vector3 GetStandPosition(int slot)
        {
            var p = standPoint ? standPoint : transform;
            if (opening != Opening.None) return p.position;
            return p.position + p.right * ((slot - 1) * 1.1f);
        }

        public Quaternion GetStandRotation()
        {
            var p = standPoint ? standPoint : transform;
            return p.rotation;
        }
    }
}
