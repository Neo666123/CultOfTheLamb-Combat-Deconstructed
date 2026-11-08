using System;
using Spine;
using Spine.Unity;
using UnityEngine;

namespace MIeMie.Combat
{
    [DisallowMultipleComponent]
    public sealed class PlayerSwordCombat : MonoBehaviour
    {
        [Header("武器数据")]
        [Tooltip("可选的初始武器；当前测试范围只接受 Sword/Axe/Hammer/Dagger。为空或引用范围外武器时使用 Sword_Default。")]
        [SerializeField] private WeaponDefinitionSO startingWeapon;

        private SkeletonAnimation skeletonAnimation;
        private OriginalPlayerSpineView view;
        private PlayerMovementProof movement;
        private WeaponDefinitionSO[] weapons = Array.Empty<WeaponDefinitionSO>();
        private WeaponDefinitionSO currentWeapon;
        private int currentWeaponIndex;
        private int comboIndex;
        private bool attacking;
        private bool canBreak;
        private bool queued;
        private bool angleLocked;
        private bool warnedUnsupportedSwipe;
        private float attackStarted;
        private float attackEnded;
        private float storedFacing;
        private GlobalCombatSettings settings;

        public bool IsAttacking => attacking;
        public WeaponDefinitionSO CurrentWeapon => currentWeapon;
        public string CurrentWeaponName => currentWeapon != null ? currentWeapon.originalName : "WeaponData missing";
        public string CurrentWeaponDisplayName => currentWeapon != null ? currentWeapon.displayName : "未加载";
        public int CurrentComboNumber => currentWeapon != null && currentWeapon.combos != null && currentWeapon.combos.Length > 0
            ? comboIndex + 1
            : 0;

        public string CurrentImplementationNote
        {
            get
            {
                if (currentWeapon == null)
                    return "WeaponDefinitionSO 未加载";
                switch (currentWeapon.normalAttackStrategy)
                {
                    case NormalAttackStrategy.GenericSwipe:
                        return CurrentSegment != null && CurrentSegment.swipeKind == WeaponSwipeKind.PlayerSwipe0
                            ? "通用 PlayerSwipe0 已接入"
                            : "专用 Swipe 数据已接入，命中实现待专用闭环";
                    case NormalAttackStrategy.HammerHoldRelease:
                        return "Hammer 按住/释放入口已接入";
                    case NormalAttackStrategy.Shield:
                        return "普通 Swipe 已接入；长按格挡待下一步";
                    case NormalAttackStrategy.Blunderbuss:
                        return "弹药/散射命中待专用实现";
                    case NormalAttackStrategy.Chain:
                        return "椭圆轨迹数据已接入，命中待专用实现";
                    default:
                        return string.Empty;
                }
            }
        }

        private AttackSegmentData CurrentSegment
        {
            get
            {
                if (currentWeapon == null || currentWeapon.combos == null || currentWeapon.combos.Length == 0)
                    return null;
                comboIndex = Mathf.Clamp(comboIndex, 0, currentWeapon.combos.Length - 1);
                return currentWeapon.combos[comboIndex];
            }
        }

        private void Awake()
        {
            skeletonAnimation = GetComponentInChildren<SkeletonAnimation>(true);
            view = GetComponent<OriginalPlayerSpineView>();
            movement = GetComponent<PlayerMovementProof>();
            settings = GlobalCombatSettings.Instance;
            LoadWeapons();
        }

        private void Start()
        {
            if (skeletonAnimation == null)
                return;
            skeletonAnimation.Initialize(false);
            if (skeletonAnimation.AnimationState != null)
                skeletonAnimation.AnimationState.Event += HandleSpineEvent;
        }

