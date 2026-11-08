using System;
using System.Reflection;
using UnityEngine;

namespace MIeMie.Combat
{
    [DisallowMultipleComponent]
    public sealed class OriginalPlayerSpineView : MonoBehaviour
    {
        [SerializeField] private Component skeletonAnimation;
        [SerializeField] private string skinName = "Lamb_0";

        private object animationState;
        private object skeleton;
        private string currentAnimation;

        public bool IsReady => skeletonAnimation != null && animationState != null && skeleton != null;

        private void Awake()
        {
            ResolveSpineObjects();
            ApplySkin();
            SetLocomotion(false, 270f, 0f, 5.5f);
        }

        public void Bind(Component component)
        {
            skeletonAnimation = component;
            ResolveSpineObjects();
            ApplySkin();
        }

        public void SetLocomotion(bool moving, float facingAngle, float speed, float runSpeed)
        {
            if (!IsReady)
                ResolveSpineObjects();
            if (!IsReady)
                return;

            string animationName = moving
                ? SelectMovingAnimation(facingAngle)
                : SelectIdleAnimation(facingAngle);

            SetAnimation(animationName, true);
            SetSkeletonScaleX((facingAngle > 90f && facingAngle < 270f) ? 1f : -1f);
            SetAnimationTimeScale(moving ? Mathf.Clamp01(speed / Mathf.Max(0.001f, runSpeed)) : 1f);
        }

        public void SetAttackAnimation(string animationName, float animationSpeed = 1f)
        {
            if (!IsReady)
                ResolveSpineObjects();
            SetAnimation(animationName, false);
            SetAnimationTimeScale(Mathf.Max(0.01f, animationSpeed));
        }

        public void SetDodgeAnimation(float facingAngle)
        {
            if (!IsReady)
                ResolveSpineObjects();
            if (!IsReady)
                return;

            string animationName;
            if (facingAngle > 40f && facingAngle < 140f)
                animationName = "roll-up";
            else if (facingAngle > 220f && facingAngle < 320f)
                animationName = "roll-down";
            else
                animationName = "roll";

            SetAnimation(animationName, true);
            SetFacingVisual(facingAngle);
            SetAnimationTimeScale(1f);
        }

        public void SetFacingVisual(float facingAngle)
        {
            if (!IsReady)
                ResolveSpineObjects();
            if (IsReady)
                SetSkeletonScaleX((facingAngle > 90f && facingAngle < 270f) ? 1f : -1f);
        }

        private static string SelectIdleAnimation(float angle)
        {
            return angle > 40f && angle < 140f ? "idle-up" : "idle";
        }

        private static string SelectMovingAnimation(float angle)
        {
            if (angle > 40f && angle < 140f)
                return angle > 70f && angle < 110f ? "run-up" : "run-up-diagonal";
            if (angle > 220f && angle < 320f)
                return angle > 250f && angle < 290f ? "run-down" : "run";
            return "run-horizontal";
        }

        private void ResolveSpineObjects()
        {
            if (skeletonAnimation == null)
            {
                Component[] components = GetComponentsInChildren<Component>(true);
                foreach (Component candidate in components)
                {
                    if (candidate != null && candidate.GetType().FullName == "Spine.Unity.SkeletonAnimation")
                    {
                        skeletonAnimation = candidate;
                        break;
                    }
                }
            }

            if (skeletonAnimation == null)
                return;

            Type type = skeletonAnimation.GetType();
            animationState = type.GetProperty("AnimationState", BindingFlags.Instance | BindingFlags.Public)?.GetValue(skeletonAnimation);
            skeleton = type.GetProperty("Skeleton", BindingFlags.Instance | BindingFlags.Public)?.GetValue(skeletonAnimation);
        }

        private void ApplySkin()
        {
            if (skeleton == null)
                return;

            MethodInfo setSkin = skeleton.GetType().GetMethod("SetSkin", new[] { typeof(string) });
            MethodInfo setupPose = skeleton.GetType().GetMethod("SetSlotsToSetupPose", Type.EmptyTypes);
            setSkin?.Invoke(skeleton, new object[] { skinName });
            setupPose?.Invoke(skeleton, null);
        }

        private void SetAnimation(string animationName, bool loop)
        {
            if (animationState == null || currentAnimation == animationName)
                return;

            MethodInfo method = animationState.GetType().GetMethod("SetAnimation", new[] { typeof(int), typeof(string), typeof(bool) });
            method?.Invoke(animationState, new object[] { 0, animationName, loop });
            currentAnimation = animationName;
        }

        private void SetSkeletonScaleX(float value)
        {
            PropertyInfo property = skeleton?.GetType().GetProperty("ScaleX", BindingFlags.Instance | BindingFlags.Public);
            if (property != null && property.CanWrite)
                property.SetValue(skeleton, value);
        }

        private void SetAnimationTimeScale(float value)
        {
            PropertyInfo property = animationState?.GetType().GetProperty("TimeScale", BindingFlags.Instance | BindingFlags.Public);
            if (property != null && property.CanWrite)
                property.SetValue(animationState, value);
        }
    }
}
