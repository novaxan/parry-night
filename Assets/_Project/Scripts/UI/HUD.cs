using UnityEngine;
using UnityEngine.UI;

namespace NightShift
{
    /// <summary>Builds and drives the whole uGUI overlay at runtime: clock, shells, facing, tablet, title and end screens.</summary>
    public class HUD : MonoBehaviour
    {
        [SerializeField] NightManager night;
        [SerializeField] Shotgun shotgun;
        [SerializeField] OfficeView view;
        [SerializeField] CameraTablet tablet;
        [SerializeField] Jumpscare jumpscare;
        [SerializeField] ParryFX parryFx;
        [SerializeField] AudioSource sfx;

        static readonly Color Amber = new Color(1f, 0.78f, 0.35f);
        static readonly Color Dim = new Color(1f, 1f, 1f, 0.55f);

        Font font;
        Text clockText, shellsText, gunStateText, facingText, messageText, camNameText, camListText, debugText, endTitle, endBody, recText;
        GameObject hudRoot, tabletRoot, titleRoot, endRoot;
        Image flash, crosshair;
        RawImage feed, staticOverlay;
        Texture2D noiseTex;
        float messageTimer, staticAmount;
        Color flashColor;

        void Awake()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildCanvas();
        }

        void OnEnable()
        {
            if (night) { night.Message += ShowMessage; night.StateChanged += OnState; }
            if (tablet) { tablet.Switched += _ => staticAmount = 1f; tablet.Toggled += up => staticAmount = 1f; }
            if (shotgun) shotgun.Acted += OnActed;
            if (jumpscare) jumpscare.Started += () => Flash(new Color(0.6f, 0f, 0f, 0.6f));
        }

        void OnDisable()
        {
            if (night) { night.Message -= ShowMessage; night.StateChanged -= OnState; }
            if (shotgun) shotgun.Acted -= OnActed;
        }

        void Start()
        {
            if (tablet) feed.texture = tablet.Feed;
            OnState(night ? night.State : GameState.Title);
        }

        // ---- runtime ------------------------------------------------------------------------

        void Update()
        {
            float dt = Time.unscaledDeltaTime;

            if (night)
            {
                clockText.text = night.HourLabel;
                debugText.text = (night.DebugFast ? "DEBUG x10 (F1)  " : "") + (night.DebugFreezeRobots ? "ROBOTS FROZEN (F2)" : "");
            }

            if (shotgun)
            {
                var m = shotgun.Model;
                string tube = "";
                for (int i = 0; i < m.TubeCapacity; i++) tube += i < m.Loaded ? "▮ " : "▯ ";
                shellsText.text = $"{tube}  <size=30>+{m.Reserve}</size>";
                shellsText.color = m.IsEmpty ? new Color(1f, 0.3f, 0.3f) : Amber;
                gunStateText.text = m.Current switch
                {
                    ShotgunModel.GunState.Reloading => "RELOADING…",
                    ShotgunModel.GunState.Pumping => "PUMP",
                    _ => m.Loaded == 0 && m.Reserve > 0 ? "R to reload" : ""
                };
            }

            if (view)
            {
                string Mark(Opening o, string label) => view.Facing == o ? $"<color=#FFC85A>[ {label} ]</color>" : label;
                facingText.text = $"{Mark(Opening.Left, "LEFT DOOR")}     {Mark(Opening.Front, "VENT")}     {Mark(Opening.Right, "RIGHT DOOR")}";
            }

            if (tablet && tablet.IsUp)
            {
                var rooms = tablet.Rooms;
                camNameText.text = $"CAM {tablet.Selected + 1}  ·  {rooms[tablet.Selected].displayName.ToUpper()}";
                string list = "";
                for (int i = 0; i < rooms.Length; i++)
                {
                    string line = $"{i + 1}  {rooms[i].displayName}";
                    list += (i == tablet.Selected ? $"<color=#FFC85A>▶ {line}</color>" : $"   {line}") + "\n";
                }
                camListText.text = list + "\n<size=20>1–6 / A D switch\nTab close</size>";
                recText.enabled = Mathf.Repeat(Time.unscaledTime, 1f) < 0.6f;

                // Rolling static: always a little, a lot right after switching.
                staticAmount = Mathf.MoveTowards(staticAmount, 0f, dt * 4f);
                var c = staticOverlay.color;
                c.a = 0.035f + 0.8f * staticAmount;
                staticOverlay.color = c;
                staticOverlay.uvRect = new Rect(Random.value, Random.value, 1.5f, 1f);
            }

            if (messageTimer > 0f)
            {
                messageTimer -= dt;
                var c = messageText.color;
                c.a = Mathf.Clamp01(messageTimer * 2f);
                messageText.color = c;
            }

            if (flash.color.a > 0f)
            {
                flashColor.a = Mathf.MoveTowards(flashColor.a, 0f, dt * 2.5f);
                flash.color = flashColor;
            }
        }

