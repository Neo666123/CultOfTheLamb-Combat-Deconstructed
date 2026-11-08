using UnityEngine;
using UnityEngine.Serialization;

namespace MIeMie.Combat
{
    public enum PlayerWeaponFamily
    {
        Sword,
        Axe,
        Hammer,
        Dagger,
        Gauntlets,
        Blunderbuss,
        Shield,
        Chain
    }

    public enum NormalAttackStrategy
    {
        GenericSwipe,
        HammerHoldRelease,
        Blunderbuss,
        Shield,
        Chain
    }

    [CreateAssetMenu(fileName = "WeaponDefinition", menuName = "MIeMie/Combat/Weapon Definition")]
    public sealed class WeaponDefinitionSO : ScriptableObject
    {
        [Header("原版身份")]
        [Tooltip("原版 WeaponData 的精确资产名称，例如 Sword_Default。")]
        public string originalName;
        [Tooltip("游戏内调试 HUD 使用的中文名称。")]
        public string displayName;
        [Tooltip("原版主武器家族。")]
        public PlayerWeaponFamily family;
        [Tooltip("原版 EquipmentType 的整数值。")]
        public int equipmentType;
        [Tooltip("点击切换武器按钮时的固定排列顺序。")]
        public int switchOrder;

        [Header("武器级手感参数")]
        [FormerlySerializedAs("speed")]
        [Tooltip("原版 WeaponData.Speed 的展示属性。已确认它只经 GetWeaponSpeed 被拾取提示和武器信息 UI 读取，不参与普通攻击动画播放。")]
        public float displaySpeedStat = 1f;
        [Tooltip("普通攻击实际写入 SkeletonAnimation.timeScale 的基础 AttackRateMultiplier。原版无附件 Default 武器基线均为 1。")]
        public float attackRateMultiplier = 1f;
        [Tooltip("该武器是否允许在 AttackState.Begin 阶段被普通闪避取消。Chain_Default 为 false，其余默认武器为 true。")]
        public bool canBreakDodge = true;
        [Tooltip("该武器普通攻击使用的原版运行分支。不同策略不能用统一圆形 Swipe 偷偷代替。")]
        public NormalAttackStrategy normalAttackStrategy;

        [Header("普通攻击段")]
        [Tooltip("按原版顺序引用每一个独立 AttackSegmentData；段数和参数均属于当前武器。")]
        public AttackSegmentData[] combos;
    }
}
