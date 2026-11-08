using UnityEngine;

namespace AbyssBioCat.Core
{
    /// <summary>
    /// 2.5D 广告牌组件：使 2D 精灵或 Spine 纸片在 3D 俯视角下始终面向摄像机，避免纸片倒伏
    /// </summary>
    [ExecuteAlways]
    public class Billboard25D : MonoBehaviour
    {
        public enum BillboardMode
        {
            CameraForward, // 完全平行于相机切面（咩咩启示录标准做法）
            YAxisOnly      // 仅绕 Y 轴旋转
        }

        [Header("对齐模式")]
        [SerializeField] private BillboardMode mode = BillboardMode.CameraForward;

        private UnityEngine.Camera _mainCamera;

        private void OnEnable()
        {
            FindCamera();
        }

        private void LateUpdate()
        {
            if (_mainCamera == null)
            {
                FindCamera();
                if (_mainCamera == null) return;
            }

            switch (mode)
            {
                case BillboardMode.CameraForward:
                    transform.rotation = _mainCamera.transform.rotation;
                    break;
                case BillboardMode.YAxisOnly:
                    Vector3 forward = _mainCamera.transform.forward;
                    forward.y = 0;
                    if (forward.sqrMagnitude > 0.001f)
                    {
                        transform.rotation = Quaternion.LookRotation(forward);
                    }
                    break;
            }
        }

        private void FindCamera()
        {
            _mainCamera = UnityEngine.Camera.main;
        }
    }
}
