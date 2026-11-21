using NightShift;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
var nm = NightManager.Instance;
nm.BeginNight();
foreach (var r in nm.Robots) r.Brain.Place(0);
var tusk = nm.Robots.First(r => r.Data.displayName == "Tusk");
var ribbon = nm.Robots.First(r => r.Data.displayName == "Ribbon");
tusk.Brain.Place(tusk.Brain.OpeningIndex);
ribbon.Brain.Place(ribbon.Brain.OpeningIndex);
float u = Time.unscaledTime + 0.5f;
await Wait.Until(() => Time.unscaledTime > u);
ScreenCapture.CaptureScreenshot("Screenshots/v2_09_arrows.png");
var sb = new System.Text.StringBuilder();
var tiRoot = Resources.FindObjectsOfTypeAll<RectTransform>().First(r => r.name == "Threat Indicator");
foreach (Transform t in tiRoot)
  if (t.name.StartsWith("Marker")) {
    var img = t.Find("Arrow").GetComponent<Image>();
    sb.Append($"| {t.name} act={t.gameObject.activeInHierarchy} pos={((RectTransform)t).anchoredPosition} scale={t.localScale} col={img.color} sprite={img.sprite} ");
  }
// keep them from attacking while we look
foreach (var r in nm.Robots) r.Brain.Place(0);
return sb.ToString();
