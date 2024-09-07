using System;
#if CINEMACHINE_INCLUDED
using Cinemachine;
#endif
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

public class FirstPersonController : MonoBehaviour
{
    private Rigidbody rb;
    private CapsuleCollider playerCollider;
    private InputManager inputManager;

    #region Camera Movement Variables

    public bool useCinemachine;
    
    #if CINEMACHINE_INCLUDED
    public CinemachineVirtualCamera playerVCamera;
    #endif

    public Camera playerCamera;

    private ICustomCamera activeCamera;
    
    public float fov = 60f;
    public bool invertCamera = false;
    public float mouseSensitivity = 2f;
    public float maxLookAngle = 50f;

    // Internal Variables
    private float yaw = 0.0f;
    private float pitch = 0.0f;

    #region Camera Zoom Variables

    public bool enableZoom = true;
    public bool holdToZoom = false;
    public float zoomFOV = 30f;
    public float zoomStepTime = 5f;

    // Internal Variables
    private bool isZoomed = false;

    #endregion

    #endregion

    #region Movement Variables

    public bool playerCanMove = true;
    public float walkSpeed = 5f;
    public float maxAcceleration = 10f;
    public float airControl = 5f;
    public float maxGroundAngle = 25f;
    public float groundSnapSpeed = 100f;
    public float probeDistance = 2f;
    public LayerMask probeMask = -1;

    // Internal Variables
    private bool isWalking = false;
    private float currentSpeed;
    private float minGroundDotProduct;
    private Vector3 contactNormal;
    private Vector3 velocity, desiredVelocity;

    #region Sprint

    public bool enableSprint = true;
    public bool unlimitedSprint = false;
    public float sprintSpeed = 7f;
    public float sprintDuration = 5f;
    public float sprintCooldown = .5f;
    public float sprintFOV = 80f;
    public float sprintFOVStepTime = 10f;

    // Sprint Bar
    public bool useSprintBar = true;
    public bool hideBarWhenFull = true;
    public Image sprintBarBG;
    public Image sprintBar;
    public float sprintBarWidthPercent = .3f;
    public float sprintBarHeightPercent = .015f;

    // Internal Variables
    private CanvasGroup sprintBarCG;
    private bool isSprinting = false;
    private float sprintRemaining;
    private float sprintBarWidth;
    private float sprintBarHeight;
    private bool isSprintCooldown = false;
    private float sprintCooldownReset;

    #endregion

    #region Jump

    public bool enableJump = true;
    public float jumpPower = 5f;

    // Internal Variables
    private int groundContactCount;
    private int stepsSinceLastGrounded;
    private bool IsGrounded => groundContactCount > 0;

    #endregion

    #region Crouch

    public bool enableCrouch = true;
    public bool holdToCrouch = true;
    public float crouchHeight = .75f;
    public float crouchLerpSpeed = 0.5f;
    public float crouchSpeed = .5f;
    public Collider propCollider;


    // Internal Variables
    private bool isCrouched = false;
    private float standingHeight;
    private float currentHeight;
    private Vector3 standingCameraHeight;

    #endregion

    #endregion

    #region Head Bob

    public bool enableHeadBob = true;
    public Transform joint;
    public float bobSpeed = 10f;
    public Vector3 bobAmount = new Vector3(.15f, .05f, 0f);

    // Internal Variables
    private Vector3 jointOriginalPos;
    private float timer = 0;

    #endregion
    
    #region Camera Interface
    public interface ICustomCamera
    {
        Vector3 GetCameraPosition();
        void SetCameraFOV(float fov);

        float GetFOV();

        void SetLocalEulerAngles(Vector3 angel);
    }

    public class RegularCamera : ICustomCamera
    {
        private Camera camera;

        public RegularCamera(Camera cam)
        {
            camera = cam;
        }

        public Vector3 GetCameraPosition()
        {
            return camera.transform.localPosition;
        }

        public void SetCameraFOV(float fov)
        {
            camera.fieldOfView = fov;
        }

        public float GetFOV()
        {
            return camera.fieldOfView;
        }

