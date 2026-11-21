using NightShift;
using UnityEngine;
using System.Linq;
using System.Reflection;
var nm = NightManager.Instance;
var gun = Object.FindFirstObjectByType<Shotgun>();
var tusk = nm.Robots.First(r => r.Data.displayName == "Tusk");
typeof(Shotgun).GetMethod("Fire", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(gun, null);
return $"after shot tusk={tusk.Brain.State} hp={tusk.Brain.Health} loaded={gun.Model.Loaded} kills={nm.Kills}";
