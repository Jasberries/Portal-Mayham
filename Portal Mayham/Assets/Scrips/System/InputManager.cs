using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour, PlayerActionMap.IPlayerActions
{
    public static InputManager Instance;
    public Vector2 ViewValue { get; private set; }
    public Vector2 MoveValue { get; private set; }
    public bool IsGrabbing { get; private set; }
    public bool IsCrouching { get; private set; }
    public bool IsSprinting { get; private set; }

    public float ScrollValue { get; private set; }

    public bool Special { get; private set; }
    public bool DisableAllMovement { get; private set; }
    public bool CanLook { get; private set; }

    public event Action Interacted;
    public event Action Pause;
    public event Action Jump;

    private PlayerActionMap input;

    public float Sensitivity
    {
        get => PlayerPrefs.GetFloat("sensitivity", 1.0f); 
        set
        {
            if (!Mathf.Approximately(PlayerPrefs.GetFloat("sensitivity"), value))
            {
                PlayerPrefs.SetFloat("sensitivity", value);
                PlayerPrefs.Save(); 
            }
        }
    }
    public void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
        
    }
    private void OnEnable()
    {
        if (input == null)
        {
            input = new PlayerActionMap();
            input.Player.SetCallbacks(this);
        }
        
        EnableInput();
    }
    private void OnDisable()
    {
        DisableInput();
    }
    public void DisablePlayerMovement(bool state)
    {
        DisableAllMovement = state;
    }

    public void DisablePlayerLook(bool state)
    {
        CanLook = !state;
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

    public void OnMove(InputAction.CallbackContext context)
    {
        MoveValue = context.ReadValue<Vector2>();
        MoveValue = Vector2.ClampMagnitude(MoveValue, 1f);
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        ViewValue = context.ReadValue<Vector2>();
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Interacted?.Invoke();
        }
    }

    public void OnSpecial(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Special = true;
        }

        if (context.canceled)
        {
            Special = false;
        }
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Jump?.Invoke();
        }
    }

    public void OnCrouching(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            IsCrouching = true;
        }

        if (context.canceled)
        {
            IsCrouching = false;
        }
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            IsSprinting = true;
        }

        if (context.canceled)
        {
            IsSprinting = false;
        }
    }

    public void OnPause(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Pause?.Invoke();
        }
    }

    public void OnGrab(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            IsGrabbing = true;
        }

        if (context.canceled)
        {
            IsGrabbing = false;
        }
    }

    public void OnScroll(InputAction.CallbackContext context)
    {
        ScrollValue = context.ReadValue<float>() / 60f;
    }
    
    private void EnableInput()
    {
        input.Player.Enable();
    }

    private void DisableInput()
    {
        input.Player.Disable();
    }
}