        public void SetLocalEulerAngles(Vector3 angel)
        {
            camera.transform.localEulerAngles = angel;
        }
    }

#if CINEMACHINE_INCLUDED
using Cinemachine;

public class CinemachineCamera : ICustomCamera
{
    private CinemachineVirtualCamera vCam;

    public CinemachineCamera(CinemachineVirtualCamera virtualCamera)
    {
        vCam = virtualCamera;
    }

    public Vector3 GetCameraPosition()
    {
        return vCam.transform.localPosition;
    }

    public void SetCameraFOV(float fov)
    {
        vCam.m_Lens.FieldOfView = fov;
    }
}
#endif
    #endregion


    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerCollider = GetComponent<CapsuleCollider>();
        currentHeight = standingHeight = playerCollider.height;
        
        if (useCinemachine)
        {
        #if CINEMACHINE_INCLUDED
            activeCamera = new CinemachineCamera(playerVirtualCamera);
        #endif
        }
        else
        {
            activeCamera = new RegularCamera(playerCamera);
        }
        
        standingCameraHeight = activeCamera.GetCameraPosition();
        
        
        
        currentSpeed = walkSpeed;

        // Set internal variables
        activeCamera.SetCameraFOV(fov);
        jointOriginalPos = joint.localPosition;

        mouseSensitivity = PlayerPrefs.GetFloat("sensitivity", 1);

        if (!unlimitedSprint)
        {
            sprintRemaining = sprintDuration;
            sprintCooldownReset = sprintCooldown;
        }

