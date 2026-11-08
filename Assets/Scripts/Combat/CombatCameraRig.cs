using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MIeMie.Combat
{
    [RequireComponent(typeof(Camera))]
    public sealed class CombatCameraRig : MonoBehaviour
    {
        public static CombatCameraRig Instance { get; private set; }

        [SerializeField] private Transform target;

        private Vector3 currentPosition;
        private Vector2 lastFacing = Vector2.down;
        private PlayerMovementProof player;
        private CombatCameraSettings settings;
        private Transform shakeRoot;
        private float previousEzShakeIntensity;
        private float previousEzShakeTimestamp;
        private readonly List<EzShakeInstance> shakeInstances = new List<EzShakeInstance>();

        private sealed class EzShakeInstance
        {
            private readonly float magnitude;
            private readonly float roughness;
            private readonly float fadeInDuration;
            private readonly float fadeOutDuration;
            private bool sustain;
            private float currentFadeTime;
            private float tick;

            public bool IsInactive => currentFadeTime <= 0f && !sustain;

            public EzShakeInstance(float magnitudeValue, float roughnessValue, float fadeInTime, float fadeOutTime)
            {
                magnitude = magnitudeValue;
                roughness = roughnessValue;
                fadeInDuration = fadeInTime;
                fadeOutDuration = fadeOutTime;
                sustain = fadeInTime > 0f;
                currentFadeTime = sustain ? 0f : 1f;
                tick = Random.Range(-100, 100);
            }

            public Vector3 UpdateShake()
            {
                Vector3 amount = new Vector3(
                    Mathf.PerlinNoise(tick, 0f) - 0.5f,
                    Mathf.PerlinNoise(0f, tick) - 0.5f,
                    Mathf.PerlinNoise(tick, tick) - 0.5f);

                if (fadeInDuration > 0f && sustain)
                {
                    if (currentFadeTime < 1f)
                        currentFadeTime += Time.deltaTime / fadeInDuration;
                    else if (fadeOutDuration > 0f)
                        sustain = false;
                }
                if (!sustain)
                    currentFadeTime -= Time.deltaTime / fadeOutDuration;

                tick += sustain
                    ? Time.deltaTime * roughness
                    : Time.deltaTime * roughness * currentFadeTime;
                return amount * magnitude * currentFadeTime;
            }
        }

        private void Awake()
        {
            Instance = this;
            settings = CombatCameraSettings.Instance;
            currentPosition = transform.position;
            GameObject shakeRootObject = new GameObject("CameraManager - EZCameraShake");
            shakeRoot = shakeRootObject.transform;
            shakeRoot.position = Vector3.zero;
            shakeRoot.rotation = Quaternion.identity;
            transform.SetParent(shakeRoot, true);
            transform.localRotation = Quaternion.Euler(settings.angle, 0f, 0f);
            if (target != null)
                player = target.GetComponent<PlayerMovementProof>();
        }

        public void Bind(Transform value)
        {
            target = value;
            player = value != null ? value.GetComponent<PlayerMovementProof>() : null;
        }

        private void LateUpdate()
        {
            Vector3 positionShake;
            Vector3 rotationShake;
            UpdateEzShake(out positionShake, out rotationShake);

            if (target != null)
            {
                if (player != null && player.Velocity.sqrMagnitude > 0.01f)
                    lastFacing = player.Velocity.normalized;

                float radians = settings.angle * Mathf.Deg2Rad;
                Vector3 boom = new Vector3(0f,
                    settings.targetDistance * Mathf.Sin(radians),
                    -settings.targetDistance * Mathf.Cos(radians));
                Vector3 look = new Vector3(lastFacing.x, lastFacing.y, 0f) * settings.lookDistance;
                Vector3 desired = target.position + boom + look;
                currentPosition = Vector3.Lerp(currentPosition, desired,
                    settings.moveSpeed * Time.unscaledDeltaTime);
            }

            // Original hierarchy: CameraShaker updates CameraManager in Update, then CameraFollowTarget
            // writes the child Camera world position in LateUpdate. Preserve that ordering and hierarchy.
            shakeRoot.localPosition = positionShake;
            shakeRoot.localEulerAngles = rotationShake;
            transform.position = currentPosition;
            transform.localRotation = Quaternion.Euler(settings.angle, 0f, 0f);
        }

        public void Shake(float intensity, bool stackShakes = true)
        {
            if (intensity <= 0f)
                return;
            if (previousEzShakeIntensity > 0f && Time.time - previousEzShakeTimestamp >= settings.ezFadeOut)
                previousEzShakeIntensity = 0f;
            if (!stackShakes && intensity <= previousEzShakeIntensity)
                return;

            previousEzShakeIntensity = intensity;
            previousEzShakeTimestamp = Time.time;
            float magnitude = intensity * settings.ezModifier * settings.accessibilityScreenShake;
            shakeInstances.Add(new EzShakeInstance(magnitude, settings.ezRoughness,
                settings.ezFadeIn, settings.ezFadeOut));
        }

        public void ShakeForDuration(float intensityMin, float intensityMax, float duration, bool stackShakes)
        {
            StartCoroutine(ShakeForDurationRoutine(intensityMin, intensityMax, duration, stackShakes));
        }

        private IEnumerator ShakeForDurationRoutine(float intensityMin, float intensityMax, float duration, bool stackShakes)
        {
            while ((duration -= Time.deltaTime) > 0f)
            {
                if (Time.deltaTime > 0f)
                    Shake(Random.Range(intensityMin, intensityMax), stackShakes);
                yield return null;
            }
        }

        private void UpdateEzShake(out Vector3 positionShake, out Vector3 rotationShake)
        {
            positionShake = Vector3.zero;
            rotationShake = Vector3.zero;
            Vector3 positionInfluence = Vector3.one * settings.ezPositionInfluence;
            Vector3 rotationInfluence = Vector3.one * settings.ezRotationInfluence;

            for (int index = 0; index < shakeInstances.Count; index++)
            {
                EzShakeInstance shake = shakeInstances[index];
                if (shake.IsInactive)
                {
                    shakeInstances.RemoveAt(index);
                    index--;
                }
                else
                {
                    positionShake += Vector3.Scale(shake.UpdateShake(), positionInfluence);
                    // Original CameraShaker.Update calls UpdateShake again for rotation.
                    rotationShake += Vector3.Scale(shake.UpdateShake(), rotationInfluence);
                }
            }
        }
    }
}
