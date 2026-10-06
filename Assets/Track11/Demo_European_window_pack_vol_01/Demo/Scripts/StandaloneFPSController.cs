using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.IO;

namespace Track11.EuropeanWindowPackVol01
{
/// <summary>
/// Lightweight first-person controller with no package or render-pipeline dependency beyond
/// Unity's CharacterController and the Input System package.
/// Attach this to the player root, not the camera. The camera should be a child transform.
/// </summary>
[RequireComponent(typeof(CharacterController))]
[DefaultExecutionOrder(-10)]
public sealed class StandaloneFPSController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private CharacterController controller;
    [SerializeField] private Transform cameraPivot;

    [Header("Look")]
    [SerializeField] private float mouseSensitivity = 0.12f;
    [SerializeField] private float pitchMin = -80f;
    [SerializeField] private float pitchMax = 80f;

    [Header("Move")]
    [SerializeField] private float walkSpeed = 3.5f;
    [SerializeField] private float slowWalkSpeed = 1.8f;
    [SerializeField] private float acceleration = 18f;
    [SerializeField] private float flySpeed = 8f;
    [SerializeField] private float flySlowSpeed = 4f;
    [SerializeField] private float flyVerticalSpeed = 6f;

    [Header("Jump & Gravity")]
    [SerializeField] private float gravity = -18f;
    [SerializeField] private float jumpHeight = 1.1f;
    [SerializeField] private float groundedStickForce = -2f;

    [Header("Crouch")]
    [SerializeField] private bool crouchIsHold = true;
    [SerializeField] private float standingHeight = 1.8f;
    [SerializeField] private float crouchHeight = 1.15f;
    [SerializeField] private float crouchSpeedMultiplier = 0.65f;
    [SerializeField] private float standingCameraLocalY = 1.6f;
    [SerializeField] private float crouchCameraLocalY = 1.0f;
    [SerializeField] private float crouchLerpSpeed = 16f;
    [SerializeField] private float headClearanceRadius = 0.18f;

    [Header("Input")]
    [SerializeField] private Key moveForwardKey = Key.W;
    [SerializeField] private Key moveBackwardKey = Key.S;
    [SerializeField] private Key moveLeftKey = Key.A;
    [SerializeField] private Key moveRightKey = Key.D;
    [SerializeField] private Key jumpKey = Key.Space;
    [SerializeField] private Key crouchKey = Key.LeftCtrl;
    [SerializeField] private Key alternateCrouchKey = Key.C;
    [SerializeField] private Key slowWalkKey = Key.LeftShift;
    [SerializeField] private Key flyToggleKey = Key.F;

    [Header("Cursor")]
    [SerializeField] private bool lockCursor = true;
    [SerializeField] private bool hideCursor = true;

    [Header("Screenshots")]
    [SerializeField] private Key screenshotKey = Key.F12;
    [SerializeField] private GameObject crosshairOverlay;
    [SerializeField] private string screenshotFolderName = "StandaloneFPSShots";
    [SerializeField] private int screenshotSuperSize = 1;

    private InputAction _move;
    private InputAction _look;
    private InputAction _jump;
    private InputAction _crouch;
    private InputAction _slowWalk;
    private InputAction _flyToggle;
    private InputAction _screenshot;

    private float _yaw;
    private float _pitch;
    private Vector3 _velocity;
    private Vector3 _wishVelocity;
    private bool _crouching;
    private bool _flying;
    private bool _screenshotInProgress;
    private float _targetHeight;
    private float _targetCameraY;

    private void Reset()
    {
        controller = GetComponent<CharacterController>();
        playerCamera = GetComponentInChildren<Camera>();
        cameraPivot = playerCamera != null ? playerCamera.transform : null;
    }

    private void Awake()
    {
        if (controller == null) controller = GetComponent<CharacterController>();
        if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();
        if (cameraPivot == null && playerCamera != null) cameraPivot = playerCamera.transform;

        _yaw = transform.eulerAngles.y;
        _pitch = 0f;

        ApplyStandingPreset(force: true);
        BuildInput();
        ApplyCursorState();
    }

