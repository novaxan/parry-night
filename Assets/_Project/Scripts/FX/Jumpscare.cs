using System;
using System.Collections;
using UnityEngine;

namespace NightShift
{
    /// <summary>Puts the attacking robot in the player's face, shakes and flickers, then reports done.</summary>
    public class Jumpscare : MonoBehaviour
    {
        [SerializeField] OfficeView view;
        [SerializeField] AudioSource sfx;
        [SerializeField] Light[] flickerLights;
        [SerializeField] float duration = 1.4f;

        public event Action Started;

        public void Play(Robot robot, Action done) => StartCoroutine(Run(robot, done));

        IEnumerator Run(Robot robot, Action done)
        {
            if (CameraTablet.Instance) CameraTablet.Instance.SetUp(false);
            view.Locked = true;
            robot.BeginJumpscare(view.transform);
            view.LookAtInstant(robot.EyePosition);
            if (sfx) sfx.PlayOneShot(ProceduralSfx.Scream, 1f);
            Started?.Invoke();

            float end = Time.unscaledTime + duration;
            while (Time.unscaledTime < end)
            {
                view.Shake(2.5f);
                foreach (var l in flickerLights) if (l) l.enabled = UnityEngine.Random.value > 0.4f;
                yield return null;
            }
            foreach (var l in flickerLights) if (l) l.enabled = false;
            done?.Invoke();
        }
    }
}
