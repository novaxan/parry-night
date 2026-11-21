using NightShift;
using UnityEngine;
using System.Linq;
// Usage: set HOLD robots at openings (re-placing resets their lurk timer so they never attack).
var nm = NightManager.Instance;
if (nm.State == GameState.Title) nm.BeginNight();
foreach (var r in nm.Robots) r.Brain.Place(0);
var tusk = nm.Robots.First(r => r.Data.displayName == "Tusk");
var ribbon = nm.Robots.First(r => r.Data.displayName == "Ribbon");
tusk.Brain.Place(tusk.Brain.OpeningIndex);
ribbon.Brain.Place(ribbon.Brain.OpeningIndex);
return nm.State.ToString();