        OnValidate();
    }

    private void OnValidate()
    {
        minGroundDotProduct = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
    }

    void Start()
    {
        inputManager.LockCursor(true);
        inputManager.DisablePlayerLook(false);
        
        #region Sprint Bar

        sprintBarCG = GetComponentInChildren<CanvasGroup>();

        if (useSprintBar)
        {
            sprintBarBG.gameObject.SetActive(true);
            sprintBar.gameObject.SetActive(true);

            float screenWidth = Screen.width;
            float screenHeight = Screen.height;

            sprintBarWidth = screenWidth * sprintBarWidthPercent;
            sprintBarHeight = screenHeight * sprintBarHeightPercent;

            sprintBarBG.rectTransform.sizeDelta = new Vector3(sprintBarWidth, sprintBarHeight, 0f);
            sprintBar.rectTransform.sizeDelta = new Vector3(sprintBarWidth - 2, sprintBarHeight - 2, 0f);

            if (hideBarWhenFull)
            {
                sprintBarCG.alpha = 0;
            }
        }
        else
        {
            sprintBarBG.gameObject.SetActive(false);
            sprintBar.gameObject.SetActive(false);
        }

        #endregion
    }

    float camRotation;

    private void Update()
    {
        #region Camera

        
        // Control camera movement
        if (inputManager.CanLook)
        {
            var mouseVector = inputManager.ViewValue;
            yaw = transform.localEulerAngles.y + mouseVector.x * mouseSensitivity * 0.1f;

            if (!invertCamera)
            {
                pitch -= mouseSensitivity * 0.1f * mouseVector.y;
            }
            else
            {
                // Inverted Y
                pitch += mouseSensitivity * 0.1f * mouseVector.y;
            }

            // Clamp pitch between lookAngle
            pitch = Mathf.Clamp(pitch, -maxLookAngle, maxLookAngle);

            transform.localEulerAngles = new Vector3(0, yaw, 0);
            activeCamera.SetLocalEulerAngles(new Vector3(pitch, 0, 0));
        }

        #region Camera Zoom

        if (enableZoom)
        {
            // Changes isZoomed when key is pressed
            // Behavior for toggle zoom
            if (Input.GetKeyDown(KeyCode.Period) && !holdToZoom && !isSprinting)
            {
                isZoomed = !isZoomed;
            }

            // Changes isZoomed when key is pressed
            // Behavior for hold to zoom
            if (holdToZoom && !isSprinting)
            {
                if (Input.GetKeyDown(KeyCode.Period))
                {
                    isZoomed = true;
                }
                else if (Input.GetKeyUp(KeyCode.Period))
                {
                    isZoomed = false;
                }
            }

            // Lerps camera.fieldOfView to allow for a smooth transition
            if (isZoomed)
            {
                activeCamera.SetCameraFOV(Mathf.Lerp(activeCamera.GetFOV(), zoomFOV, zoomStepTime * Time.deltaTime));
            }
            else if (!isZoomed && !isSprinting)
            {
                activeCamera.SetCameraFOV(Mathf.Lerp(activeCamera.GetFOV(), fov, zoomStepTime * Time.deltaTime));
            }
        }

        #endregion

        #endregion

        if (inputManager.DisableAllMovement)
        {
            rb.velocity = Vector3.zero;
            return;
        }

        #region Sprint

        if (enableSprint)
        {
            if (isSprinting)
            {
                isZoomed = false;
                activeCamera.SetCameraFOV(Mathf.Lerp(activeCamera.GetFOV(), sprintFOV, sprintFOVStepTime * Time.deltaTime));

                // Drain sprint remaining while sprinting
                if (!unlimitedSprint && (Math.Abs(velocity.x - 0.01f) > 0.04f || Math.Abs(velocity.z - 0.01f) > 0.04f))
                {
                    sprintRemaining -= 1 * Time.deltaTime;
                    if (sprintRemaining <= 0)
                    {
                        isSprinting = false;
                        isSprintCooldown = true;
                    }
                }
            }
            else
            {
                // Regain sprint while not sprinting
                sprintRemaining = Mathf.Clamp(sprintRemaining += 1 * Time.deltaTime, 0, sprintDuration);
                if (useCinemachine)
                {
                    
                }
                playerVirtualCamera.m_Lens.FieldOfView =
                    Mathf.Lerp(playerVirtualCamera.m_Lens.FieldOfView, fov, sprintFOVStepTime * Time.deltaTime);
            }

            // Handles sprint cooldown 
            // When sprint remaining == 0 stops sprint ability until hitting cooldown
            if (isSprintCooldown)
            {
                sprintCooldown -= 1 * Time.deltaTime;
                if (sprintCooldown <= 0)
                {
                    isSprintCooldown = false;
                }
            }
            else
            {
                sprintCooldown = sprintCooldownReset;
            }

            // Handles sprintBar 
            if (useSprintBar && !unlimitedSprint)
            {
                float sprintRemainingPercent = sprintRemaining / sprintDuration;
                sprintBar.transform.localScale = new Vector3(sprintRemainingPercent, 1f, 1f);
            }
        }

        #endregion

        #region Crouch

        if (enableCrouch)
        {
            isCrouched = !Mathf.Approximately(standingHeight, currentHeight);
            Crouch();
        }

        #endregion
        

        if (enableHeadBob)
        {
            HeadBob();
        }
    }

    void FixedUpdate()
    {
        #region Movement

        Vector2 playerInput = inputManager.MoveValue;

        if (inputManager.DisableAllMovement) return;
        
        stepsSinceLastGrounded += 1;
        velocity = rb.velocity;
        if (IsGrounded || SnapToGround())
        {
            stepsSinceLastGrounded = 0;
            contactNormal.Normalize();
        }

        Sprint();
        desiredVelocity = new Vector3(playerInput.x, 0f, playerInput.y) * currentSpeed;
        AdjustVelocity();
        rb.velocity = velocity;
        ClearState();
        #endregion
    }
    
    // Sets isGrounded based on a raycast sent straight down from the player object

    #region GroundCheck

    private void OnCollisionEnter(Collision collision)
    {
        EvaluateCollision(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        EvaluateCollision(collision);
    }

    private void EvaluateCollision(Collision collision)
    {
        for (int i = 0; i < collision.contactCount; i++)
        {
            Vector3 normal = collision.GetContact(i).normal;
            if (normal.y >= minGroundDotProduct)
            {
                groundContactCount += 1;
                contactNormal += normal;
            }
        }
    }

    #endregion

    private void Jump()
    {
        if (!enableJump || !IsGrounded) return;
        
        // Adds force to the player rigidbody to jump
        if (IsGrounded && !isCrouched)
        {
            rb.AddForce(0f, jumpPower, 0f, ForceMode.Impulse);
            groundContactCount = 0;
        }

        // When crouched and using toggle system, will uncrouch for a jump
        if (isCrouched && !holdToCrouch)
        {
            Crouch();
        }
    }

    private void Sprint()
    {
        if (Math.Abs(velocity.x - 0.01f) > 0.04f || Math.Abs(velocity.z - 0.01f) > 0.04f && IsGrounded)
        {
            isWalking = true;
        }
        else
        {
            isWalking = false;
        }

        if (enableSprint && inputManager.IsSprinting && sprintRemaining > 0f && !isSprintCooldown &&
            !isCrouched)
        {
            currentSpeed = sprintSpeed;

            if (Math.Abs(velocity.x - 0.01f) > 0.04f || Math.Abs(velocity.z - 0.01f) > 0.04f)
            {
                isSprinting = true;

                if (hideBarWhenFull && !unlimitedSprint)
                {
                    sprintBarCG.alpha += 5 * Time.deltaTime;
                }
            }
        }
        else
        {
            if (!isCrouched)
            {
                currentSpeed = walkSpeed;
            }

            isSprinting = false;

            if (hideBarWhenFull && Mathf.Approximately(sprintRemaining, sprintDuration))
            {
                sprintBarCG.alpha -= 3 * Time.deltaTime;
            }
        }
    }

    private void Crouch()
    {
        float targetHeight = inputManager.IsCrouching ? crouchHeight : standingHeight;
        currentSpeed = inputManager.IsCrouching ? crouchSpeed : walkSpeed;

        if (isCrouched && !inputManager.IsCrouching)
        {
            targetHeight = AdjustForCeiling(targetHeight);
        }

        if (!Mathf.Approximately(targetHeight, currentHeight))
        {
            UpdateHeight(targetHeight);
        }
    }

    private float AdjustForCeiling(float targetHeight)
    {
        Vector3 castOrigin = transform.position + new Vector3(0, currentHeight / 2, 0);
        if (Physics.Raycast(castOrigin, Vector3.up, out RaycastHit hit, 0.8f))
        {
            float distanceToCeiling = hit.point.y - castOrigin.y;
            targetHeight = Mathf.Max(currentHeight + distanceToCeiling - 0.1f, crouchHeight);
            currentSpeed = crouchSpeed;
        }

        return targetHeight;
    }

    private bool SnapToGround()
    {
        if (stepsSinceLastGrounded > 1 || velocity.magnitude > groundSnapSpeed)
        {
            return false;
        }

        if (!Physics.Raycast(rb.position, Vector3.down, out RaycastHit hit, probeDistance, probeMask) ||
            hit.normal.y < minGroundDotProduct)
        {
            return false;
        }

        groundContactCount = 1;
        contactNormal = hit.normal;

        float dot = Vector3.Dot(velocity, hit.normal);
        if (dot > 0)
        {
            velocity = (velocity - hit.normal * dot).normalized * velocity.magnitude;
        }

        return true;
    }

    private void UpdateHeight(float targetHeight)
    {
        // Check if the current height is within 0.01f of the target height
        currentHeight = Mathf.Abs(currentHeight - targetHeight) < 0.1f ? targetHeight :
            Mathf.Lerp(currentHeight, targetHeight, crouchLerpSpeed * Time.deltaTime);

        // Adjust camera height based on current height
        Vector3 halfHeightDifference = new Vector3(0, (standingHeight - currentHeight) / 2, 0);
        Vector3 newCameraHeight = standingCameraHeight - halfHeightDifference;
        playerVirtualCamera.transform.localPosition = newCameraHeight;

        // Adjust player collider height
        playerCollider.height = currentHeight;

        // Ensure player stays grounded
        if (Physics.Raycast(rb.position, Vector3.down, out RaycastHit hit, probeDistance, probeMask))
        {
            float distanceToGround = hit.distance - playerCollider.height / 2;
            if (Mathf.Abs(distanceToGround) > 0.001f)
            {
                rb.MovePosition(rb.position - new Vector3(0, distanceToGround, 0));
            }
        }

        // Adjust prop collider scale
        float colliderScale, colliderY;
        if (Mathf.Approximately(targetHeight, crouchHeight))
        {
            colliderScale = 0.3f;
            colliderY = -0.3f;
        }
        else
        {
            colliderScale = 0.6f;
            colliderY = -0.5f;
        }

        propCollider.transform.localScale =
            Vector3.Lerp(propCollider.transform.localScale, new Vector3(1, colliderScale, 1), crouchLerpSpeed * Time.deltaTime);
        propCollider.transform.localPosition =
            Vector3.Lerp(propCollider.transform.localPosition, new Vector3(0, colliderY, 0), crouchLerpSpeed * Time.deltaTime);
    }


    private Vector3 ProjectOnContactPlane(Vector3 vector)
    {
        return vector - contactNormal * Vector3.Dot(vector, contactNormal);
    }

    private void AdjustVelocity()
    {
        Vector3 right = new Vector3(playerVirtualCamera.transform.right.x, 0, playerVirtualCamera.transform.right.z);
        Vector3 forward = new Vector3(playerVirtualCamera.transform.forward.x, 0, playerVirtualCamera.transform.forward.z);

        Vector3 xAxis = ProjectOnContactPlane(right).normalized;
        Vector3 zAxis = ProjectOnContactPlane(forward).normalized;

        float currentX = Vector3.Dot(velocity, xAxis);
        float currentZ = Vector3.Dot(velocity, zAxis);

        float acceleration = IsGrounded ? maxAcceleration : airControl;
        float maxSpeedChange = acceleration * Time.deltaTime;

        float newX =
            Mathf.MoveTowards(currentX, desiredVelocity.x, maxSpeedChange);
        float newZ =
            Mathf.MoveTowards(currentZ, desiredVelocity.z, maxSpeedChange);

        velocity += xAxis * (newX - currentX) + zAxis * (newZ - currentZ);
    }

    void ClearState()
    {
        groundContactCount = 0;
        contactNormal = Vector3.zero;
    }

    private void HeadBob()
    {
        if (isWalking)
        {
            // Calculates HeadBob speed during sprint
            if (isSprinting)
            {
                timer += Time.deltaTime * (bobSpeed + sprintSpeed);
            }
            // Calculates HeadBob speed during crouched movement
            else if (isCrouched)
            {
                timer += Time.deltaTime * (bobSpeed * 0.5f);
            }
            // Calculates HeadBob speed during walking
            else
            {
                timer += Time.deltaTime * bobSpeed;
            }

            // Applies HeadBob movement
            joint.localPosition = new Vector3(jointOriginalPos.x + Mathf.Sin(timer) * bobAmount.x,
                jointOriginalPos.y + Mathf.Sin(timer) * bobAmount.y,
                jointOriginalPos.z + Mathf.Sin(timer) * bobAmount.z);
        }
        else
        {
            // Resets when play stops moving
            timer = 0;
            joint.localPosition = new Vector3(
                Mathf.Lerp(joint.localPosition.x, jointOriginalPos.x, Time.deltaTime * bobSpeed),
                Mathf.Lerp(joint.localPosition.y, jointOriginalPos.y, Time.deltaTime * bobSpeed),
                Mathf.Lerp(joint.localPosition.z, jointOriginalPos.z, Time.deltaTime * bobSpeed));
        }
    }

    private void OnEnable()
    {
       inputManager.Jump += Jump;
    }

    private void OnDisable()
    {
        inputManager.Jump -= Jump;

    }
}

