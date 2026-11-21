using UnityEngine;
using UnityEngine.InputSystem;

namespace NightShift
{
    /// <summary>All controls, defined in code so there is no asset to keep in sync.</summary>
    public class GameInput : MonoBehaviour
    {
        public static GameInput Instance { get; private set; }

        public InputAction TurnLeft { get; private set; }
        public InputAction TurnRight { get; private set; }
        public InputAction Fire { get; private set; }
        public InputAction Parry { get; private set; }
        public InputAction Reload { get; private set; }
        public InputAction Tablet { get; private set; }
        public InputAction Begin { get; private set; }
        public InputAction Restart { get; private set; }
        public InputAction DebugSpeed { get; private set; }
        public InputAction DebugFreeze { get; private set; }
        public InputAction[] Cameras { get; private set; }

        InputActionMap map;

        void Awake()
        {
            Instance = this;
            map = new InputActionMap("NightShift");

            TurnLeft = Add("TurnLeft", "<Keyboard>/a", "<Keyboard>/leftArrow", "<Keyboard>/q", "<Gamepad>/leftShoulder", "<Gamepad>/dpad/left");
            TurnRight = Add("TurnRight", "<Keyboard>/d", "<Keyboard>/rightArrow", "<Keyboard>/e", "<Gamepad>/rightShoulder", "<Gamepad>/dpad/right");
            Fire = Add("Fire", "<Mouse>/leftButton", "<Keyboard>/space", "<Gamepad>/rightTrigger");
            Parry = Add("Parry", "<Mouse>/rightButton", "<Keyboard>/f", "<Gamepad>/leftTrigger");
            Reload = Add("Reload", "<Keyboard>/r", "<Gamepad>/buttonWest");
            Tablet = Add("Tablet", "<Keyboard>/tab", "<Keyboard>/s", "<Keyboard>/downArrow", "<Gamepad>/buttonNorth");
            Begin = Add("Begin", "<Keyboard>/space", "<Keyboard>/enter", "<Mouse>/leftButton", "<Gamepad>/start", "<Gamepad>/buttonSouth");
            Restart = Add("Restart", "<Keyboard>/r", "<Keyboard>/enter", "<Gamepad>/start");
            DebugSpeed = Add("DebugSpeed", "<Keyboard>/f1");
            DebugFreeze = Add("DebugFreeze", "<Keyboard>/f2");

            Cameras = new InputAction[6];
            for (int i = 0; i < 6; i++)
                Cameras[i] = Add("Cam" + (i + 1), $"<Keyboard>/{i + 1}", $"<Keyboard>/numpad{i + 1}");

            map.Enable();
        }

        InputAction Add(string name, params string[] bindings)
        {
            var a = map.AddAction(name, InputActionType.Button);
            foreach (var b in bindings) a.AddBinding(b);
            return a;
        }

        void OnDestroy()
        {
            map?.Disable();
            map?.Dispose();
            if (Instance == this) Instance = null;
        }
    }
}
