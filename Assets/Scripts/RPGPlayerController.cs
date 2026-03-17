using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(CharacterController))]
public class RPGPlayerController : MonoBehaviour
{
    // ─── Movement ─────────────────────────────────────────────────────────────
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 4f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float acceleration = 12f;
    [SerializeField] private float deceleration = 16f;
    [Tooltip("How fast the character rotates to face the camera direction.")]
    [SerializeField] private float rotationSpeed = 720f; // degrees per second
    [Tooltip("Assign your character's mesh/model child here. It rotates to face movement " +
             "direction while the root (followed by Cinemachine) stays stable.")]
    [SerializeField] private Transform meshRoot;

    // ─── Jumping & Gravity ────────────────────────────────────────────────────
    [Header("Jumping & Gravity")]
    [SerializeField] private float jumpHeight = 1.4f;
    [SerializeField] private float gravity = -22f;

    // ─── Ground Detection ─────────────────────────────────────────────────────
    [Header("Ground Detection")]
    [SerializeField] private float groundRadius = 0.28f;
    [SerializeField] private LayerMask groundMask = ~0;

    // ─── Stamina ──────────────────────────────────────────────────────────────
    [Header("Stamina")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float sprintDrainRate = 20f;
    [SerializeField] private float jumpStaminaCost = 15f;
    [SerializeField] private float staminaRegenRate = 12f;
    [SerializeField] private float staminaRegenDelay = 1.5f;
    [SerializeField] private float minStaminaToSprint = 10f;

    // Fires when stamina changes. Passes 0-1 normalised — wire to a UI slider
    public UnityEvent<float> OnStaminaChanged;

    // ─── Camera State ─────────────────────────────────────────────────────────
    [Header("Camera")]
    [SerializeField] private CameraState cameraState;

    // ─── Public State ─────────────────────────────────────────────────────────
    public float CurrentStamina { get; private set; }
    public bool IsSprinting { get; private set; }
    public bool IsGrounded { get; private set; }

    // ─── Private ──────────────────────────────────────────────────────────────
    private CharacterController _cc;
    private Vector3 _velocity;
    private float _verticalVelocity;
    private float _staminaRegenTimer;
    private float _lastStamina;
    private bool _sprintLocked;
    private Transform _groundCheck;

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();
        CurrentStamina = maxStamina;
        _lastStamina = maxStamina;

        var gc = new GameObject("_GroundCheck");
        gc.transform.SetParent(transform);
        gc.transform.localPosition = new Vector3(0f, -(_cc.height * 0.5f) + 0.05f, 0f);
        _groundCheck = gc.transform;
    }

    // Input state — written in Update, consumed in FixedUpdate
    private Vector2 _moveInput;
    private bool _jumpPressed;

    private void Update()
    {
        // Read input every frame so no presses are missed
        _moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (Input.GetButtonDown("Jump"))
            _jumpPressed = true;
    }

    private void FixedUpdate()
    {
        // Physics and movement in FixedUpdate for consistent simulation
        CheckGround();
        HandleStamina();
        HandleMovement();
        HandleJump();
        ApplyGravity();
        ApplyMotion();
    }

    private void LateUpdate()
    {
        // Rotation in LateUpdate — after Cinemachine has published the final yaw
        ApplyCharacterRotation();
    }

    // ─── Ground ───────────────────────────────────────────────────────────────

    private void CheckGround()
    {
        // Offset the check slightly downward so it overlaps the ground consistently
        // without flickering when the CharacterController is resting on a surface
        Vector3 origin = _groundCheck.position + Vector3.down * 0.05f;
        IsGrounded = Physics.CheckSphere(origin, groundRadius, groundMask,
                                         QueryTriggerInteraction.Ignore);
    }

    // ─── Stamina ──────────────────────────────────────────────────────────────

    private void HandleStamina()
    {
        bool sprintInput = Input.GetKey(KeyCode.LeftShift);

        if (_sprintLocked && CurrentStamina >= minStaminaToSprint)
            _sprintLocked = false;

        IsSprinting = sprintInput && IsGrounded && !_sprintLocked
                      && CurrentStamina > 0f && HasMovementInput();

        if (IsSprinting)
        {
            CurrentStamina -= sprintDrainRate * Time.deltaTime;
            if (CurrentStamina <= 0f) { CurrentStamina = 0f; _sprintLocked = true; }
            _staminaRegenTimer = staminaRegenDelay;
        }
        else
        {
            if (_staminaRegenTimer > 0f)
                _staminaRegenTimer -= Time.deltaTime;
            else if (CurrentStamina < maxStamina)
                CurrentStamina = Mathf.Min(CurrentStamina + staminaRegenRate * Time.deltaTime, maxStamina);
        }

        if (!Mathf.Approximately(CurrentStamina, _lastStamina))
        {
            OnStaminaChanged?.Invoke(CurrentStamina / maxStamina);
            _lastStamina = CurrentStamina;
        }
    }