    private void OnEnable() => EnableInput(true);
    private void OnDisable() => EnableInput(false);

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus) ApplyCursorState();
    }

    private void Update()
    {
        if (controller == null || playerCamera == null) return;

        if (_flyToggle.WasPressedThisFrame())
            SetFlying(!_flying);

        if (!_screenshotInProgress && _screenshot.WasPressedThisFrame())
            StartCoroutine(CaptureScreenshotWithoutCrosshair());

        if (_flying)
        {
            _crouching = false;
        }
        else
        {
            if (crouchIsHold) _crouching = _crouch.IsPressed();
            else if (_crouch.WasPressedThisFrame()) _crouching = !_crouching;
        }

        UpdateLook();
        UpdateCrouchTargets();
        ApplyHeightAndCamera();
        UpdateMovement();
    }

    private void LateUpdate()
    {
        if (lockCursor)
            ApplyCursorState();
    }

    private void BuildInput()
    {
        _move = new InputAction("Move", InputActionType.Value);
        _move.AddCompositeBinding("2DVector")
            .With("Up", $"<Keyboard>/{moveForwardKey.ToString().ToLowerInvariant()}")
            .With("Down", $"<Keyboard>/{moveBackwardKey.ToString().ToLowerInvariant()}")
            .With("Left", $"<Keyboard>/{moveLeftKey.ToString().ToLowerInvariant()}")
            .With("Right", $"<Keyboard>/{moveRightKey.ToString().ToLowerInvariant()}");

        _look = new InputAction("Look", InputActionType.Value);
        _look.AddBinding("<Mouse>/delta");

        _jump = new InputAction("Jump", InputActionType.Button);
        _jump.AddBinding($"<Keyboard>/{jumpKey.ToString().ToLowerInvariant()}");

        _crouch = new InputAction("Crouch", InputActionType.Button);
        _crouch.AddBinding($"<Keyboard>/{crouchKey.ToString().ToLowerInvariant()}");
        _crouch.AddBinding($"<Keyboard>/{alternateCrouchKey.ToString().ToLowerInvariant()}");

        _slowWalk = new InputAction("SlowWalk", InputActionType.Button);
        _slowWalk.AddBinding($"<Keyboard>/{slowWalkKey.ToString().ToLowerInvariant()}");

        _flyToggle = new InputAction("FlyToggle", InputActionType.Button);
        _flyToggle.AddBinding($"<Keyboard>/{flyToggleKey.ToString().ToLowerInvariant()}");

        _screenshot = new InputAction("Screenshot", InputActionType.Button);
        _screenshot.AddBinding($"<Keyboard>/{screenshotKey.ToString().ToLowerInvariant()}");
    }

    private void EnableInput(bool enabled)
    {
        if (_move == null) return;

        if (enabled)
        {
            _move.Enable();
            _look.Enable();
            _jump.Enable();
            _crouch.Enable();
            _slowWalk.Enable();
            _flyToggle.Enable();
            _screenshot.Enable();
        }
        else
        {
            _move.Disable();
            _look.Disable();
            _jump.Disable();
            _crouch.Disable();
            _slowWalk.Disable();
            _flyToggle.Disable();
            _screenshot.Disable();
        }
    }

    private void UpdateLook()
    {
        Vector2 lookDelta = _look.ReadValue<Vector2>();
        float dx = lookDelta.x * mouseSensitivity;
        float dy = lookDelta.y * mouseSensitivity;

        _yaw += dx;
        _pitch -= dy;
        _pitch = Mathf.Clamp(_pitch, pitchMin, pitchMax);

        transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
        if (cameraPivot != null)
            cameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
    }

    private void UpdateMovement()
    {
        Vector2 move = _move.ReadValue<Vector2>();
        Vector3 input = Vector3.ClampMagnitude(new Vector3(move.x, 0f, move.y), 1f);
        Vector3 world = transform.TransformDirection(input);

        if (_flying)
        {
            UpdateFlightMovement(world);
            return;
        }

        float speed = _slowWalk.IsPressed() ? slowWalkSpeed : walkSpeed;
        if (_crouching) speed *= crouchSpeedMultiplier;

        Vector3 desired = world * speed;
        _wishVelocity = Vector3.MoveTowards(_wishVelocity, desired, acceleration * Time.deltaTime);

        if (controller.isGrounded)
        {
            if (_velocity.y < 0f) _velocity.y = groundedStickForce;

            if (_jump.WasPressedThisFrame() && !_crouching)
            {
                float jumpVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                _velocity.y = jumpVelocity;
            }
        }

        _velocity.y += gravity * Time.deltaTime;
        Vector3 motion = _wishVelocity + Vector3.up * _velocity.y;
        controller.Move(motion * Time.deltaTime);
    }

    private void UpdateFlightMovement(Vector3 world)
    {
        float speed = _slowWalk.IsPressed() ? flySlowSpeed : flySpeed;
        Vector3 desired = world * speed;
        _wishVelocity = Vector3.MoveTowards(_wishVelocity, desired, acceleration * Time.deltaTime);

        float vertical = 0f;
        if (_jump.IsPressed()) vertical += 1f;
        if (_crouch.IsPressed()) vertical -= 1f;

        Vector3 motion = _wishVelocity + Vector3.up * (vertical * flyVerticalSpeed);
        controller.Move(motion * Time.deltaTime);
        _velocity = Vector3.zero;
    }

    private void UpdateCrouchTargets()
    {
        if (_crouching)
        {
            _targetHeight = crouchHeight;
            _targetCameraY = crouchCameraLocalY;
            return;
        }

        if (CanStandUp())
        {
            _targetHeight = standingHeight;
            _targetCameraY = standingCameraLocalY;
        }
        else
        {
            _targetHeight = crouchHeight;
            _targetCameraY = crouchCameraLocalY;
            _crouching = true;
        }
    }

    private bool CanStandUp()
    {
        float currentHeight = controller.height;
        float desiredHeight = standingHeight;
        if (desiredHeight <= currentHeight + 0.001f) return true;

        Vector3 origin = transform.position + Vector3.up * (currentHeight * 0.5f);
        float castDistance = desiredHeight - currentHeight;
        return !Physics.SphereCast(origin, headClearanceRadius, Vector3.up, out _, castDistance, ~0, QueryTriggerInteraction.Ignore);
    }

    private void ApplyHeightAndCamera()
    {
        float newHeight = Mathf.Lerp(controller.height, _targetHeight, crouchLerpSpeed * Time.deltaTime);
        controller.height = newHeight;
        controller.center = new Vector3(0f, newHeight * 0.5f, 0f);

        if (cameraPivot != null)
        {
            Vector3 localPos = cameraPivot.localPosition;
            localPos.y = Mathf.Lerp(localPos.y, _targetCameraY, crouchLerpSpeed * Time.deltaTime);
            cameraPivot.localPosition = localPos;
        }
    }

    private void ApplyStandingPreset(bool force)
    {
        _targetHeight = standingHeight;
        _targetCameraY = standingCameraLocalY;

        if (force)
        {
            controller.height = standingHeight;
            controller.center = new Vector3(0f, standingHeight * 0.5f, 0f);

            if (cameraPivot != null)
            {
                Vector3 localPos = cameraPivot.localPosition;
                localPos.y = standingCameraLocalY;
                cameraPivot.localPosition = localPos;
            }
        }
    }

    private void ApplyCursorState()
    {
        if (!lockCursor) return;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = !hideCursor;
    }

    private void SetFlying(bool flying)
    {
        _flying = flying;
        _velocity = Vector3.zero;
        _wishVelocity = Vector3.zero;

        if (_flying)
            _crouching = false;
    }

    private IEnumerator CaptureScreenshotWithoutCrosshair()
    {
        _screenshotInProgress = true;

        bool restoreCrosshair = crosshairOverlay != null && crosshairOverlay.activeSelf;
        if (restoreCrosshair)
            crosshairOverlay.SetActive(false);

        yield return new WaitForEndOfFrame();

        string folderPath = Path.Combine(Application.persistentDataPath, screenshotFolderName);
        Directory.CreateDirectory(folderPath);

        string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string filePath = Path.Combine(folderPath, $"screenshot_{timestamp}.png");
        ScreenCapture.CaptureScreenshot(filePath, Mathf.Max(1, screenshotSuperSize));

        yield return null;

        if (restoreCrosshair && crosshairOverlay != null)
            crosshairOverlay.SetActive(true);

        Debug.Log($"[StandaloneFPSController] Screenshot saved: {filePath}", this);
        _screenshotInProgress = false;
    }
}
}
