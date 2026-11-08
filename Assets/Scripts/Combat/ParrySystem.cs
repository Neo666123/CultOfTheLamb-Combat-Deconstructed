using System.Collections;
using UnityEngine;
using AbyssBioCat.CameraSystem;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace AbyssBioCat.Combat
{
    /// <summary>
    /// 核心战斗机制：宽松低门槛弹反系统 (Parry) 与打击停顿 (HitStop)
    /// 原生适配 Unity 6 New Input System
    /// </summary>
    public class ParrySystem : MonoBehaviour
    {
        [Header("弹反判定窗口")]
        public float parryDuration = 0.15f; // 严格对齐 DEMO_MIN 要求的 0.15s 窗口       // 宽松弹反窗口（0.25秒，门槛极低人人可弹）

        [Header("打击感配置")]
        public float hitStopDuration = 0.08f;     // 弹反成功顿帧时长（0.08秒视觉停顿）
        public float cameraShakeIntensity = 0.35f;// 镜头震动幅度

        private bool _isParrying;
        private float _parryTimer;
        private CameraFollow25D _cameraFollow;

        private void Start()
        {
            if (UnityEngine.Camera.main != null)
            {
                _cameraFollow = UnityEngine.Camera.main.GetComponent<CameraFollow25D>();
            }
        }

        private void Update()
        {
            bool parryPressed = false;

#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
            {
                parryPressed = true;
            }

            var gamepad = Gamepad.current;
            if (gamepad != null && (gamepad.buttonEast.wasPressedThisFrame || gamepad.buttonWest.wasPressedThisFrame))
            {
                parryPressed = true;
            }
#else
            if (Input.GetKeyDown(KeyCode.Space))
            {
                parryPressed = true;
            }
#endif

            if (parryPressed && !_isParrying)
            {
                StartCoroutine(ParryWindowRoutine());
            }
        }

        private IEnumerator ParryWindowRoutine()
        {
            _isParrying = true;
            _parryTimer = parryDuration;

            Debug.Log("<color=cyan>[Parry] 小黑猫尾巴毛球警戒！开启 0.25 秒宽松弹反窗口！</color>");

            while (_parryTimer > 0)
            {
                _parryTimer -= Time.deltaTime;
                yield return null;
            }

            _isParrying = false;
        }

        public bool TryParry(Vector3 attackSourcePos)
        {
            if (_isParrying)
            {
                OnParrySuccess(attackSourcePos);
                return true;
            }
            return false;
        }

        private void OnParrySuccess(Vector3 attackSourcePos)
        {
            Debug.Log("<color=yellow>★★★ 弹反成功！触发顿帧与华丽反馈！★★★</color>");

            StartCoroutine(HitStopRoutine(hitStopDuration));

            if (_cameraFollow != null)
            {
                _cameraFollow.Shake(0.18f, cameraShakeIntensity);
            }
        }

        private IEnumerator HitStopRoutine(float duration)
        {
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = 1f;
        }
    }
}

