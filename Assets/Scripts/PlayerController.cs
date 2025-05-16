using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 2f; // 基础移动速度
    [SerializeField] private float sprintMultiplier = 2f; // 冲刺速度倍数
    [SerializeField] private Transform cameraTransform; // 相机的 Transform
    private Rigidbody rb;
    private Vector2 moveInput; // 存储输入方向

    private InputAction moveAction; // 输入 Action 引用
    private InputAction sprintAction; // 冲刺 Action 引用

    private Animator animator;

    private void Awake()
    {
        // 初始化 Input System
        InputActionAsset inputActions = GetComponent<PlayerInput>().actions;
        moveAction = inputActions.FindActionMap("Player").FindAction("Move");
        sprintAction = inputActions.FindActionMap("Player").FindAction("Sprint"); // 假设已在 Input Actions 中定义 Sprint 动作
        rb = GetComponent<Rigidbody>();

        animator = GetComponent<Animator>();

        // 启用刚体插值以减少抖动
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // 如果未指定相机，尝试自动获取主相机
        if (cameraTransform == null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    private void OnEnable()
    {
        // 启用输入 Action 并绑定回调
        moveAction.Enable();
        moveAction.performed += OnMove;
        moveAction.canceled += OnMoveStop;

        sprintAction.Enable();
        sprintAction.performed += OnSprint;
        sprintAction.canceled += OnSprintStop;
    }

    private void OnDisable()
    {
        // 禁用输入 Action 并解绑回调
        moveAction.Disable();
        moveAction.performed -= OnMove;
        moveAction.canceled -= OnMoveStop;

        sprintAction.Disable();
        sprintAction.performed -= OnSprint;
        sprintAction.canceled -= OnSprintStop;
    }

    // 当按下 WASD 时持续触发
    private void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();

        // 只有在有移动输入时才更新动画状态
        if (moveInput != Vector2.zero)
        {
            animator.SetInteger("walk", 1);
            animator.SetInteger("idle", 0);
        }
    }

    // 当松开按键时停止移动
    private void OnMoveStop(InputAction.CallbackContext context)
    {
        moveInput = Vector2.zero;

        // 停止移动时重置动画状态
        animator.SetInteger("walk", 0);
        animator.SetInteger("idle", 1);
        animator.SetInteger("run", 0);
    }

    // 当按下 shift 时持续加速
    private void OnSprint(InputAction.CallbackContext context)
    {
        // 只有在有移动输入时才设置为跑步状态
        if (moveInput != Vector2.zero)
        {
            animator.SetInteger("run", 1);
            animator.SetInteger("walk", 0);
            animator.SetInteger("idle", 0);
        }
    }

    // 当松开 shift 按键时停止加速
    private void OnSprintStop(InputAction.CallbackContext context)
    {
        // 停止冲刺时恢复为行走状态（如果有移动输入）
        if (moveInput != Vector2.zero)
        {
            animator.SetInteger("run", 0);
            animator.SetInteger("walk", 1);
            animator.SetInteger("idle", 0);
        }
        else
        {
            // 如果没有移动输入，恢复为静止状态
            animator.SetInteger("run", 0);
            animator.SetInteger("walk", 0);
            animator.SetInteger("idle", 1);
        }
    }

    private void FixedUpdate()
    {
        if (StatusManage.Instance.GameState == StatusManage.GameStateType.Playing)
        {
            MovePlayer();
        }
    }
    
    private void MovePlayer()
    {
        // 检测是否按下 Left Shift 键
        float currentSpeed = sprintAction.IsPressed() ? moveSpeed * sprintMultiplier : moveSpeed;

        // 将输入方向转换为相机方向的世界坐标系移动
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        // 忽略垂直方向
        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        Vector3 movement = (forward * moveInput.y + right * moveInput.x) * currentSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + movement);

        // 同步玩家的旋转到相机的水平旋转
        Quaternion targetRotation = Quaternion.Euler(0f, cameraTransform.eulerAngles.y, 0f);
        rb.MoveRotation(targetRotation);
    }
}


