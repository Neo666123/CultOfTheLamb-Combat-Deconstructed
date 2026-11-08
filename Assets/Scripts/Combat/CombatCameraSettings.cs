using UnityEngine;

namespace MIeMie.Combat
{
    [CreateAssetMenu(fileName = "CombatCameraSettings", menuName = "MIeMie/Combat/Combat Camera Settings")]
    public sealed class CombatCameraSettings : ScriptableObject
    {
        private const string ResourcePath = "Settings/CombatCameraSettings";
        private static CombatCameraSettings instance;

        [Header("战斗相机跟随")]
        [Tooltip("相机跟随目标的基础距离。普通 Dungeon1 原版值：12。")]
        public float targetDistance = 12f;
        [Tooltip("相机围绕 X 轴的俯视角，单位度。普通 Dungeon1 原版值：-45。")]
        public float angle = -45f;
        [Tooltip("相机沿玩家最近朝向产生的前视偏移。普通 Dungeon1 原版值：0.5。")]
        public float lookDistance = 0.5f;
        [Tooltip("相机当前位置向目标位置插值的速度。普通 Dungeon1 原版值：5。")]
        public float moveSpeed = 5f;

        [Header("EZCameraShake 算法与全局输出")]
        [Tooltip("无障碍设置提供的最终震屏倍率。它统一作用于所有震屏来源，不代表各武器自身强度。当前用户调校值：2.62。")]
        [Range(0f, 5f)] public float accessibilityScreenShake = 2.62f;
        [Tooltip("CameraManager 对调用方传入强度的统一倍率。原版 Dungeon1 CameraManager：3。各攻击段自身强度位于 AttackSegmentData.cameraShake。")]
        public float ezModifier = 3f;
        [Tooltip("EZCameraShake PerlinNoise tick 的粗糙度。原版值：4。")]
        public float ezRoughness = 4f;
        [Tooltip("一次 EZ Shake 淡入完整强度的时间，单位秒。原版值：0.1。")]
        public float ezFadeIn = 0.1f;
        [Tooltip("一次 EZ Shake 淡出停止的时间，单位秒。原版值：0.4。")]
        public float ezFadeOut = 0.4f;
        [Tooltip("Perlin Shake 对 CameraManager 局部位置三个轴的统一影响倍率。原版值：0.35。")]
        public float ezPositionInfluence = 0.35f;
        [Tooltip("Perlin Shake 对 CameraManager 局部旋转三个轴的统一影响倍率。原版值：0.35。")]
        public float ezRotationInfluence = 0.35f;

        public static CombatCameraSettings Instance
        {
            get
            {
                if (instance == null)
                    instance = Resources.Load<CombatCameraSettings>(ResourcePath);
                if (instance == null)
                {
                    instance = CreateInstance<CombatCameraSettings>();
                    Debug.LogWarning("[MIeMie] CombatCameraSettings.asset is missing; runtime defaults are in use.");
                }
                return instance;
            }
        }
    }
}
