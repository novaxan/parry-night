using UnityEngine;

namespace NightShift
{
    /// <summary>Security camera tablet. One camera hops between room anchors and renders into a low-res feed.</summary>
    public class CameraTablet : MonoBehaviour
    {
        public static CameraTablet Instance { get; private set; }

        [SerializeField] Camera feedCamera;
        [SerializeField] Room[] rooms;
        [SerializeField] AudioSource sfx;
        [SerializeField] Vector2Int feedResolution = new Vector2Int(480, 270);

        public bool IsUp { get; private set; }
        public int Selected { get; private set; }
        public Room[] Rooms => rooms;
        public RenderTexture Feed { get; private set; }

        public event System.Action<bool> Toggled;
        public event System.Action<int> Switched;

        void Awake()
        {
            Instance = this;
            Feed = new RenderTexture(feedResolution.x, feedResolution.y, 24) { filterMode = FilterMode.Point, name = "CameraFeed" };
            if (feedCamera)
            {
                feedCamera.targetTexture = Feed;
                feedCamera.enabled = false;
            }
        }

        void Start() => Select(0, silent: true);

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (Feed) Feed.Release();
        }

        void Update()
        {
            var nm = NightManager.Instance;
            var input = GameInput.Instance;
            if (!nm || !input) return;

            if (nm.State != GameState.Playing)
            {
                if (IsUp) SetUp(false);
                return;
            }

            if (input.Tablet.WasPressedThisFrame()) SetUp(!IsUp);

            if (IsUp)
            {
                for (int i = 0; i < input.Cameras.Length && i < rooms.Length; i++)
                    if (input.Cameras[i].WasPressedThisFrame()) Select(i);
                if (input.TurnLeft.WasPressedThisFrame()) Select((Selected + rooms.Length - 1) % rooms.Length);
                if (input.TurnRight.WasPressedThisFrame()) Select((Selected + 1) % rooms.Length);
            }
        }

        public void SetUp(bool up)
        {
            if (IsUp == up) return;
            IsUp = up;
            if (feedCamera) feedCamera.enabled = up;
            if (sfx) sfx.PlayOneShot(ProceduralSfx.Beep, 0.6f);
            Toggled?.Invoke(up);
        }

        public void Select(int index, bool silent = false)
        {
            if (rooms == null || rooms.Length == 0) return;
            Selected = Mathf.Clamp(index, 0, rooms.Length - 1);
            var anchor = rooms[Selected].cameraAnchor ? rooms[Selected].cameraAnchor : rooms[Selected].transform;
            if (feedCamera) feedCamera.transform.SetPositionAndRotation(anchor.position, anchor.rotation);
            if (!silent && sfx) sfx.PlayOneShot(ProceduralSfx.Static, 0.7f);
            Switched?.Invoke(Selected);
        }
    }
}
