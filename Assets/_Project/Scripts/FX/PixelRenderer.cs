using UnityEngine;
using UnityEngine.UI;

namespace NightShift
{
    /// <summary>
    /// Retro low-res look: the main camera renders into a small point-filtered RenderTexture, which is
    /// stretched over the screen behind the HUD. The HUD stays crisp; the 3D world is chunky pixels.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class PixelRenderer : MonoBehaviour
    {
        [Tooltip("Vertical resolution of the game world. 200-270 reads as early-3D; lower is chunkier.")]
        [Range(120, 720)] public int verticalResolution = 225;

        Camera cam;
        Camera output;
        RenderTexture rt;
        RawImage screenImage;
        int lastW, lastH, lastRes;

        void Awake()
        {
            cam = GetComponent<Camera>();

            // A camera that renders nothing, so the screen gets cleared each frame.
            var outGo = new GameObject("Screen Output", typeof(Camera));
            outGo.transform.SetParent(transform, false);
            output = outGo.GetComponent<Camera>();
            output.cullingMask = 0;
            output.clearFlags = CameraClearFlags.SolidColor;
            output.backgroundColor = Color.black;
            output.depth = cam.depth + 1;

            var canvasGo = new GameObject("Pixel Screen", typeof(Canvas));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = -100;   // behind the HUD
            var imgGo = new GameObject("World", typeof(RectTransform), typeof(RawImage));
            imgGo.transform.SetParent(canvasGo.transform, false);
            screenImage = imgGo.GetComponent<RawImage>();
            screenImage.raycastTarget = false;
            var r = (RectTransform)imgGo.transform;
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;

            Rebuild();
        }

        void Update()
        {
            if (Screen.width != lastW || Screen.height != lastH || verticalResolution != lastRes) Rebuild();
        }

        void Rebuild()
        {
            lastW = Screen.width;
            lastH = Screen.height;
            lastRes = verticalResolution;
            int h = Mathf.Max(64, verticalResolution);
            int w = Mathf.Max(64, Mathf.RoundToInt(h * (lastW / (float)Mathf.Max(1, lastH))));

            if (rt) { cam.targetTexture = null; rt.Release(); Destroy(rt); }
            rt = new RenderTexture(w, h, 24) { filterMode = FilterMode.Point, name = "PixelWorld" };
            rt.Create();
            cam.targetTexture = rt;
            screenImage.texture = rt;
        }

        void OnDestroy()
        {
            if (cam) cam.targetTexture = null;
            if (rt) rt.Release();
        }
    }
}
