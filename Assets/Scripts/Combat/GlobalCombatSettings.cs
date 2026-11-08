using UnityEngine;

namespace MIeMie.Combat
{
    [CreateAssetMenu(fileName = "GlobalCombatSettings", menuName = "MIeMie/Combat/Global Combat Settings")]
    public sealed class GlobalCombatSettings : ScriptableObject
    {
        private const string ResourcePath = "Settings/GlobalCombatSettings";
        private static GlobalCombatSettings instance;

        [Header("玩家移动（原版全局基线）")]
        [Tooltip("玩家正常跑动的最大速度，单位为 Unity 单位/秒。原版运行时基线：5.5。")]
        public float runSpeed = 5.5f;
        [Tooltip("摇杆或键盘输入达到该绝对值后才进入移动状态，用于过滤微小输入。原版值：0.3。")]
        public float minimumInput = 0.3f;
        [Tooltip("移动速度逼近最大跑速时的除数。原版公式：(目标速度-当前速度)/3×DeltaTime60；值越大，加速越慢。原版值：3。")]
        public float accelerationDivisor = 3f;
        [Tooltip("停止输入后速度逼近 0 时的除数。值越大，滑行减速越慢。原版 Idle 值：3。")]
        public float idleDecelerationDivisor = 3f;

        [Header("玩家闪避（原版全局基线）")]
        [Tooltip("普通闪避稳定阶段的目标移动速度，单位为 Unity 单位/秒。原版 Runtime Player Prefab：12。")]
        public float dodgeSpeed = 12f;
        [Tooltip("进入普通闪避瞬间对 Dodge Speed 使用的起步倍率。原版值：1.2，因此基础起步速度为 14.4。")]
        public float dodgeEntrySpeedMultiplier = 1.2f;
        [Tooltip("松开空格后仍必须维持闪避的最短时间，单位秒。原版 Runtime Player Prefab：0.27。")]
        public float dodgeMinimumDuration = 0.27f;
        [Tooltip("持续按住空格时一次闪避允许维持的最长时间，单位秒。原版 Runtime Player Prefab：0.35。")]
        public float dodgeMaximumDuration = 0.35f;
        [Tooltip("成功进入闪避后写入的冷却时间，单位秒。原版值：0.35；Dodging 期间不递减，离开后才开始递减。")]
        public float dodgeCooldown = 0.35f;
        [Tooltip("闪避开始后仍允许 WASD 修正方向的时间窗口，单位秒。原版代码固定为前 0.1 秒。")]
        public float dodgeSteerWindow = 0.1f;
        [Tooltip("闪避起步速度向 Dodge Speed 回落时 Mathf.Lerp 的时间系数。原版公式：Lerp(speed, DodgeSpeed, 2×deltaTime)。")]
        public float dodgeSpeedLerpRate = 2f;
        [Tooltip("基础 Run 的闪避距离倍率。原版 DataManager 默认值：1；不包含 Tarot/Trinket 的额外增量。")]
        public float dodgeDistanceMultiplier = 1f;
        [Tooltip("从 Dodging 状态直接发动普通攻击时，对该攻击段 Lunge Speed 与 Lunge Duration 同时使用的倍率。原版值：1.25。")]
        public float dodgeAttackLungeMultiplier = 1.25f;
        [Header("PlayerWeapon / PlayerController 共享规则")]
        [Tooltip("所有普通武器开始允许记录队列输入前的共享阈值。原版 DoAttackRoutine 固定值：0.1；每段是否可排队仍由 AttackSegmentData.CanQueueNextAttack 决定。")]
        public float inputQueueDelay = 0.1f;
        [Tooltip("所有普通武器离开攻击状态后重置 Combo 的共享等待时间。原版 PlayerWeapon.ResetCombo：0.2；真正可衔接时刻由各段 Spine Event 决定。")]
        public float comboResetDelay = 0.2f;
        [Tooltip("PlayerController 对所有武器 Lunge 使用的玩家级衰减倍率。实际速度=AttackSegmentData.LungeSpeed×该值；原版 Player Prefab：0.5。")]
        public float lungeDampener = 0.5f;
        [InspectorName("冲刺继承移动速度")]
        [Tooltip("PlayerController 的共享 Lunge 算法开关。开启时不会先清零当前 speed；关闭属于非原版调校模式，不是单把武器参数。")]
        public bool inheritMovementSpeedOnLunge = true;

        public static GlobalCombatSettings Instance
        {
            get
            {
                if (instance == null)
                    instance = Resources.Load<GlobalCombatSettings>(ResourcePath);
                if (instance == null)
                {
                    instance = CreateInstance<GlobalCombatSettings>();
                    Debug.LogWarning("[MIeMie] GlobalCombatSettings.asset is missing; runtime defaults are in use.");
                }
                return instance;
            }
        }

    }
}
