using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NightShift
{
    public enum GameState { Title, Playing, Jumpscare, Won, Lost }

    /// <summary>Runs the 12 AM to 6 AM clock, hourly events, and win/lose/restart.</summary>
    public class NightManager : MonoBehaviour
    {
        public static NightManager Instance { get; private set; }

        [Tooltip("Real seconds per in-game hour.")]
        [SerializeField] float hourLength = 60f;
        [SerializeField] int endHour = 6;
        [SerializeField] int supplyDropHour = 3;
        [SerializeField] int supplyDropShells = 3;
        [SerializeField] GameObject supplyBox;
        [SerializeField] Shotgun shotgun;
        [SerializeField] Jumpscare jumpscare;
        [SerializeField] AudioSource sfx;
        [Tooltip("Looping background hum.")]
        [SerializeField] AudioSource ambience;

        public GameState State { get; private set; } = GameState.Title;
        public int Hour { get; private set; }
        public float HourProgress => Mathf.Clamp01((elapsed - Hour * hourLength) / hourLength);
        public string HourLabel => Hour == 0 ? "12 AM" : Hour + " AM";
        public string EndReason { get; private set; }
        public int Kills { get; private set; }
        public bool DebugFast { get; private set; }
        /// <summary>Debug (F2): robots stop thinking but keep animating. Handy for looking at them.</summary>
        public bool DebugFreezeRobots { get; set; }
        public IReadOnlyList<Robot> Robots => robots;

        public event System.Action<GameState> StateChanged;
        public event System.Action<int> HourChanged;
        public event System.Action<string> Message;

        readonly List<Robot> robots = new();
        float elapsed;
        float hitStopUntil;
        float hitStopScale = 0.05f;

        void Awake()
        {
            Instance = this;
            Time.timeScale = 1f;
            robots.AddRange(FindObjectsByType<Robot>(FindObjectsSortMode.InstanceID));
            if (supplyBox) supplyBox.SetActive(false);
            if (ambience)
            {
                ambience.clip = ProceduralSfx.Hum;
                ambience.loop = true;
                ambience.Play();
            }
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            Time.timeScale = 1f;
        }

        public void BeginNight()
        {
            if (State != GameState.Title) return;
            SetState(GameState.Playing);
            PlaySfx(ProceduralSfx.Chime, 0.8f);
            Message?.Invoke("12 AM. Keep them out.");
        }

        void Update()
        {
            var input = GameInput.Instance;
            if (input)
            {
                if (State == GameState.Title && input.Begin.WasPressedThisFrame()) BeginNight();
                else if ((State == GameState.Won || State == GameState.Lost) && input.Restart.WasPressedThisFrame()) Restart();
                if (input.DebugSpeed.WasPressedThisFrame()) DebugFast = !DebugFast;
                if (input.DebugFreeze.WasPressedThisFrame()) DebugFreezeRobots = !DebugFreezeRobots;
            }

            Time.timeScale = Time.unscaledTime < hitStopUntil ? hitStopScale : (DebugFast && State == GameState.Playing ? 10f : 1f);

            if (State != GameState.Playing) return;

            elapsed += Time.deltaTime;
            int h = Mathf.Min(endHour, Mathf.FloorToInt(elapsed / hourLength));
            if (h != Hour)
            {
                Hour = h;
                HourChanged?.Invoke(Hour);
                if (Hour >= endHour) { Win("6 AM. You survived the night."); return; }
                PlaySfx(ProceduralSfx.Chime, 0.5f);
                if (Hour == supplyDropHour) SupplyDrop();
            }
        }

        void SupplyDrop()
        {
            if (shotgun) shotgun.Model.AddReserve(supplyDropShells);
            if (supplyBox) supplyBox.SetActive(true);
            Message?.Invoke($"3 AM supply drop: +{supplyDropShells} shells");
        }

        /// <summary>Freeze time briefly for impact (parries, kills).</summary>
        public void HitStop(float realSeconds, float scale = 0.05f)
        {
            if (Time.unscaledTime >= hitStopUntil) hitStopScale = scale;
            else hitStopScale = Mathf.Min(hitStopScale, scale);
            hitStopUntil = Mathf.Max(hitStopUntil, Time.unscaledTime + realSeconds);
        }

        public void ShowMessage(string msg) => Message?.Invoke(msg);

        public void OnRobotKilled(Robot r)
        {
            Kills++;
            Message?.Invoke($"{r.Data.displayName} SCRAPPED");
            if (State == GameState.Playing && robots.TrueForAll(x => !x.Brain.IsAlive))
                StartCoroutine(WinAfter(1.5f, "Every robot scrapped. The night is yours."));
        }

        IEnumerator WinAfter(float delay, string reason)
        {
            yield return new WaitForSeconds(delay);
            if (State == GameState.Playing) Win(reason);
        }

        public void OnRobotAttack(Robot r)
        {
            if (State != GameState.Playing) return;
            SetState(GameState.Jumpscare);
            EndReason = $"{r.Data.displayName} got you at {HourLabel}.";
            if (jumpscare) jumpscare.Play(r, () => SetState(GameState.Lost));
            else SetState(GameState.Lost);
        }

        void Win(string reason)
        {
            EndReason = reason;
            SetState(GameState.Won);
            PlaySfx(ProceduralSfx.Chime, 1f);
        }

        void PlaySfx(AudioClip clip, float volume)
        {
            if (sfx) sfx.PlayOneShot(clip, volume);
        }

        void SetState(GameState s)
        {
            State = s;
            StateChanged?.Invoke(s);
        }

        public void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>Debug helper: jump the clock forward.</summary>
        public void DebugSetElapsed(float seconds) => elapsed = seconds;
#endif
    }
}
