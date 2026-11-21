using NightShift;
using UnityEngine;
using System.Linq;
using System.Reflection;
var log = new System.Text.StringBuilder();
var nm = NightManager.Instance;
var view = Object.FindFirstObjectByType<OfficeView>();
var gun = Object.FindFirstObjectByType<Shotgun>();
var flags = BindingFlags.NonPublic | BindingFlags.Instance;
async System.Threading.Tasks.Task Sec(float s) { float u = Time.unscaledTime + s; await Wait.Until(() => Time.unscaledTime > u); }
nm.BeginNight();
var tusk = nm.Robots.First(r => r.Data.displayName == "Tusk");
foreach (var r in nm.Robots) r.Brain.Place(0);
// 1. Tusk lurks at the left door while we face the vent: arrow on the left.
tusk.Brain.Place(tusk.Brain.OpeningIndex);
await Sec(0.6f);
ScreenCapture.CaptureScreenshot("Screenshots/v2_01_arrow_left.png");
// 2. Turn to face it: brackets lock on.
typeof(OfficeView).GetMethod("Turn", flags).Invoke(view, new object[] { -1 });
await Wait.Until(() => view.Covered == Opening.Left);
await Sec(0.4f);
ScreenCapture.CaptureScreenshot("Screenshots/v2_02_locked_on.png");
// 3. Telegraph: shrinking ring.
await Wait.Until(() => tusk.Brain.State == RobotState.Telegraph && tusk.Brain.ParryCue > 0.45f);
ScreenCapture.CaptureScreenshot("Screenshots/v2_03_ring_shrinking.png");
// 4. Window open: green ring.
await Wait.Until(() => tusk.Brain.InParryWindow);
await Sec(0.05f);
ScreenCapture.CaptureScreenshot("Screenshots/v2_04_window_green.png");
await Sec(0.05f);
log.Append($"window={tusk.Brain.InParryWindow} timer={tusk.Brain.StateTimer:F2}; ");
// 5. Parry: FX.
typeof(Shotgun).GetMethod("Parry", flags).Invoke(gun, null);
log.Append($"parried={tusk.Brain.State}; ");
await Sec(0.04f);
ScreenCapture.CaptureScreenshot("Screenshots/v2_05_parry_flash.png");
await Sec(0.16f);
ScreenCapture.CaptureScreenshot("Screenshots/v2_06_parry_slam.png");
await Sec(0.4f);
// 6. Shoot twice, then reload and capture mid-insert.
typeof(Shotgun).GetMethod("Fire", flags).Invoke(gun, null);
log.Append($"after shot={tusk.Brain.State}; ");
await Sec(0.7f);
typeof(Shotgun).GetMethod("Fire", flags).Invoke(gun, null);
await Sec(0.7f);
typeof(Shotgun).GetMethod("Reload", flags).Invoke(gun, null);
await Sec(0.35f);
ScreenCapture.CaptureScreenshot("Screenshots/v2_07_reload_rise.png");
await Sec(0.25f);
ScreenCapture.CaptureScreenshot("Screenshots/v2_08_reload_insert.png");
await Wait.Until(() => gun.Model.Current == ShotgunModel.GunState.Idle);
log.Append($"loaded={gun.Model.Loaded} reserve={gun.Model.Reserve}");
return log.ToString();