// Custom Editor
#if UNITY_EDITOR
[CustomEditor(typeof(FirstPersonController)), InitializeOnLoadAttribute]
public class FirstPersonControllerEditor : Editor
{
    FirstPersonController fpc;
    SerializedObject SerFPC;

    private void OnEnable()
    {
        fpc = (FirstPersonController)target;
        SerFPC = new SerializedObject(fpc);
    }

    public override void OnInspectorGUI()
    {
        SerFPC.Update();

        EditorGUILayout.Space();
        GUILayout.Label("Modular First Person Controller",
            new GUIStyle(GUI.skin.label)
                { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 16 });
        EditorGUILayout.Space();
        
        
        #region Camera Setup

        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Label("Camera Setup",
            new GUIStyle(GUI.skin.label)
                { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 13 },
            GUILayout.ExpandWidth(true));
        EditorGUILayout.Space();
        
        fpc.playerVirtualCamera = (CinemachineVirtualCamera)EditorGUILayout.ObjectField(
            new GUIContent("Camera", "Camera attached to the controller."), fpc.playerVirtualCamera, typeof(CinemachineVirtualCamera), true);
        fpc.fov = EditorGUILayout.Slider(
            new GUIContent("Field of View", "The camera’s view angle. Changes the player camera directly."), fpc.fov,
            fpc.zoomFOV, 179f);

        fpc.invertCamera = EditorGUILayout.ToggleLeft(
            new GUIContent("Invert Camera Rotation", "Inverts the up and down movement of the camera."),
            fpc.invertCamera);
        fpc.maxLookAngle =
            EditorGUILayout.Slider(
                new GUIContent("Max Look Angle", "Determines the max and min angle the player camera is able to look."),
                fpc.maxLookAngle, 40, 90);
        GUI.enabled = true;

        EditorGUILayout.Space();

        #region Camera Zoom Setup

        GUILayout.Label("Zoom",
            new GUIStyle(GUI.skin.label)
                { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold, fontSize = 13 },
            GUILayout.ExpandWidth(true));

        fpc.enableZoom = EditorGUILayout.ToggleLeft(
            new GUIContent("Enable Zoom", "Determines if the player is able to zoom in while playing."),
            fpc.enableZoom);

        GUI.enabled = fpc.enableZoom;
        fpc.holdToZoom = EditorGUILayout.ToggleLeft(
            new GUIContent("Hold to Zoom",
                "Requires the player to hold the zoom key instead if pressing to zoom and unzoom."), fpc.holdToZoom);
        fpc.zoomFOV =
            EditorGUILayout.Slider(new GUIContent("Zoom FOV", "Determines the field of view the camera zooms to."),
                fpc.zoomFOV, .1f, fpc.fov);
        fpc.zoomStepTime =
            EditorGUILayout.Slider(
                new GUIContent("Step Time", "Determines how fast the FOV transitions while zooming in."),
                fpc.zoomStepTime, .1f, 10f);
        GUI.enabled = true;

        #endregion

        #endregion

        #region Movement Setup

        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Label("Movement Setup",
            new GUIStyle(GUI.skin.label)
                { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 13 },
            GUILayout.ExpandWidth(true));
        EditorGUILayout.Space();

        fpc.playerCanMove = EditorGUILayout.ToggleLeft(
            new GUIContent("Enable Player Movement", "Determines if the player is allowed to move."),
            fpc.playerCanMove);

        GUI.enabled = fpc.playerCanMove;
        fpc.walkSpeed =
            EditorGUILayout.Slider(
                new GUIContent("Walk Speed", "Determines how fast the player will move while walking."), fpc.walkSpeed,
                .1f, fpc.sprintSpeed);
        fpc.maxAcceleration =
            EditorGUILayout.Slider(new GUIContent("Acceleration", "Determines how fast the player will accelerate."),
                fpc.maxAcceleration, .1f, 100f);
        GUI.enabled = true;

        fpc.maxGroundAngle =
            EditorGUILayout.Slider(
                new GUIContent("Max Ground Angel", "Determines how steep slopes the player can walk up."),
                fpc.maxGroundAngle, 0f, 75f);
        fpc.groundSnapSpeed =
            EditorGUILayout.Slider(
                new GUIContent("Ground Snap Speed", "Determines how fast the player will snap to the ground."),
                fpc.groundSnapSpeed, 0f, 400f);

        fpc.probeDistance =
            EditorGUILayout.Slider(new GUIContent("Prob Distance", "How long the snap to ground ray should be"),
                fpc.probeDistance, 1f, 3f);

        fpc.probeMask = EditorGUILayout.MaskField(new GUIContent("Probe Mask",
            "Determines what layer the player should consider ground"), fpc.probeMask, InternalEditorUtility.layers);

        EditorGUILayout.Space();

        #region Sprint

        GUILayout.Label("Sprint",
            new GUIStyle(GUI.skin.label)
                { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold, fontSize = 13 },
            GUILayout.ExpandWidth(true));

        fpc.enableSprint = EditorGUILayout.ToggleLeft(
            new GUIContent("Enable Sprint", "Determines if the player is allowed to sprint."), fpc.enableSprint);

        GUI.enabled = fpc.enableSprint;
        fpc.unlimitedSprint = EditorGUILayout.ToggleLeft(
            new GUIContent("Unlimited Sprint",
                "Determines if 'Sprint Duration' is enabled. Turning this on will allow for unlimited sprint."),
            fpc.unlimitedSprint);
        fpc.sprintSpeed =
            EditorGUILayout.Slider(
                new GUIContent("Sprint Speed", "Determines how fast the player will move while sprinting."),
                fpc.sprintSpeed, fpc.walkSpeed, 20f);

        //GUI.enabled = !fpc.unlimitedSprint;
        fpc.sprintDuration =
            EditorGUILayout.Slider(
                new GUIContent("Sprint Duration",
                    "Determines how long the player can sprint while unlimited sprint is disabled."),
                fpc.sprintDuration, 1f, 20f);
        fpc.sprintCooldown =
            EditorGUILayout.Slider(
                new GUIContent("Sprint Cooldown",
                    "Determines how long the recovery time is when the player runs out of sprint."), fpc.sprintCooldown,
                .1f, fpc.sprintDuration);
        //GUI.enabled = true;

        fpc.sprintFOV =
            EditorGUILayout.Slider(
                new GUIContent("Sprint FOV", "Determines the field of view the camera changes to while sprinting."),
                fpc.sprintFOV, fpc.fov, 179f);
        fpc.sprintFOVStepTime =
            EditorGUILayout.Slider(
                new GUIContent("Step Time", "Determines how fast the FOV transitions while sprinting."),
                fpc.sprintFOVStepTime, .1f, 20f);

        fpc.useSprintBar = EditorGUILayout.ToggleLeft(
            new GUIContent("Use Sprint Bar", "Determines if the default sprint bar will appear on screen."),
            fpc.useSprintBar);

        // Only displays sprint bar options if sprint bar is enabled
        if (fpc.useSprintBar)
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.BeginHorizontal();
            fpc.hideBarWhenFull = EditorGUILayout.ToggleLeft(
                new GUIContent("Hide Full Bar",
                    "Hides the sprint bar when sprint duration is full, and fades the bar in when sprinting. Disabling this will leave the bar on screen at all times when the sprint bar is enabled."),
                fpc.hideBarWhenFull);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(new GUIContent("Bar BG", "Object to be used as sprint bar background."));
            fpc.sprintBarBG = (Image)EditorGUILayout.ObjectField(fpc.sprintBarBG, typeof(Image), true);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(new GUIContent("Bar", "Object to be used as sprint bar foreground."));
            fpc.sprintBar = (Image)EditorGUILayout.ObjectField(fpc.sprintBar, typeof(Image), true);
            EditorGUILayout.EndHorizontal();


            EditorGUILayout.BeginHorizontal();
            fpc.sprintBarWidthPercent = EditorGUILayout.Slider(
                new GUIContent("Bar Width", "Determines the width of the sprint bar."), fpc.sprintBarWidthPercent, .1f,
                .5f);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            fpc.sprintBarHeightPercent = EditorGUILayout.Slider(
                new GUIContent("Bar Height", "Determines the height of the sprint bar."), fpc.sprintBarHeightPercent,
                .001f, .025f);
            EditorGUILayout.EndHorizontal();
            EditorGUI.indentLevel--;
        }

