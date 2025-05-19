using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public Vector2 MoveInput { get; private set; }
    public bool IsSprinting { get; private set; }

    private InputAction moveAction;
    private InputAction sprintAction;

    public void Initialize(PlayerInput playerInput)
    {
        InputActionAsset inputActions = playerInput.actions;
        moveAction = inputActions.FindActionMap("Player").FindAction("Move");
        sprintAction = inputActions.FindActionMap("Player").FindAction("Sprint");

        moveAction.performed += OnMove;
        moveAction.canceled += OnMoveStop;

        sprintAction.performed += OnSprint;
        sprintAction.canceled += OnSprintStop;

        moveAction.Enable();
        sprintAction.Enable();
    }

    public void Cleanup()
    {
        moveAction.Disable();
        sprintAction.Disable();

        moveAction.performed -= OnMove;
        moveAction.canceled -= OnMoveStop;

        sprintAction.performed -= OnSprint;
        sprintAction.canceled -= OnSprintStop;
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        MoveInput = context.ReadValue<Vector2>();
    }

    private void OnMoveStop(InputAction.CallbackContext context)
    {
        MoveInput = Vector2.zero;
    }

    private void OnSprint(InputAction.CallbackContext context)
    {
        IsSprinting = true;
    }

    private void OnSprintStop(InputAction.CallbackContext context)
    {
        IsSprinting = false;
    }
}