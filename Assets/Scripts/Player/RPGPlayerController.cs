using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(CharacterController))]
public class RPGPlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 4f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float acceleration = 12f;
    [SerializeField] private float deceleration = 16f;

    [Header("Jumping & Gravity")]
    [SerializeField] private float jumpHeight = 1.4f;
    [SerializeField] private float gravity = -22f;

    [Header("Ground Detection")]
    [SerializeField] private float groundRadius = 0.28f;
    [SerializeField] private LayerMask groundMask = ~0;

    [Header("Stamina")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float sprintDrainRate = 20f;
    [SerializeField] private float jumpStaminaCost = 15f;
    [SerializeField] private float staminaRegenRate = 12f;
    [SerializeField] private float staminaRegenDelay = 1.5f;
    [SerializeField] private float minStaminaToSprint = 10f;
    public UnityEvent<float> OnStaminaChanged;

    [Header("Camera")]
    [SerializeField] private CameraState cameraState;

    [Header("Visuals (optional)")]
    [Tooltip("Assign your mesh child here. It will rotate to face the camera.")]
    [SerializeField] private Transform meshRoot;
    [Tooltip("How smoothly the visual follows vertical movement. Higher = snappier.")]
    [SerializeField] private float verticalSmoothSpeed = 12f;

    public float CurrentStamina { get; private set; }
    public bool IsSprinting { get; private set; }
    public bool IsGrounded { get; private set; }

    private CharacterController _cc;
    private Vector3 _velocity;
    private float _smoothY;
    private float _verticalVelocity;
    private float _staminaRegenTimer;
    private float _lastStamina;
    private bool _sprintLocked;
    private Vector2 _moveInput;
    private bool _jumpPressed;
    private Transform _groundCheck;

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();
        CurrentStamina = maxStamina;
        _lastStamina = maxStamina;
        _smoothY = transform.position.y;

        var gc = new GameObject("_GroundCheck");
        gc.transform.SetParent(transform);
        gc.transform.localPosition = new Vector3(0f, -(_cc.height * 0.5f) + 0.05f, 0f);
        _groundCheck = gc.transform;
    }

    private void Update()
    {
        _moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (Input.GetButtonDown("Jump"))
            _jumpPressed = true;

        CheckGround();
        HandleStamina();
        HandleMovement();
        HandleJump();
        ApplyGravity();
        _cc.Move((_velocity + Vector3.up * _verticalVelocity) * Time.deltaTime);
    }

    private void LateUpdate()
    {
        bool freelooking = cameraState != null && cameraState.IsFreelooking;

        if (meshRoot != null)
        {
            if (!freelooking && cameraState != null)
            {
                // Always face the camera direction
                meshRoot.rotation = Quaternion.Euler(0f, cameraState.CameraYaw, 0f);
            }
            // During freelook — mesh stays frozen, camera orbits freely
        }
    }

    private void CheckGround()
    {
        Vector3 origin = _groundCheck.position + Vector3.down * 0.05f;
        IsGrounded = Physics.CheckSphere(origin, groundRadius, groundMask,
                                         QueryTriggerInteraction.Ignore);
    }

    private void HandleMovement()
    {
        float targetSpeed = IsSprinting ? sprintSpeed : walkSpeed;

        Transform cam = Camera.main != null ? Camera.main.transform : null;
        Vector3 forward = cam != null ? cam.forward : Vector3.forward;
        Vector3 right = cam != null ? cam.right : Vector3.right;

        forward.y = 0f; forward.Normalize();
        right.y = 0f; right.Normalize();

        Vector3 moveDir = (forward * _moveInput.y + right * _moveInput.x).normalized;

        float rate = _moveInput.magnitude >= 0.1f ? acceleration : deceleration;
        _velocity = Vector3.MoveTowards(_velocity, moveDir * targetSpeed, rate * Time.deltaTime);
    }

    private void HandleStamina()
    {
        bool sprintInput = Input.GetKey(KeyCode.LeftShift);

        if (_sprintLocked && CurrentStamina >= minStaminaToSprint)
            _sprintLocked = false;

        IsSprinting = sprintInput && IsGrounded && !_sprintLocked
                      && CurrentStamina > 0f && _moveInput.magnitude >= 0.1f;

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

    private void ApplyGravity()
    {
        if (IsGrounded && _verticalVelocity < 0f) _verticalVelocity = -8f;
        else _verticalVelocity += gravity * Time.deltaTime;
    }

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

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Rigidbody rb = hit.collider.attachedRigidbody;
        if (rb == null || rb.isKinematic) return;

        // Don't push objects below the player (e.g. ground)
        if (hit.moveDirection.y < -0.3f) return;

        // Push force is based on the direction the player is moving
        Vector3 pushDir = new Vector3(hit.moveDirection.x, 0f, hit.moveDirection.z);
        rb.AddForce(pushDir * walkSpeed, ForceMode.Force);
    }

    private void OnDrawGizmosSelected()
    {
        if (_groundCheck == null) return;
        Gizmos.color = IsGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(_groundCheck.position + Vector3.down * 0.05f, groundRadius);
    }
}   