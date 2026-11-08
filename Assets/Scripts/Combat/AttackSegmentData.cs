using UnityEngine;

namespace MIeMie.Combat
{
    public enum WeaponSwipeKind
    {
        PlayerSwipe0,
        PlayerSwipeAxe,
        PlayerSwipeBlunder,
        PlayerSwipeChainSemicircleLeft,
        PlayerSwipeChainFinal
    }

    [CreateAssetMenu(fileName = "AttackSegment", menuName = "MIeMie/Combat/Attack Segment Data")]
    public sealed class AttackSegmentData : ScriptableObject
    {
        [Header("身份与动画")]
        [Tooltip("该段在 Inspector 和调试 HUD 中显示的中文名称。")]
        public string displayName;
        [Tooltip("原版 player-main Spine 中该段使用的动画名称。")]
        public string animationName;

        [Header("命中参数（原版 WeaponData.Combo）")]
        [Tooltip("该段的序列化基础伤害；尚未包含等级、Attachment、暴击和运行时倍率。")]
        public float damage;
        [Tooltip("该段攻击范围；通用 Swipe 同时把它作为玩家前方中心偏移和圆形半径。")]
        public float rangeRadius;
        [Tooltip("该段命中时传给原版 CameraManager 的震屏强度，运行时还会乘全局 EZ Modifier。")]
        public float cameraShake;
        [Tooltip("原版 WeaponCombos.HitKnockback。仅当目标 Health.DontCombo=true 时，用于取消命中者的冲刺并让命中者自身反冲；不会乘到敌人的受击击退力上。")]
        public float hitKnockback;
        [Tooltip("原版 WeaponData.Combo.AttackType 整数值，用于区分普通、重击、枪械等伤害类型。")]
        public int attackType;
        [Tooltip("该段实际引用的原版 Swipe 类型。专用武器运行时可能不会走通用圆形 Swipe。")]
        public WeaponSwipeKind swipeKind;
        [Tooltip("该段所用 Swipe 碰撞体保持激活的时间，单位秒。PlayerSwipe0 原版值：0.1；专用 Swipe 必须读取自身证据。")]
        public float hitboxDuration = 0.1f;

        [Header("逐段手感修正（MIeMie，非原版 Combo 字段）")]
        [Tooltip("乘在受击方自身物理击退结果上的逐段承载修正。1=保持受击方原始击退，0=不击退。它不改变上方原版 HitKnockback 的攻击者反冲语义。")]
        [Min(0f)] public float targetKnockbackMultiplier = 1f;
        [Tooltip("乘在受击方 UnitObject/Health 受击震屏上的逐段修正。1=保持受击方震屏，0=关闭该层；不会修改该段自身 Camera Shake。")]
        [Min(0f)] public float targetReactionShakeMultiplier = 1f;
        [Tooltip("乘在受击方普通命中 HitStop 上的逐段修正。1=保持受击方原始时长，0=关闭普通命中顿帧。")]
        [Min(0f)] public float normalHitStopMultiplier = 1f;
        [Tooltip("乘在受击方致死命中 HitStop 上的逐段修正。1=保持受击方原始时长，0=关闭致死顿帧。")]
        [Min(0f)] public float deathHitStopMultiplier = 1f;

        [Header("连招与方向")]
        [Tooltip("原版该段是否允许缓存下一段普通攻击输入。")]
        public bool canQueueNextAttack;
        [Tooltip("该段在 Update Angle 事件之前是否允许鼠标更新攻击朝向。")]
        public bool canChangeDirectionDuringAttack;
        [Tooltip("该段是否允许在攻击过程中持续自由改变方向；Dagger 前四段和 Gauntlets 使用。")]
        public bool canFreelyChangeDirection;
        [Tooltip("该段是否显示方向指示器；原版 Hammer_Default 为 true。")]
        public bool showDirectionIndicator;

        [Header("攻击冲刺与后坐力")]
        [Tooltip("该段传给 PlayerController.Lunge 的原始速度；运行时还会乘全局 Lunge Dampener。")]
        public float lungeSpeed;
        [Tooltip("该段冲刺速度下降过程的持续时间，单位秒。")]
        public float lungeDuration;
        [Tooltip("枪械后坐力持续时间，单位秒；近战通常为 0。")]
        public float recoilDuration;
        [Tooltip("枪械后坐力强度倍率；近战通常为 0。")]
        public float recoilPowerMultiplier;

        [Header("Blunderbuss 专用")]
        [Tooltip("该段一次射击生成的弹丸数量。原版 Default 为 3。")]
        public int blunderbussBulletCount = 3;
        [Tooltip("该段序列化的装填速度。原版 Default 为 0.8。")]
        public float blunderbussReloadSpeed = 0.8f;

        [Header("Chain 专用椭圆轨迹")]
        [Tooltip("Chain Swipe 的起始角度，单位度。")]
        public float chainStartAngle;
        [Tooltip("Chain Swipe 在一次攻击中移动的角度，单位度。")]
        public float chainAngleToMove;
        [Tooltip("Chain Swipe 沿椭圆轨迹移动的持续时间，单位秒。")]
        public float chainDuration;
        [Tooltip("Chain 椭圆轨迹 X 半径。")]
        public float chainEllipseRadiusX;
        [Tooltip("Chain 椭圆轨迹 Y 半径。")]
        public float chainEllipseRadiusY;
        [Tooltip("Chain 碰撞体启用前的延迟，单位秒。")]
        public float chainColliderEnabledDelay;
        [Tooltip("Chain 碰撞体关闭前的延迟，单位秒。")]
        public float chainColliderDisabledDelay = float.MaxValue;
        [Tooltip("Chain 轨迹相对玩家的偏移倍率。")]
        public float chainOffsetMultiplier;
    }
}
