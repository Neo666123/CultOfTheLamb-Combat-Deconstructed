using System.Collections;
using UnityEngine;

namespace MIeMie.Combat
{
    public sealed class CombatTime : MonoBehaviour
    {
        private static CombatTime instance;
        private Coroutine routine;

        private void Awake()
        {
            instance = this;
        }

        public static void HitStop(float realtimeDuration)
        {
            if (instance == null || realtimeDuration <= 0f)
                return;
            if (instance.routine != null)
                instance.StopCoroutine(instance.routine);
            instance.routine = instance.StartCoroutine(instance.StopRoutine(realtimeDuration));
        }

        private IEnumerator StopRoutine(float duration)
        {
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = 1f;
            routine = null;
        }

        private void OnDisable()
        {
            Time.timeScale = 1f;
        }
    }
}
