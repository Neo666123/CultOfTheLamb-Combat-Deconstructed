using System.Collections;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace AbyssBioCat.Player
{
    /// <summary>
    /// 遵循 PLAYER_CONTROLLER_PICK.md 规范的玩家控制器
    /// 结合 open-unity 骨架与 Lix 2D 物理冲刺，针对 Unity 6 优化
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("基础移动")]
        [SerializeField] private float moveSpeed = 5f;

        [Header("冲刺配置 (Dash)")]
        [SerializeField] private float dashSpeed = 20f;
        [SerializeField] private float dashDuration = 0.2f;
        [SerializeField] private float dashCooldown = 0.6f;

        [Header("视觉表现")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private SpriteRenderer spriteRenderer;

        // 状态标识
        public bool IsDashing { get; private set; }
        public bool IsInvulnerable { get; private set; } // 冲刺期间的 0.2s 无敌帧
        public Vector2 LastMoveDirection { get; private set; } = Vector2.down; // 默认朝南（下）

        private Rigidbody2D _rb2d;
        private Vector2 _moveInput;
        private float _lastDashTime = -999f;
        private bool _facingRight = true;

        private void Awake()
        {
            _rb2d = GetComponent<Rigidbody2D>();

            // 按照规范强制配置 Rigidbody2D
            _rb2d.gravityScale = 0f;
            _rb2d.freezeRotation = true;
            _rb2d.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _rb2d.interpolation = RigidbodyInterpolation2D.Interpolate;

            if (visualRoot == null && transform.childCount > 0)
            {
                visualRoot = transform.GetChild(0);
            }
            if (spriteRenderer == null && visualRoot != null)
            {
                spriteRenderer = visualRoot.GetComponent<SpriteRenderer>();
            }
        }

        private void Update()
        {
            HandleInput();
        }

        private void FixedUpdate()
        {
            ApplyMovement();
        }

        private void HandleInput()
        {
            if (IsDashing) return;

            Vector2 input = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) input.y += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) input.y -= 1f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) input.x -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) input.x += 1f;

                if ((keyboard.leftShiftKey.wasPressedThisFrame || keyboard.rightShiftKey.wasPressedThisFrame) && CanDash())
                {
                    StartCoroutine(DashRoutine());
                }
            }

            var gamepad = Gamepad.current;
            if (gamepad != null)
            {
                Vector2 stick = gamepad.leftStick.ReadValue();
                if (stick.sqrMagnitude > 0.04f) input = stick;

                if ((gamepad.buttonSouth.wasPressedThisFrame || gamepad.rightTrigger.wasPressedThisFrame) && CanDash())
                {
                    StartCoroutine(DashRoutine());
                }
            }
#else
            input.x = Input.GetAxisRaw("Horizontal");
            input.y = Input.GetAxisRaw("Vertical");

            if (Input.GetKeyDown(KeyCode.LeftShift) && CanDash())
            {
                StartCoroutine(DashRoutine());
            }
#endif

            // 输入归一化
            _moveInput = input.normalized;

            // 待机朝向（8-dir 核心逻辑：记录最后一次非零移动方向，停下时不归零重置）
            if (_moveInput.sqrMagnitude > 0.01f)
            {
                LastMoveDirection = _moveInput;

                // 左右翻转视觉
                if (_moveInput.x > 0.01f && !_facingRight)
                {
                    SetFacing(true);
                }
                else if (_moveInput.x < -0.01f && _facingRight)
                {
                    SetFacing(false);
                }
            }
        }

        private void ApplyMovement()
        {
            if (IsDashing) return;

            // Unity 6 标准属性：linearVelocity（不再使用旧版 velocity）
            _rb2d.linearVelocity = _moveInput * moveSpeed;
        }

        private bool CanDash()
        {
            return Time.time >= _lastDashTime + dashCooldown;
        }

        private IEnumerator DashRoutine()
        {
            IsDashing = true;
            IsInvulnerable = true;
            _lastDashTime = Time.time;

            // 冲刺方向：若当前有摇杆输入则朝输入冲，若停在原地则朝最后待机朝向冲
            Vector2 dashDir = _moveInput.sqrMagnitude > 0.01f ? _moveInput : LastMoveDirection;
            _rb2d.linearVelocity = dashDir * dashSpeed;

            yield return new WaitForSeconds(dashDuration);

            // 冲刺结束瞬间刹车
            _rb2d.linearVelocity = Vector2.zero;
            IsInvulnerable = false;
            IsDashing = false;
        }

        private void SetFacing(bool right)
        {
            _facingRight = right;
            if (visualRoot != null)
            {
                Vector3 scale = visualRoot.localScale;
                scale.x = Mathf.Abs(scale.x) * (_facingRight ? 1f : -1f);
                visualRoot.localScale = scale;
            }
            else if (spriteRenderer != null)
            {
                spriteRenderer.flipX = !_facingRight;
            }
        }
    }
}
