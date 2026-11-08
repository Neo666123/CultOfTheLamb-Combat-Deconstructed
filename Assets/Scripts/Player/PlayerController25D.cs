using UnityEngine;
using AbyssBioCat.Core;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace AbyssBioCat.Player
{
    /// <summary>
    /// 2.5D 小黑猫八向移动控制器
    /// 极致干脆、零滑步、指哪走哪（向《哈迪斯》《咩咩启示录》手感看齐）
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController25D : MonoBehaviour
    {
        [Header("移动速度")]
        public float moveSpeed = 9f;           // 奔跑速度

        [Header("重力")]
        public float gravity = 30f;

        [Header("视觉呈现")]
        public Transform visualRoot;           // 挂载 Sprite / Spine 的子物体
        public SpriteRenderer spriteRenderer;  // 测试阶段的默认精灵

        private CharacterController _characterController;
        private Vector3 _velocity;
        private bool _facingRight = true;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            if (visualRoot == null && transform.childCount > 0)
            {
                visualRoot = transform.GetChild(0);
            }
        }

        private void Update()
        {
            HandleMovement();
        }

        private void HandleMovement()
        {
            Vector2 input = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) input.y += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) input.y -= 1f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) input.x -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) input.x += 1f;
            }

            var gamepad = Gamepad.current;
            if (gamepad != null)
            {
                Vector2 stick = gamepad.leftStick.ReadValue();
                if (stick.sqrMagnitude > 0.04f)
                {
                    input = stick;
                }
            }
#else
            input.x = Input.GetAxisRaw("Horizontal");
            input.y = Input.GetAxisRaw("Vertical");
#endif

            Vector3 inputDirection = new Vector3(input.x, 0f, input.y).normalized;

            // 彻底砍掉滑步惯性：按下立刻跑满，松手瞬间停死（零拖泥带水）
            Vector3 horizontalMove = inputDirection * moveSpeed;

            // 重力保持贴地
            if (_characterController.isGrounded)
            {
                _velocity.y = -0.5f;
            }
            else
            {
                _velocity.y -= gravity * Time.deltaTime;
            }

            // 执行移动
            Vector3 finalMotion = (horizontalMove + new Vector3(0, _velocity.y, 0)) * Time.deltaTime;
            _characterController.Move(finalMotion);

            // 朝向翻转（左右转向）
            if (input.x > 0.01f && !_facingRight)
            {
                Flip(true);
            }
            else if (input.x < -0.01f && _facingRight)
            {
                Flip(false);
            }
        }

        private void Flip(bool right)
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