        void OnState(GameState s)
        {
            titleRoot.SetActive(s == GameState.Title);
            hudRoot.SetActive(s == GameState.Playing);
            endRoot.SetActive(s == GameState.Won || s == GameState.Lost);
            if (s != GameState.Playing)
            {
                messageTimer = 0f;
                messageText.color = new Color(1, 1, 1, 0);
            }
            if (s == GameState.Won || s == GameState.Lost)
            {
                bool won = s == GameState.Won;
                endTitle.text = won ? "6 AM: SHIFT COMPLETE" : "YOU DIED";
                endTitle.color = won ? Amber : new Color(0.9f, 0.1f, 0.1f);
                endBody.text = $"{night.EndReason}\nRobots scrapped: {night.Kills}/{night.Robots.Count}\n\n<size=28>Press R or Enter to try again</size>";
            }
            if (tablet) tabletRoot.SetActive(false);
        }

        void LateUpdate()
        {
            bool up = tablet && tablet.IsUp && night && night.State == GameState.Playing;
            if (tabletRoot.activeSelf != up) tabletRoot.SetActive(up);
            crosshair.enabled = !up;
        }

        void OnActed(bool hit, bool parry)
        {
            if (!parry) Flash(new Color(1f, 0.8f, 0.4f, hit ? 0.25f : 0.12f));
        }

        void Flash(Color c)
        {
            flashColor = c;
            flash.color = c;
        }

        void ShowMessage(string msg)
        {
            messageText.text = msg;
            messageTimer = 2.2f;
        }

        // ---- construction -------------------------------------------------------------------

