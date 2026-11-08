using UnityEngine;

namespace MIeMie.Combat
{
    [CreateAssetMenu(fileName = "ForestScampCombatSettings", menuName = "MIeMie/Combat/Forest Scamp Settings")]
    public sealed class ForestScampCombatSettings : ScriptableObject
    {
        private const string ResourcePath = "Settings/ForestScampCombatSettings";
        private static ForestScampCombatSettings instance;

        [Header("受击方：Forest Scamp 活体响应")]
        [Tooltip("受击方 EnemyScuttleSwiper.ApplyForceRoutine 使用的原始基础力。原版类内值：1500；不随攻击武器变化。")]
        public float rawKnockback = 1500f;
        [Tooltip("受击方 Forest Scamp Prefab 自身的击退倍率。原版值：0.7；不同敌人可以不同，不随攻击武器变化。")]
        public float knockbackModifier = 0.7f;
        [Tooltip("MIeMie Rigidbody2D 尺度换算系数，不代表原版数据。当前用户调校值：0.52。")]
        [Range(0f, 1f)] public float knockbackIntegrationScale = 0.52f;
        [Tooltip("Scamp Rigidbody2D.AddForce 使用的模式。原版未显式传 mode，对应 Unity 默认 Force。")]
        public ForceMode2D knockbackForceMode = ForceMode2D.Force;
        [Tooltip("死亡身体远离攻击者时使用的原始 AddForce。原版 Dead Body - Human Grunt：1700。")]
        public float deadBodyForce = 1700f;
        [Tooltip("死亡身体 Rigidbody2D 的线性阻力。当前重建值：10。")]
        public float deadBodyDrag = 10f;
        [Tooltip("受击方 UnitObject.OnHit 的普通近战停帧，单位秒。当前用户调校值：0.08；原版代码值为 0.05。AttackType 可使某些攻击跳过 HitStop，但数值不是武器配置。")]
        public float normalHitStop = 0.08f;
        [Tooltip("Forest Scamp 死亡受击停帧时长，单位秒。当前用户调校值：0.15；原版证据值为 0.1。")]
        public float deathHitStop = 0.15f;

        [Header("受击方 Health / 视觉代理")]
        [Tooltip("目标 Health 配置的玩家命中 Impact 颜色。原版为纯红。")]
        public Color impactColor = Color.red;
        [Tooltip("目标 Health 配置的 Impact 缩放。当前用户调校值：1；原版 Forest Scamp 为 0.5。")]
        public float impactScale = 1f;
        [Tooltip("Forest Scamp 原版流血颜色证据值。")]
        public Color bloodColor = new Color(0.23f, 0.2f, 0.2f, 1f);
        [Tooltip("MIeMie 血滴视觉代理颜色，不覆盖原版 bloodColor 证据。")]
        public Color bloodProxyColor = new Color(0.62f, 0.035f, 0.025f, 1f);
        [Tooltip("MIeMie 地面血迹代理颜色。")]
        public Color groundBloodProxyColor = new Color(0.46f, 0.018f, 0.012f, 0.88f);
        [Tooltip("普通受击生成的血滴代理数量。")]
        public int normalBloodParticles = 3;
        [Tooltip("死亡时生成的血滴数量。Forest Scamp 原版值：5。")]
        public int deathBloodParticles = 5;
        [Tooltip("受击红闪保持时间，单位秒。当前用户调校值：0.15。")]
        public float hitFlashDuration = 0.15f;
        [Tooltip("Impact 视觉代理生命周期，单位秒。当前用户调校值：0.2。")]
        public float impactDuration = 0.2f;
        [Tooltip("飞溅血滴视觉代理生命周期，单位秒。当前用户调校值：0.6。")]
        public float bloodParticleDuration = 0.6f;
        [Tooltip("普通命中地面血迹代理保留时间，单位秒。")]
        public float groundBloodDuration = 4f;
        [Tooltip("死亡地面血迹代理保留时间，单位秒。")]
        public float deathGroundBloodDuration = 7f;
        [Tooltip("普通地面血迹代理尺寸，Unity 世界单位。")]
        public float normalGroundBloodSize = 0.24f;
        [Tooltip("死亡地面血迹代理尺寸，Unity 世界单位。")]
        public float deathGroundBloodSize = 0.52f;

        [Header("受击方 UnitObject / Health 震屏输入")]
        [Tooltip("受击方 UnitObject.OnHit 持续震屏每帧随机强度下限；之后由 Camera Settings 统一执行震屏算法。原版共享代码值：0.6，不是武器参数。")]
        public float unitHitShakeMin = 0.6f;
        [Tooltip("受击方 UnitObject.OnHit 持续震屏每帧随机强度上限。原版共享代码值：0.8，不是武器参数。")]
        public float unitHitShakeMax = 0.8f;
        [Tooltip("受击方 UnitObject.OnHit 持续尝试触发震屏的时间，单位秒。原版共享代码值：0.3，不是武器参数。")]
        public float unitHitShakeDuration = 0.3f;
        [Tooltip("受击方 Health 在存活命中时传给相机的强度。当前用户调校值：1；Forest Scamp 原版由死亡强度 2 / 3 得到约 0.6667。它不是武器参数。")]
        public float genericHitShake = 1f;
        [Tooltip("Forest Scamp 死亡时 Health 路径传给相机的强度。原版值：2。")]
        public float deathShake = 2f;

        public static ForestScampCombatSettings Instance
        {
            get
            {
                if (instance == null)
                    instance = Resources.Load<ForestScampCombatSettings>(ResourcePath);
                if (instance == null)
                {
                    instance = CreateInstance<ForestScampCombatSettings>();
                    Debug.LogWarning("[MIeMie] ForestScampCombatSettings.asset is missing; runtime defaults are in use.");
                }
                return instance;
            }
        }
    }
}
