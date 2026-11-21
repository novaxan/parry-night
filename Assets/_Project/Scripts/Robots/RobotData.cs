using UnityEngine;

namespace NightShift
{
    /// <summary>Tunable stats for one robot. Edit the assets in Assets/_Project/Data to balance the night.</summary>
    [CreateAssetMenu(menuName = "Night Shift/Robot Data", fileName = "RobotData")]
    public class RobotData : ScriptableObject
    {
        public string displayName = "Robot";
        [Tooltip("Bumped by the scene builder when its default tuning changes. Assets below the builder's version get the new defaults.")]
        [HideInInspector] public int tuningVersion;

        [Header("Health")]
        [Min(0.5f)] public float maxHealth = 2f;

        [Header("Movement")]
        [Tooltip("Seconds between movement rolls.")]
        [Min(0.5f)] public float moveInterval = 6f;
        [Tooltip("Chance out of 20 to advance on each roll at 12 AM.")]
        [Range(0, 20)] public int startAggression = 5;
        [Tooltip("Aggression added every in-game hour.")]
        [Range(0, 5)] public int aggressionPerHour = 2;

        [Header("Attack")]
        [Tooltip("Seconds the robot lurks in the opening before it telegraphs.")]
        public Vector2 lurkTime = new Vector2(3f, 6f);
        [Tooltip("Warning time: eyes flash red and it screeches.")]
        [Min(0.1f)] public float telegraphTime = 0.8f;
        [Tooltip("Duration of the lunge. Parry window sits at its end.")]
        [Min(0.1f)] public float lungeTime = 0.5f;
        [Tooltip("Parry succeeds only in the last N seconds of the lunge.")]
        [Min(0.05f)] public float parryWindow = 0.25f;
        [Min(0.1f)] public float stunTime = 2f;
        [Tooltip("Damage multiplier for shots that land while stunned.")]
        [Min(1f)] public float stunnedDamageMultiplier = 3f;

        [Header("Look")]
        public Color bodyColor = Color.gray;
        public Color eyeColor = Color.white;
    }
}
