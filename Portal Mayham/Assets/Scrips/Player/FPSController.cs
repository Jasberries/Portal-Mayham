using System;
using UnityEngine;

public class FPSController : PortalTraveller {

    public float walkSpeed = 3;
    public float runSpeed = 6;
    public float smoothMoveTime = 0.1f;
    public float jumpForce = 8;
    public float gravity = 18;

    public bool lockCursor;
    public float mouseSensitivity = 10;
    public Vector2 pitchMinMax = new (-40, 85);
    public float rotationSmoothTime = 0.1f;
    public float yaw;
    public float pitch;

    [SerializeField] private Transform playerGrabPointTransform;
    [SerializeField] private float raycastLength = 10f;
    [SerializeField] private LayerMask layer;
    
    CharacterController controller;
    Camera cam; 
    Interactable currentInteractableObject;
    Interactable interactedObject;
    InputSystem inputSystem;
    float smoothYaw;
    float smoothPitch;
    float yawSmoothV;
    float pitchSmoothV;
    float verticalVelocity;
    Vector3 velocity;
    Vector3 smoothV;
    Vector3 rotationSmoothVelocity;
    Vector3 currentRotation;

    bool jumping;
    float lastGroundedTime;
    bool disabled;

    void Start () {
        cam = Camera.main;
        controller = GetComponent<CharacterController>();
        inputSystem = GetComponent<InputSystem>();
        inputSystem.LockCursor(lockCursor);
        SetUpEvents();
        
        yaw = transform.eulerAngles.y;
        pitch = cam.transform.localEulerAngles.x;
        smoothYaw = yaw;
        smoothPitch = pitch;
    }

    void Update ()
    {
        inputSystem.LockCursor(true);

        if (disabled) return;
        Vector2 input = inputSystem.MoveValue;

        Vector3 inputDir = new Vector3 (input.x, 0, input.y).normalized;
        Vector3 worldInputDir = transform.TransformDirection (inputDir);

        float currentSpeed = inputSystem.IsCrouching ? runSpeed : walkSpeed;
        Vector3 targetVelocity = worldInputDir * currentSpeed;
        velocity = Vector3.SmoothDamp (velocity, targetVelocity, ref smoothV, smoothMoveTime);

        verticalVelocity -= gravity * Time.deltaTime;
        velocity = new Vector3 (velocity.x, verticalVelocity, velocity.z);

        var flags = controller.Move (velocity * Time.deltaTime);
        if (flags == CollisionFlags.Below) {
            jumping = false;
            lastGroundedTime = Time.time;
            verticalVelocity = 0;
        }

        if (inputSystem.Jumped()) {
            float timeSinceLastTouchedGround = Time.time - lastGroundedTime;
            if (controller.isGrounded || !jumping && timeSinceLastTouchedGround < 0.15f) {
                jumping = true;
                verticalVelocity = jumpForce;
            }
        }

        Vector2 mVector = inputSystem.LookValue;
        
        yaw += mVector.x * mouseSensitivity * 0.1f;
        pitch -= mVector.y * mouseSensitivity * 0.1f;
        pitch = Mathf.Clamp (pitch, pitchMinMax.x, pitchMinMax.y);
        smoothPitch = Mathf.SmoothDampAngle (smoothPitch, pitch, ref pitchSmoothV, rotationSmoothTime);
        smoothYaw = Mathf.SmoothDampAngle (smoothYaw, yaw, ref yawSmoothV, rotationSmoothTime);

        transform.eulerAngles = Vector3.up * smoothYaw;
        cam.transform.localEulerAngles = Vector3.right * smoothPitch;
        
        //Hande interact with object
        if (Physics.Raycast(cam.transform.position, cam.transform.forward,
                out RaycastHit hit, raycastLength, layer))
        {
            if (hit.collider.TryGetComponent(out interactedObject))
            {
                var interactState = interactedObject.GetState();
                switch (interactState)
                {
                    case Interactable.InteractEnum.Interactable:
                        interactedObject.ShowOutline(true);
                        break;
                    case Interactable.InteractEnum.Grabbable:
                    case Interactable.InteractEnum.PickUp:
                        HandleGrab();
                        break;
                }
            }
        }
        else
        {
            if (interactedObject == null) return;
            interactedObject.ShowOutline(false);
            interactedObject = null;
        }
    }

    private void HandleInteract()
    {
        if (interactedObject != null)
        {
            interactedObject.Interacted();
        }
    }

    private void HandleGrab()
    {
        if (inputSystem.IsGrabbing)
        {
            interactedObject.Grab(playerGrabPointTransform);
        }
        else
        {
            if (interactedObject == null) return;
            interactedObject.Drop();
            interactedObject = null;
        }
    }

    public override void Teleport (Transform fromPortal, Transform toPortal, Vector3 pos, Quaternion rot) {
        transform.position = pos;
        Vector3 eulerRot = rot.eulerAngles;
        float delta = Mathf.DeltaAngle (smoothYaw, eulerRot.y);
        yaw += delta;
        smoothYaw += delta;
        transform.eulerAngles = Vector3.up * smoothYaw;
        velocity = toPortal.TransformVector (fromPortal.InverseTransformVector (velocity));
        Physics.SyncTransforms ();
    }

    private void SetUpEvents()
    {
        inputSystem.Interacted += HandleInteract;
    }

    private void OnDisable()
    {
        inputSystem.Interacted -= HandleInteract;
    }
}