        private void OnDestroy()
        {
            if (skeletonAnimation != null && skeletonAnimation.AnimationState != null)
                skeletonAnimation.AnimationState.Event -= HandleSpineEvent;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
                CycleWeapon(1);

            if (currentWeapon == null || CurrentSegment == null)
                return;

            bool attackDown = Input.GetButtonDown("Fire1") || Input.GetMouseButtonDown(0);
            bool attackUp = Input.GetButtonUp("Fire1") || Input.GetMouseButtonUp(0);
            bool beginInput = currentWeapon.normalAttackStrategy == NormalAttackStrategy.HammerHoldRelease
                ? attackUp
                : attackDown;

            if (!attacking)
            {
                if (Time.time - attackEnded > settings.comboResetDelay)
                    comboIndex = 0;
                if (beginInput)
                    BeginAttack();
                return;
            }

            AttackSegmentData segment = CurrentSegment;
            if (segment == null)
                return;

            if (segment.canFreelyChangeDirection || (!angleLocked && segment.canChangeDirectionDuringAttack))
            {
                storedFacing = ResolveAttackFacing();
                movement.SetFacingAngle(storedFacing);
                view.SetFacingVisual(storedFacing);
            }

            if (canBreak)
            {
                if (beginInput || queued)
                {
                    queued = false;
                    BeginAttack();
                }
                else if (movement.RawInput.magnitude >= settings.minimumInput)
                {
                    FinishAttack();
                }
            }
            else if (attackDown && segment.canQueueNextAttack &&
                     Time.unscaledTime - attackStarted > settings.inputQueueDelay)
            {
                queued = true;
            }
        }

        public void CycleWeapon(int direction)
        {
            if (weapons == null || weapons.Length == 0)
                return;
            int next = (currentWeaponIndex + direction) % weapons.Length;
            if (next < 0)
                next += weapons.Length;
            SetWeapon(weapons[next]);
        }

        public void SetWeapon(WeaponDefinitionSO weapon)
        {
            if (weapon == null)
                return;
            if (attacking)
                FinishAttack();
            currentWeapon = weapon;
            currentWeaponIndex = Array.IndexOf(weapons, weapon);
            if (currentWeaponIndex < 0)
                currentWeaponIndex = 0;
            comboIndex = 0;
            queued = false;
            canBreak = false;
            angleLocked = false;
            warnedUnsupportedSwipe = false;
            view?.SetLocomotion(movement.RawInput.magnitude >= settings.minimumInput,
                movement.FacingAngle, movement.CurrentSpeed, settings.runSpeed);
        }

        private void LoadWeapons()
        {
            WeaponDefinitionSO[] allWeaponData = Resources.LoadAll<WeaponDefinitionSO>("Weapons");
            weapons = Array.FindAll(allWeaponData, IsInCurrentTestScope);
            Array.Sort(weapons, (left, right) => left.switchOrder.CompareTo(right.switchOrder));
            if (weapons.Length == 0)
            {
                Debug.LogError("[MIeMie] No Sword/Axe/Hammer/Dagger WeaponDefinitionSO found under Resources/Weapons.");
                return;
            }

            WeaponDefinitionSO selected = startingWeapon;
            if (selected == null || Array.IndexOf(weapons, selected) < 0)
                selected = Array.Find(weapons, item => item.originalName == "Sword_Default") ?? weapons[0];
            SetWeapon(selected);
        }

        private static bool IsInCurrentTestScope(WeaponDefinitionSO weapon)
        {
            if (weapon == null)
                return false;
            return weapon.family == PlayerWeaponFamily.Sword ||
                   weapon.family == PlayerWeaponFamily.Axe ||
                   weapon.family == PlayerWeaponFamily.Hammer ||
                   weapon.family == PlayerWeaponFamily.Dagger;
        }