    // ─── Movement ─────────────────────────────────────────────────────────────

    private void HandleMovement()
    {
        Vector3 inputDir = new Vector3(_moveInput.x, 0f, _moveInput.y).normalized;

        float targetSpeed = IsSprinting ? sprintSpeed : walkSpeed;

        // Move relative to camera yaw — forward (W) = camera's forward, right (D) = camera's right
        Vector3 moveDir = Vector3.zero;
        if (inputDir.magnitude >= 0.1f)
        {
            float cameraYaw = cameraState != null ? cameraState.CameraYaw : transform.eulerAngles.y;
            Vector3 camForward = Quaternion.Euler(0f, cameraYaw, 0f) * Vector3.forward;
            Vector3 camRight = Quaternion.Euler(0f, cameraYaw, 0f) * Vector3.right;
            moveDir = (camForward * inputDir.z + camRight * inputDir.x).normalized;
        }

        float rate = inputDir.magnitude >= 0.1f ? acceleration : deceleration;
        _velocity = Vector3.MoveTowards(_velocity, moveDir * targetSpeed, rate * Time.deltaTime);
    }

    // ─── Rotation ─────────────────────────────────────────────────────────────

    private void ApplyCharacterRotation()
    {
        bool freelooking = cameraState != null && cameraState.IsFreelooking;
        float cameraYaw = cameraState != null ? cameraState.CameraYaw : transform.eulerAngles.y;

        // Root snaps instantly to camera yaw — no damping, no lag
        transform.rotation = Quaternion.Euler(0f, cameraYaw, 0f);

        // Mesh also snaps instantly — faces movement direction when moving, camera when idle
        if (meshRoot != null)
        {
            Vector3 flatVelocity = new Vector3(_velocity.x, 0f, _velocity.z);
            if (flatVelocity.magnitude > 0.1f && !freelooking)
                meshRoot.rotation = Quaternion.LookRotation(flatVelocity);
            else
                meshRoot.rotation = Quaternion.Euler(0f, cameraYaw, 0f);
        }
    }

    // ─── Jump ─────────────────────────────────────────────────────────────────────

    private void HandleJump()
    {
        if (!_jumpPressed || !IsGrounded) return;
        _jumpPressed = false;
        if (CurrentStamina < jumpStaminaCost) return;

        _verticalVelocity = Mathf.Sqrt(2f * Mathf.Abs(gravity) * jumpHeight);
        CurrentStamina -= jumpStaminaCost;
        _staminaRegenTimer = staminaRegenDelay;
        OnStaminaChanged?.Invoke(CurrentStamina / maxStamina);
        _lastStamina = CurrentStamina;
    }

    // ─── Gravity ──────────────────────────────────────────────────────────────

    private void ApplyGravity()
    {
        if (IsGrounded && _verticalVelocity < 0f) _verticalVelocity = -8f;
        else _verticalVelocity += gravity * Time.deltaTime;
    }

    // ─── Final Move ───────────────────────────────────────────────────────────

    private void ApplyMotion()
    {
        _cc.Move((_velocity + Vector3.up * _verticalVelocity) * Time.deltaTime);
    }

    // ─── Public API ───────────────────────────────────────────────────────────

    public void RestoreStamina(float amount)
    {
        CurrentStamina = Mathf.Min(CurrentStamina + amount, maxStamina);
        OnStaminaChanged?.Invoke(CurrentStamina / maxStamina);
        _lastStamina = CurrentStamina;
    }

    public void DrainStamina(float amount)
    {
        CurrentStamina = Mathf.Max(CurrentStamina - amount, 0f);
        _staminaRegenTimer = staminaRegenDelay;
        OnStaminaChanged?.Invoke(CurrentStamina / maxStamina);
        _lastStamina = CurrentStamina;
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private bool HasMovementInput() =>
        _moveInput.magnitude > 0.1f;

    private void OnDrawGizmosSelected()
    {
        if (_groundCheck == null) return;
        Gizmos.color = IsGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(_groundCheck.position, groundRadius);
    }
}