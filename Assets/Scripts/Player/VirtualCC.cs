using UnityEngine;
using Cinemachine;

public class RPGCameraController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private CameraState cameraState;

    [Header("Mouse Sensitivity")]
    [SerializeField] private float xSensitivity = 2f;
    [SerializeField] private float ySensitivity = 1.5f;

    [Header("Vertical Look Limits")]
    [SerializeField] private float minPitch = -20f;
    [SerializeField] private float maxPitch = 60f;

    [Header("Freelook (hold RMB to orbit without rotating character)")]
    [SerializeField] private KeyCode freelookKey = KeyCode.Mouse1;

    [Header("Zoom")]
    [SerializeField] private float zoomSpeed = 5f;
    [SerializeField] private float zoomSmoothing = 8f;
    [SerializeField] private float minDistance = 1.5f;
    [SerializeField] private float maxDistance = 10f;
    [SerializeField] private CinemachineVirtualCamera virtualCamera;

    // ─── Private ──────────────────────────────────────────────────────────────
    private float _yaw;
    private float _pitch;
    private float _targetDistance;
    private float _currentDistance;
    private Cinemachine3rdPersonFollow _follow;

    private void Awake()
    {
        // Initialise yaw to face same direction as player
        if (player != null)
            _yaw = player.eulerAngles.y;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (virtualCamera != null)
        {
            _follow = virtualCamera.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
            if (_follow != null)
            {
                _targetDistance = _follow.CameraDistance;
                _currentDistance = _follow.CameraDistance;
            }
        }
    }

    private void Update()
    {
        HandleMouseLook();
        HandleZoom();
        ApplyTransform();

        // CameraYaw is published in HandleMouseLook conditionally based on freelook state
    }

    private void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * xSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * ySensitivity;

        bool freelooking = Input.GetKey(freelookKey);

        if (cameraState != null)
            cameraState.IsFreelooking = freelooking;

        // Always rotate camera with mouse
        _yaw += mouseX;
        _pitch -= mouseY;
        _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);

        // When not freelooking, publish yaw so player rotates with camera
        // When freelooking, stop publishing so player stays frozen while camera orbits
        if (cameraState != null && !freelooking)
            cameraState.CameraYaw = _yaw;
    }

    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
            _targetDistance = Mathf.Clamp(_targetDistance - scroll * zoomSpeed, minDistance, maxDistance);

        _currentDistance = Mathf.Lerp(_currentDistance, _targetDistance, zoomSmoothing * Time.deltaTime);

        if (_follow != null)
            _follow.CameraDistance = _currentDistance;
    }

    private void ApplyTransform()
    {
        if (player == null) return;

        // Pivot always sits on the player — never rotates the player transform
        transform.position = player.position;
        transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
    }
}