        GUI.enabled = true;

        EditorGUILayout.Space();

        #endregion

        #region Jump

        GUILayout.Label("Jump",
            new GUIStyle(GUI.skin.label)
                { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold, fontSize = 13 },
            GUILayout.ExpandWidth(true));

        fpc.enableJump =
            EditorGUILayout.ToggleLeft(new GUIContent("Enable Jump", "Determines if the player is allowed to jump."),
                fpc.enableJump);

        GUI.enabled = fpc.enableJump;
        fpc.jumpPower =
            EditorGUILayout.Slider(new GUIContent("Jump Power", "Determines how high the player will jump."),
                fpc.jumpPower, .1f, 20f);
        GUI.enabled = true;

        EditorGUILayout.Space();

        #endregion

        #region Crouch

        GUILayout.Label("Crouch",
            new GUIStyle(GUI.skin.label)
                { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold, fontSize = 13 },
            GUILayout.ExpandWidth(true));

        fpc.enableCrouch = EditorGUILayout.ToggleLeft(
            new GUIContent("Enable Crouch", "Determines if the player is allowed to crouch."), fpc.enableCrouch);

        GUI.enabled = fpc.enableCrouch;
        fpc.holdToCrouch = EditorGUILayout.ToggleLeft(
            new GUIContent("Hold To Crouch",
                "Requires the player to hold the crouch key instead if pressing to crouch and uncrouch."),
            fpc.holdToCrouch);
        fpc.crouchHeight =
            EditorGUILayout.Slider(
                new GUIContent("Crouch Height", "Determines the y scale of the player object when crouched."),
                fpc.crouchHeight, 1f, 2f);
        fpc.crouchLerpSpeed =
            EditorGUILayout.Slider(
                new GUIContent("Crouch Lerp Speed", "Determines the the speed the player will crouch."),
                fpc.crouchLerpSpeed, 0f, 15f);
        fpc.crouchSpeed =
            EditorGUILayout.Slider(
                new GUIContent("Crouch Speed",
                    "Determines the percent 'Walk Speed' is reduced by. 1 being no reduction, and .5 being half."),
                fpc.crouchSpeed, 0.5f, fpc.walkSpeed);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel(new GUIContent("Prop Collider",
            "The Collider that will interact with the props to that the player can't walk on top if them"));
        fpc.propCollider = (Collider)EditorGUILayout.ObjectField(fpc.propCollider, typeof(Collider), true);
        EditorGUILayout.EndHorizontal();
        GUI.enabled = true;

