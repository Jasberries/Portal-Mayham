using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class InputSystem : MonoBehaviour
{
    public Vector2 LookValue { get; private set; }
    public Vector2 MoveValue { get; private set; }
    public bool IsGrabbing { get; private set; }
    public bool IsCrouching { get; private set; }
    public bool DisableMovement { get; private set; }
    public event Action Interacted;

    [HideInInspector]
    public PlayerInput playerInput;

    public void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
    }

    private void OnLook(InputAction.CallbackContext context)
    {
        if (playerInput.currentControlScheme == "Gamepad")
        {
            LookValue = context.ReadValue<Vector2>() * 10f;
        }
        else
        {
            LookValue = context.ReadValue<Vector2>();
        }
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        MoveValue = context.ReadValue<Vector2>();
    }

    public bool Jumped()
    {
        return playerInput.actions["Jump"].IsPressed();
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        Interacted?.Invoke();
    }

    private void OnGrab(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            IsGrabbing = true;
        }
        else if (context.canceled)
        {
            IsGrabbing = false;
        }
    }

    private void OnCrouch(InputAction.CallbackContext context)
    {
    }

    public void DisablePlayerMovement(bool state)
    {
        DisableMovement = state;
    }

    public void LockCursor(bool lockCursor)
    {
        if (!lockCursor)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    private void OnEnable()
    {
        playerInput.actions["Move"].performed += OnMove;
        playerInput.actions["Move"].canceled += OnMove;
        playerInput.actions["Look"].performed += OnLook;
        playerInput.actions["Look"].canceled += OnLook;
        // playerInput.actions["Grab"].performed += OnGrab;
        // playerInput.actions["Grab"].canceled += OnGrab;
        playerInput.actions["Interact"].performed += OnInteract;
    }

    private void OnDisable()
    {
        playerInput.actions["Move"].performed -= OnMove;
        playerInput.actions["Move"].canceled -= OnMove;
        playerInput.actions["Look"].performed -= OnLook;
        playerInput.actions["Look"].canceled -= OnLook;
        // playerInput.actions["Grab"].performed -= OnGrab;
        // playerInput.actions["Grab"].canceled -= OnGrab;
        playerInput.actions["Interact"].performed -= OnInteract;
    }
}