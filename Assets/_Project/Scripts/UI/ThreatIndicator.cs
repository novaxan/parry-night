using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NightShift
{
    /// <summary>
    /// Crosshair-centred danger HUD.
    /// - An arrow beside the crosshair points toward every opening that has a robot in it
    ///   (amber = lurking, flashing red = attacking, so turn now).
    /// - When you face a robot, brackets lock around the crosshair.
    /// - When the robot you face attacks, a ring shrinks onto the crosshair. It lands on the inner ring and
    ///   turns GREEN exactly when the parry window opens (with a ding), so you know exactly when to click.
    /// </summary>
    public class ThreatIndicator : MonoBehaviour
    {
        static readonly Color Amber = new Color(1f, 0.75f, 0.25f);
        static readonly Color Danger = new Color(1f, 0.15f, 0.1f);
        static readonly Color ParryGreen = new Color(0.3f, 1f, 0.45f);
        static readonly Color StunCyan = new Color(0.4f, 0.95f, 1f);

        class Marker
        {
            public RectTransform root;
            public Image arrow, arrow2;
            public Text label;
        }

        OfficeView view;
        Shotgun gun;
        AudioSource sfx;

        RectTransform center;
        readonly Dictionary<Opening, Marker> markers = new();
        Image[] brackets;
        Image outerRing, targetRing, burstHint;
        Text centerLabel, edgeLabel;
        Image edgeLeft, edgeRight;
        readonly HashSet<Robot> windowWasOpen = new();

        public void Build(Transform parent, Font font, OfficeView view, Shotgun gun, AudioSource sfx)
        {
            this.view = view;
            this.gun = gun;
            this.sfx = sfx;

            var go = new GameObject("Threat Indicator", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            center = (RectTransform)go.transform;
            center.anchorMin = center.anchorMax = new Vector2(0.5f, 0.5f);
            center.sizeDelta = Vector2.zero;

            // Screen-edge glow on the side you need to turn toward.
            edgeLeft = EdgeGlow(parent, "EdgeLeft", left: true);
            edgeRight = EdgeGlow(parent, "EdgeRight", left: false);

            // Parry rings
            targetRing = Img(center, "TargetRing", UiSprites.Ring, new Vector2(64, 64), Color.white);
            outerRing = Img(center, "OuterRing", UiSprites.Ring, new Vector2(64, 64), Color.white);
            burstHint = Img(center, "WindowBurst", UiSprites.Burst, new Vector2(150, 150), ParryGreen);

            // Lock-on brackets
            brackets = new Image[4];
            for (int i = 0; i < 4; i++)
            {
                brackets[i] = Img(center, "Bracket" + i, UiSprites.Corner, new Vector2(26, 26), Amber);
                brackets[i].rectTransform.localRotation = Quaternion.Euler(0, 0, -90f * i);
            }

            centerLabel = Label(center, "CenterLabel", font, 30, new Vector2(0, -78));
            edgeLabel = Label(center, "TurnLabel", font, 30, new Vector2(0, 78));

            foreach (var o in new[] { Opening.Left, Opening.Front, Opening.Right })
            {
                var m = new Marker();
                var mgo = new GameObject("Marker " + o, typeof(RectTransform));
                mgo.transform.SetParent(center, false);
                m.root = (RectTransform)mgo.transform;
                m.arrow = Img(m.root, "Arrow", UiSprites.Triangle, new Vector2(56, 56), Amber);
                m.arrow2 = Img(m.root, "Arrow2", UiSprites.Triangle, new Vector2(56, 56), Amber);
                m.arrow2.rectTransform.anchoredPosition = new Vector2(26, 0);
                m.label = Label(m.root, "Label", font, 26, new Vector2(0, -44));
                markers[o] = m;
            }
            HideAll();
        }

        void HideAll()
        {
            foreach (var m in markers.Values) m.root.gameObject.SetActive(false);
            foreach (var b in brackets) b.enabled = false;
            outerRing.enabled = targetRing.enabled = burstHint.enabled = false;
            centerLabel.text = edgeLabel.text = "";
            edgeLeft.enabled = edgeRight.enabled = false;
        }

        static int Index(Opening o) => o switch { Opening.Left => -1, Opening.Right => 1, _ => 0 };

        void LateUpdate()
        {
            var nm = NightManager.Instance;
            bool tabletUp = CameraTablet.Instance && CameraTablet.Instance.IsUp;
            if (!nm || nm.State != GameState.Playing || view == null)
            {
                HideAll();
                windowWasOpen.Clear();
                return;
            }

            float t = Time.unscaledTime;
            float pulse = Mathf.PingPong(t * 6f, 1f);
            bool anyLeftDanger = false, anyRightDanger = false;
            Opening facing = view.Covered != Opening.None ? view.Covered : view.Facing;

            // Ding the moment any robot's parry window opens, even off-screen, so the ear learns the beat.
            foreach (var r in nm.Robots)
            {
                bool open = r.Brain.InParryWindow;
                if (open && !windowWasOpen.Contains(r)) { windowWasOpen.Add(r); if (sfx) sfx.PlayOneShot(ProceduralSfx.Ding, 0.8f); }
                else if (!open) windowWasOpen.Remove(r);
            }

            // ---- side arrows ---------------------------------------------------------------
            foreach (var kv in markers)
            {
                var opening = kv.Key;
                var m = kv.Value;
                var robot = Shotgun.RobotAt(opening);
                int d = Index(opening) - Index(facing);
                bool show = robot && (d != 0 || tabletUp);
                m.root.gameObject.SetActive(show);
                if (!show) continue;

                var st = robot.Brain.State;
                bool attacking = st is RobotState.Telegraph or RobotState.Lunge;
                Color c = attacking ? (pulse > 0.5f ? Danger : Color.white) : st == RobotState.Stunned ? StunCyan : Amber;

                int dir = d == 0 ? (Index(opening) <= 0 ? -1 : 1) : (d < 0 ? -1 : 1);
                float dist = 170f + (Mathf.Abs(d) - 1) * 90f + (attacking ? Mathf.Sin(t * 20f) * 8f : 0f);
                // Stack Front-vent markers slightly lower so they never overlap a door marker on the same side.
                float y = opening == Opening.Front ? -70f : 0f;
                m.root.anchoredPosition = new Vector2(dir * dist, y);
                m.root.localScale = Vector3.one * (attacking ? 1.3f + pulse * 0.3f : 1f + Mathf.Sin(t * 4f) * 0.06f);
                m.arrow.rectTransform.localRotation = Quaternion.Euler(0, 0, dir < 0 ? 180f : 0f);
                m.arrow2.rectTransform.localRotation = m.arrow.rectTransform.localRotation;
                m.arrow2.rectTransform.anchoredPosition = new Vector2(dir * 40f, 0);
                m.arrow2.enabled = Mathf.Abs(d) >= 2;   // double arrow = it's behind you, two turns away
                m.arrow.color = m.arrow2.color = c;
                m.label.color = c;
                m.label.text = opening switch { Opening.Left => "LEFT", Opening.Right => "RIGHT", _ => "VENT" } + (attacking ? "!" : "");

                if (attacking) { if (dir < 0) anyLeftDanger = true; else anyRightDanger = true; }
            }

            edgeLeft.enabled = anyLeftDanger;
            edgeRight.enabled = anyRightDanger;
            edgeLeft.color = edgeRight.color = new Color(1f, 0f, 0f, 0.25f + pulse * 0.25f);
            edgeLabel.text = (anyLeftDanger || anyRightDanger) && !tabletUp ? (anyLeftDanger ? "◄ TURN" : "TURN ►") : "";
            edgeLabel.color = Danger;

            // ---- the robot in front of you ------------------------------------------------
            var front = tabletUp ? null : Shotgun.RobotAt(view.Covered);
            foreach (var b in brackets) b.enabled = front;
            outerRing.enabled = targetRing.enabled = burstHint.enabled = false;
            centerLabel.text = "";
            if (!front) return;

            var s = front.Brain.State;
            float cue = front.Brain.ParryCue;
            bool window = front.Brain.InParryWindow;
            Color bc;
            float spread;

            if (s == RobotState.Stunned)
            {
                bc = StunCyan;
                spread = 30f;
                centerLabel.text = "SHOOT! x3";
                centerLabel.color = StunCyan;
            }
            else if (window)
            {
                bc = ParryGreen;
                spread = 34f + pulse * 6f;
                outerRing.enabled = targetRing.enabled = burstHint.enabled = true;
                outerRing.rectTransform.sizeDelta = targetRing.rectTransform.sizeDelta = Vector2.one * (70f + pulse * 10f);
                outerRing.color = targetRing.color = ParryGreen;
                burstHint.color = new Color(ParryGreen.r, ParryGreen.g, ParryGreen.b, 0.8f);
                burstHint.rectTransform.localRotation = Quaternion.Euler(0, 0, t * 200f);
                burstHint.rectTransform.sizeDelta = Vector2.one * (140f + pulse * 30f);
                centerLabel.text = "PARRY NOW!  [RMB]";
                centerLabel.color = ParryGreen;
            }
            else if (cue >= 0f)
            {
                // Shrinking ring: lands on the target ring the instant the window opens.
                bc = pulse > 0.5f ? Danger : Color.white;
                spread = 40f - cue * 8f;
                outerRing.enabled = targetRing.enabled = true;
                targetRing.rectTransform.sizeDelta = Vector2.one * 70f;
                targetRing.color = new Color(1f, 1f, 1f, 0.6f);
                float size = Mathf.Lerp(380f, 70f, cue * cue * (3f - 2f * cue));
                outerRing.rectTransform.sizeDelta = Vector2.one * size;
                outerRing.color = Color.Lerp(Danger, Color.white, cue);
                centerLabel.text = gun && gun.ParryArmed ? "READY..." : "GET READY TO PARRY";
                centerLabel.color = Color.Lerp(Danger, Color.white, cue);
            }
            else
            {
                bc = Amber;
                spread = 46f + Mathf.Sin(t * 3f) * 3f;
                centerLabel.text = "SHOOT TO DRIVE IT BACK";
                centerLabel.color = Amber;
            }

            for (int i = 0; i < 4; i++)
            {
                brackets[i].color = bc;
                float a = (135f - 90f * i) * Mathf.Deg2Rad;
                brackets[i].rectTransform.anchoredPosition = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * spread;
            }
        }

        // ---- construction helpers ---------------------------------------------------------

        static Image Img(Transform parent, string name, Sprite sprite, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            ((RectTransform)go.transform).sizeDelta = size;
            return img;
        }

        static Text Label(Transform parent, string name, Font font, int size, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Outline));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            go.GetComponent<Outline>().effectColor = new Color(0, 0, 0, 0.9f);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(600, 40);
            rt.anchoredPosition = pos;
            return t;
        }

        static Image EdgeGlow(Transform parent, string name, bool left)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = UiSprites.SoftEdge;
            img.raycastTarget = false;
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(left ? 0f : 1f, 0f);
            rt.anchorMax = new Vector2(left ? 0f : 1f, 1f);
            rt.pivot = new Vector2(0f, 0.5f);   // mirrored by scale on the right, so it grows inward
            rt.sizeDelta = new Vector2(260, 0);
            rt.localScale = new Vector3(left ? 1f : -1f, 1f, 1f);
            return img;
        }
    }
}
