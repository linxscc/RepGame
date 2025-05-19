using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private AnimationConfig animationConfig;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float sprintMultiplier = 2f;
    [SerializeField] private Transform cameraTransform;

    private Rigidbody rb;
    private InputManager inputManager;
    private AnimationStateManager animationStateManager;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        inputManager = gameObject.AddComponent<InputManager>();
        animationStateManager = gameObject.AddComponent<AnimationStateManager>();

        inputManager.Initialize(GetComponent<PlayerInput>());
        animationStateManager.Initialize(GetComponent<Animator>(), animationConfig);

        rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (cameraTransform == null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    private void OnEnable()
    {
        inputManager.Initialize(GetComponent<PlayerInput>());
    }

    private void OnDisable()
    {
        inputManager.Cleanup();
    }

    private void FixedUpdate()
    {
        if (StatusManage.Instance.GameState == StatusManage.GameStateType.Playing)
        {
            HandleMovement();
            HandleAnimation();
        }
    }

    private void HandleMovement()
    {
        float currentSpeed = inputManager.IsSprinting ? moveSpeed * sprintMultiplier : moveSpeed;

        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        Vector3 movement = (forward * inputManager.MoveInput.y + right * inputManager.MoveInput.x) * currentSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + movement);

        if (inputManager.MoveInput != Vector2.zero)
        {
            Quaternion targetRotation = Quaternion.Euler(0f, cameraTransform.eulerAngles.y, 0f);
            rb.MoveRotation(targetRotation);
        }
    }

    private void HandleAnimation()
    {
        if (inputManager.MoveInput == Vector2.zero)
        {
            animationStateManager.SetIdle();
        }
        else if (inputManager.IsSprinting)
        {
            animationStateManager.SetRunning();
        }
        else
        {
            animationStateManager.SetWalking();
        }
    }
}