        private void BeginAttack()
        {
            AttackSegmentData segment = CurrentSegment;
            if (segment == null)
                return;

            bool attackFromDodge = movement.CancelDodgeForAttack();
            float dodgeAttackMultiplier = attackFromDodge ? settings.dodgeAttackLungeMultiplier : 1f;
            attacking = true;
            canBreak = false;
            angleLocked = !segment.canChangeDirectionDuringAttack;
            attackStarted = Time.unscaledTime;
            storedFacing = ResolveAttackFacing();
            movement.SetCombatLock(true);
            movement.StartLunge(
                storedFacing,
                segment.lungeSpeed * dodgeAttackMultiplier,
                segment.lungeDuration * dodgeAttackMultiplier);
            view.SetFacingVisual(storedFacing);
            view.SetAttackAnimation(segment.animationName, currentWeapon.attackRateMultiplier);
        }

        public bool TryCancelForDodge()
        {
            if (!attacking)
                return true;
            if (currentWeapon != null && !currentWeapon.canBreakDodge && !canBreak)
                return false;

            attacking = false;
            canBreak = true;
            queued = false;
            angleLocked = false;
            attackEnded = Time.time;
            movement.SetCombatLock(false);
            return true;
        }

        private float ResolveAttackFacing()
        {
            Camera activeCamera = Camera.main;
            if (activeCamera == null)
                return movement.FacingAngle;

            Vector3 playerScreenPosition = activeCamera.WorldToScreenPoint(transform.position);
            Vector2 direction = (Vector2)Input.mousePosition - (Vector2)playerScreenPosition;
            if (direction.sqrMagnitude < 0.0001f)
                return movement.FacingAngle;
            return Mathf.Repeat(Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg, 360f);
        }

        private void HandleSpineEvent(TrackEntry entry, Spine.Event spineEvent)
        {
            AttackSegmentData segment = CurrentSegment;
            if (!attacking || segment == null)
                return;

            string eventName = spineEvent.Data.Name;
            if (eventName == "Update Angle")
            {
                movement.SetFacingAngle(storedFacing);
                view.SetFacingVisual(storedFacing);
                if (!segment.canFreelyChangeDirection)
                    angleLocked = true;
            }
            else if (eventName == "Attack Deal Damage")
            {
                DealCurrentSegmentDamage(segment);
            }
            else if (eventName == "Attack Can Break")
            {
                comboIndex = (comboIndex + 1) % currentWeapon.combos.Length;
                canBreak = true;
                if (queued)
                {
                    queued = false;
                    BeginAttack();
                }
            }
            else if (eventName == "Attack Has Finished")
            {
                FinishAttack();
            }
        }

        private void DealCurrentSegmentDamage(AttackSegmentData segment)
        {
            CombatCameraRig.Instance?.Shake(Mathf.Max(0f, segment.cameraShake));

            bool genericPlayerSwipe = segment.swipeKind == WeaponSwipeKind.PlayerSwipe0 &&
                                      currentWeapon.normalAttackStrategy != NormalAttackStrategy.Chain &&
                                      currentWeapon.normalAttackStrategy != NormalAttackStrategy.Blunderbuss;
            if (!genericPlayerSwipe)
            {
                if (!warnedUnsupportedSwipe)
                {
                    Debug.LogWarning("[MIeMie] " + currentWeapon.originalName + " uses " + segment.swipeKind +
                                     ". Its data and animation are active, but its dedicated hit algorithm is intentionally not approximated.");
                    warnedUnsupportedSwipe = true;
                }
                return;
            }

            SwordSwipe.Create(gameObject, storedFacing, segment.rangeRadius, segment.damage,
                segment.hitKnockback, segment.targetKnockbackMultiplier, segment.targetReactionShakeMultiplier,
                segment.normalHitStopMultiplier, segment.deathHitStopMultiplier,
                segment.hitboxDuration, CombatTeam.Player,
                currentWeapon.originalName + " / " + segment.swipeKind);
        }

        private void FinishAttack()
        {
            if (!attacking)
                return;
            attacking = false;
            canBreak = false;
            queued = false;
            attackEnded = Time.time;
            movement.SetCombatLock(false);
        }
    }
}
