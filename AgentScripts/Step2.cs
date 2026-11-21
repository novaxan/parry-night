using NightShift;
using UnityEngine;
using System.Linq;
var nm = NightManager.Instance;
var tusk = nm.Robots.First(r => r.Data.displayName == "Tusk");
// keep the other robots home so nothing interferes
foreach (var r in nm.Robots) if (r != tusk) r.Brain.Aggression = 0;
await Wait.Until(() => tusk.Brain.State == RobotState.Telegraph && tusk.Brain.StateTimer < 0.4f);
return $"tusk={tusk.Brain.State}";