        void BuildCanvas()
        {
            var canvasGo = new GameObject("HUD Canvas", typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            var root = canvasGo.transform;

            noiseTex = new Texture2D(256, 256, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
            var px = new Color32[256 * 256];
            for (int i = 0; i < px.Length; i++) { byte v = (byte)Random.Range(0, 256); px[i] = new Color32(v, v, v, 255); }
            noiseTex.SetPixels32(px);
            noiseTex.Apply();

            // In-game HUD
            hudRoot = Panel(root, "HUD", Color.clear);
            clockText = Label(hudRoot.transform, "Clock", "12 AM", 64, TextAnchor.UpperRight, new Vector2(1, 1), new Vector2(-40, -30), new Vector2(400, 80));
            var night1 = Label(hudRoot.transform, "Night", "NIGHT 1", 26, TextAnchor.UpperRight, new Vector2(1, 1), new Vector2(-40, -105), new Vector2(400, 40));
            night1.color = Dim;
            debugText = Label(hudRoot.transform, "Debug", "", 22, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(40, -30), new Vector2(400, 40));
            debugText.color = Color.cyan;
            shellsText = Label(hudRoot.transform, "Shells", "", 52, TextAnchor.LowerRight, new Vector2(1, 0), new Vector2(-40, 70), new Vector2(500, 70));
            gunStateText = Label(hudRoot.transform, "GunState", "", 26, TextAnchor.LowerRight, new Vector2(1, 0), new Vector2(-40, 30), new Vector2(500, 40));
            gunStateText.color = Dim;
            facingText = Label(hudRoot.transform, "Facing", "", 26, TextAnchor.LowerCenter, new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(1000, 40));
            facingText.color = Dim;
            var help = Label(hudRoot.transform, "Help", "A/D turn   LMB fire   RMB parry   R reload   Tab cameras", 20, TextAnchor.LowerLeft, new Vector2(0, 0), new Vector2(40, 30), new Vector2(900, 30));
            help.color = new Color(1, 1, 1, 0.35f);

            var ch = new GameObject("Crosshair", typeof(RectTransform), typeof(Image));
            ch.transform.SetParent(root, false);
            crosshair = ch.GetComponent<Image>();
            crosshair.color = new Color(1, 1, 1, 0.5f);
            ((RectTransform)ch.transform).sizeDelta = new Vector2(6, 6);

            // Danger arrows + parry timing ring around the crosshair
            gameObject.AddComponent<ThreatIndicator>().Build(hudRoot.transform, font, view, shotgun, sfx);

            // Tablet
            tabletRoot = Panel(root, "Tablet", new Color(0.02f, 0.03f, 0.03f, 0.96f));
            var feedGo = new GameObject("Feed", typeof(RectTransform), typeof(RawImage));
            feedGo.transform.SetParent(tabletRoot.transform, false);
            feed = feedGo.GetComponent<RawImage>();
            feed.color = new Color(0.85f, 1f, 0.9f);
            var frt = (RectTransform)feedGo.transform;
            frt.anchorMin = frt.anchorMax = new Vector2(0.42f, 0.5f);
            frt.sizeDelta = new Vector2(1280, 720);

            var stGo = new GameObject("Static", typeof(RectTransform), typeof(RawImage));
            stGo.transform.SetParent(feedGo.transform, false);
            staticOverlay = stGo.GetComponent<RawImage>();
            staticOverlay.texture = noiseTex;
            staticOverlay.raycastTarget = false;
            staticOverlay.color = new Color(1, 1, 1, 0.2f);
            Stretch((RectTransform)stGo.transform);

            camNameText = Label(feedGo.transform, "CamName", "", 34, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(24, -18), new Vector2(900, 50));
            recText = Label(feedGo.transform, "Rec", "● REC", 30, TextAnchor.UpperRight, new Vector2(1, 1), new Vector2(-24, -18), new Vector2(200, 50));
            recText.color = new Color(1f, 0.2f, 0.2f);
            camListText = Label(tabletRoot.transform, "CamList", "", 30, TextAnchor.UpperLeft, new Vector2(1, 0.5f), new Vector2(-40, 0), new Vector2(330, 520));
            tabletRoot.SetActive(false);

            // Full-screen flash
            var fl = Panel(root, "Flash", new Color(0, 0, 0, 0));
            flash = fl.GetComponent<Image>();
            flash.raycastTarget = false;

            // Message line
            messageText = Label(root, "Message", "", 40, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, -160), new Vector2(1400, 60));
            messageText.color = new Color(1, 1, 1, 0);

            // Parry effects sit above the HUD but under the title/end screens
            if (parryFx) parryFx.Build(root, font);

            // Title
            titleRoot = Panel(root, "Title", new Color(0, 0, 0, 0.88f));
            var t1 = Label(titleRoot.transform, "Title", "NIGHT SHIFT: BUCKSHOT", 96, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 230), new Vector2(1600, 120));
            t1.color = Amber;
            Label(titleRoot.transform, "Sub", "The robots still come for you at night. This time you brought a shotgun.", 32, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 140), new Vector2(1600, 50)).color = Dim;
            Label(titleRoot.transform, "Rules",
                "Survive from 12 AM to 6 AM, or scrap all three robots.\n\n" +
                "A / D   look at left door, vent, right door\n" +
                "Left click   fire (2 shells in the tube, pumps after every shot)\n" +
                "Right click   PARRY when a lunging robot is about to hit you. Then shoot it for triple damage\n" +
                "R   reload (slow, one shell at a time)       Tab   security cameras\n\n" +
                "Arrows by the crosshair show where a robot is. Red flashing arrow = it's attacking: TURN.\n" +
                "Facing an attacker, a ring shrinks onto the crosshair. When it turns GREEN (ding!), right click.\n" +
                "Shooting a lurking robot knocks it back. Once it lunges, only a parry stops it.",
                26, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, -70), new Vector2(1600, 360));
            var go = Label(titleRoot.transform, "Go", "Click or press Space to start your shift", 34, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, -310), new Vector2(1200, 60));
            go.color = Amber;

            // End
            endRoot = Panel(root, "End", new Color(0, 0, 0, 0.9f));
            endTitle = Label(endRoot.transform, "EndTitle", "", 110, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 120), new Vector2(1600, 140));
            endBody = Label(endRoot.transform, "EndBody", "", 36, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, -80), new Vector2(1600, 260));
            endRoot.SetActive(false);
        }

        GameObject Panel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            Stretch((RectTransform)go.transform);
            return go;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        Text Label(Transform parent, string name, string text, int size, TextAnchor align, Vector2 anchor, Vector2 pos, Vector2 boxSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Shadow));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = font;
            t.text = text;
            t.fontSize = size;
            t.alignment = align;
            t.color = Color.white;
            t.supportRichText = true;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            go.GetComponent<Shadow>().effectColor = new Color(0, 0, 0, 0.8f);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(anchor.x, anchor.y);
            rt.anchoredPosition = pos;
            rt.sizeDelta = boxSize;
            return t;
        }
    }
}
