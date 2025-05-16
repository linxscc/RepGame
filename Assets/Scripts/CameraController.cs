using UnityEngine;
using UnityEngine.InputSystem;

namespace Game
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private Transform playerTransform; // 玩家 Transform
        [SerializeField] private Vector3 offset = new Vector3(0f, 5f, -10f); // 相机与玩家的偏移量
        [SerializeField] private float sensitivity = 0.1f; // 鼠标灵敏度
        [SerializeField] private InputActionAsset inputActions;

        private float yaw = 0f; // 水平旋转角度
        private float pitch = 0f; // 垂直旋转角度
        private InputAction cameraMoveAction;

        private void Awake()
        {
            // 初始化 Input System
            cameraMoveAction = inputActions.FindActionMap("Camera").FindAction("Rotate");

            // 如果未指定玩家，尝试自动获取玩家
            if (playerTransform == null)
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    playerTransform = player.transform;
                }
            }

            // 根据初始游戏状态设置光标锁定
            UpdateCursorLock(StatusManage.Instance.GameState);
        }

        private void OnEnable()
        {
            cameraMoveAction.Enable();
            cameraMoveAction.performed += OnLook;

            // 订阅游戏状态变化事件
            StatusManage.Instance.OnGameStateChanged += UpdateCursorLock;
        }

        private void OnDisable()
        {
            cameraMoveAction.Disable();
            cameraMoveAction.performed -= OnLook;

            // 取消订阅游戏状态变化事件
            StatusManage.Instance.OnGameStateChanged -= UpdateCursorLock;
        }

        private void OnLook(InputAction.CallbackContext context)
        {
            Vector2 lookInput = context.ReadValue<Vector2>();

            // 更新水平和垂直旋转角度
            yaw += lookInput.x * sensitivity;
            pitch -= lookInput.y * sensitivity;

            // 限制垂直旋转角度在 -30 到 60 度之间，防止相机翻转
            pitch = Mathf.Clamp(pitch, -30f, 60f);
        }

        private void LateUpdate()
        {
            if (StatusManage.Instance.GameState == StatusManage.GameStateType.Playing)
            {
                UpdateCameraPosition();
            }
        }

        private void UpdateCameraPosition()
        {
            if (playerTransform == null) return;

            // 计算相机的旋转
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);

            // 更新相机位置和旋转
            transform.position = playerTransform.position + rotation * offset;
            transform.LookAt(playerTransform.position + Vector3.up * 1.5f); // 让相机稍微看向玩家的上方
        }

        private void LockCursor(bool isLocked)
        {
            Cursor.lockState = isLocked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !isLocked;
        }

        private void UpdateCursorLock(StatusManage.GameStateType gameState)
        {
            if (gameState == StatusManage.GameStateType.Playing)
            {
                LockCursor(true);
            }
            else
            {
                LockCursor(false);
            }
        }
    }
}