using NightShift;
using UnityEngine;
using System.Linq;
var nm = NightManager.Instance;
var ribbon = nm.Robots.First(r => r.Data.displayName == "Ribbon");
ribbon.Brain.Place(ribbon.Brain.OpeningIndex);
float u = Time.unscaledTime + 0.5f;
await Wait.Until(() => Time.unscaledTime > u);
var ti = Object.FindFirstObjectByType<ThreatIndicator>();
var sb = new System.Text.StringBuilder($"state={nm.State} ribbon={ribbon.Brain.State} at={Shotgun.RobotAt(Opening.Right)} ti={ti} ");
var tiRoot = Resources.FindObjectsOfTypeAll<RectTransform>().FirstOrDefault(r => r.name == "Threat Indicator"); sb.Append($"root={tiRoot} activeInH={tiRoot?.gameObject.activeInHierarchy} "); if (tiRoot) foreach (Transform t in tiRoot)
  if (t.name.StartsWith("Marker")) sb.Append($"| {t.name} active={t.gameObject.activeInHierarchy} pos={((RectTransform)t).anchoredPosition} ");
ScreenCapture.CaptureScreenshot("Screenshots/v2_09_arrow_dbg.png");
return sb.ToString();