        #endregion

        #endregion

        #region Head Bob

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Label("Head Bob Setup",
            new GUIStyle(GUI.skin.label)
                { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 13 },
            GUILayout.ExpandWidth(true));
        EditorGUILayout.Space();

        fpc.enableHeadBob = EditorGUILayout.ToggleLeft(
            new GUIContent("Enable Head Bob", "Determines if the camera will bob while the player is walking."),
            fpc.enableHeadBob);


        GUI.enabled = fpc.enableHeadBob;
        fpc.joint = (Transform)EditorGUILayout.ObjectField(
            new GUIContent("Camera Joint", "Joint object position is moved while head bob is active."), fpc.joint,
            typeof(Transform), true);
        fpc.bobSpeed =
            EditorGUILayout.Slider(new GUIContent("Speed", "Determines how often a bob rotation is completed."),
                fpc.bobSpeed, 1, 20);
        fpc.bobAmount = EditorGUILayout.Vector3Field(
            new GUIContent("Bob Amount", "Determines the amount the joint moves in both directions on every axes."),
            fpc.bobAmount);
        GUI.enabled = true;

        #endregion

        //Sets any changes from the prefab
        if (GUI.changed)
        {
            EditorUtility.SetDirty(fpc);
            Undo.RecordObject(fpc, "FPC Change");
            SerFPC.ApplyModifiedProperties();
        }
    }
}

#endif

