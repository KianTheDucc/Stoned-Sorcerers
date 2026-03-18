using UnityEngine;
using Cinemachine;


[RequireComponent(typeof(CinemachineFreeLook))]
public class RPGCinemachineInput : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private CameraState cameraState;      // shared ScriptableObject

    [Header("Mouse Sensitivity")]
    [SerializeField] private float xSensitivity = 300f;
    [SerializeField] private float ySensitivity = 2f;

    [Header("Freelook (hold to orbit without turning character)")]
    [SerializeField] private KeyCode freelookKey = KeyCode.Mouse1;
    [SerializeField] private float returnDelay = 0.5f;
    [SerializeField] private float returnPitch = 0.45f;
    [SerializeField] private float returnPitchSpeed = 3f;

    [Header("Zoom")]
    [SerializeField] private float zoomSpeed = 1f;
    [SerializeField] private float zoomSmoothing = 8f;
    [SerializeField] private float minZoomMultiplier = 0.3f;
    [SerializeField] private float maxZoomMultiplier = 2.0f;

    private CinemachineFreeLook _freeLook;
    private float[] _baseRadii = new float[3];
    private float _zoomMultiplier = 1f;
    private float _targetZoom = 1f;
    private float _returnTimer;

    private void Awake()
    {
        _freeLook = GetComponent<CinemachineFreeLook>();

        for (int i = 0; i < 3; i++)
            _baseRadii[i] = _freeLook.m_Orbits[i].m_Radius;

        // Disable Cinemachine's built-in axis smoothing and speed clamping —
        // we drive the axes ourselves so these just cause the jittery catch-up effect
        _freeLook.m_XAxis.m_MaxSpeed = 0f;
        _freeLook.m_YAxis.m_MaxSpeed = 0f;
        _freeLook.m_XAxis.m_AccelTime = 0f;
        _freeLook.m_YAxis.m_AccelTime = 0f;
        _freeLook.m_XAxis.m_DecelTime = 0f;
        _freeLook.m_YAxis.m_DecelTime = 0f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // IMPORTANT: FreeLook must use Update (not FixedUpdate) since we drive it with mouse input
        // Set this in the Inspector too: FreeLook → Update Method → Update
    }

    private void Update()
    {
        HandleMouseLook();
        HandleZoom();
    }

    private void LateUpdate()
    {
        // Read AFTER Cinemachine has finished moving the camera this frame
        if (cameraState != null && Camera.main != null)
            cameraState.CameraYaw = Camera.main.transform.eulerAngles.y;
    }

    // Keeping camera on Update — mouse input is per-frame and must never run in FixedUpdate

    private void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * xSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * ySensitivity * Time.deltaTime;

        // Mouse always rotates the camera
        _freeLook.m_XAxis.Value += mouseX;
        _freeLook.m_YAxis.Value -= mouseY;
        _freeLook.m_YAxis.Value = Mathf.Clamp01(_freeLook.m_YAxis.Value);

        bool mouseMoving = Mathf.Abs(Input.GetAxis("Mouse X")) > 0.01f
                        || Mathf.Abs(Input.GetAxis("Mouse Y")) > 0.01f;

        if (Input.GetKey(freelookKey))
        {
            // Freelook: camera orbits but character does NOT rotate
            _returnTimer = returnDelay;
            if (cameraState != null)
                cameraState.IsFreelooking = true;
        }
        else
        {
            if (cameraState != null)
                cameraState.IsFreelooking = false;

            // No auto-recentering — camera stays exactly where the mouse leaves it
        }
    }

    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
            _targetZoom = Mathf.Clamp(_targetZoom - scroll * zoomSpeed,
                                      minZoomMultiplier, maxZoomMultiplier);

        _zoomMultiplier = Mathf.Lerp(_zoomMultiplier, _targetZoom, zoomSmoothing * Time.deltaTime);

        for (int i = 0; i < 3; i++)
            _freeLook.m_Orbits[i].m_Radius = _baseRadii[i] * _zoomMultiplier;
    }
}