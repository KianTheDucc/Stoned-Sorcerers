using UnityEngine;

public class ChargeAttack : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject chargeProjectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private LayerMask groundMask;

    [Header("Input")]
    [SerializeField] private KeyCode attackKey = KeyCode.Mouse1;

    [Header("Charge")]
    [SerializeField] private float minChargeTime = 0.3f;  // minimum hold before it counts
    [SerializeField] private float maxChargeTime = 2f;    // full charge at this duration
    [SerializeField] private float cooldown = 1f;

    [Header("Charge Indicator (optional)")]
    [Tooltip("Assign a child GameObject (e.g. small glowing sphere) that scales up while charging.")]
    [SerializeField] private Transform chargeIndicator;
    [SerializeField] private float indicatorMaxScale = 1.5f;

    private float _chargeTimer;
    private bool _charging;
    private float _cooldownTimer;
    private Camera _cam;

    private void Awake()
    {
        _cam = Camera.main;
        if (chargeIndicator != null)
            chargeIndicator.localScale = Vector3.zero;
    }

    private void Update()
    {
        if (_cooldownTimer > 0f)
            _cooldownTimer -= Time.deltaTime;

        if (Input.GetKeyDown(attackKey) && _cooldownTimer <= 0f)
            StartCharge();

        if (_charging)
        {
            _chargeTimer += Time.deltaTime;
            UpdateIndicator();

            // Auto fire at max charge
            if (_chargeTimer >= maxChargeTime)
                Fire();
        }

        if (Input.GetKeyUp(attackKey) && _charging)
            Fire();
    }

    private void StartCharge()
    {
        _charging = true;
        _chargeTimer = 0f;
    }

    private void Fire()
    {
        _charging = false;

        if (chargeIndicator != null)
            chargeIndicator.localScale = Vector3.zero;

        // Don't fire if held less than minimum charge time
        if (_chargeTimer < minChargeTime) return;

        float chargeRatio = Mathf.Clamp01(_chargeTimer / maxChargeTime);

        // Aim toward cursor
        Ray ray = _cam.ScreenPointToRay(Input.mousePosition);
        Vector3 targetPoint = Physics.Raycast(ray, out RaycastHit hit, 100f, groundMask,
                                              QueryTriggerInteraction.Ignore)
                            ? hit.point
                            : ray.GetPoint(30f);

        Vector3 origin = firePoint != null ? firePoint.position : transform.position + Vector3.up;
        Vector3 direction = (targetPoint - origin).normalized;

        GameObject proj = Instantiate(chargeProjectilePrefab, origin, Quaternion.LookRotation(direction));
        ChargeProjectile cp = proj.GetComponent<ChargeProjectile>();
        if (cp != null) cp.Launch(direction, chargeRatio);

        _cooldownTimer = cooldown;
    }

    private void UpdateIndicator()
    {
        if (chargeIndicator == null) return;
        float t = Mathf.Clamp01(_chargeTimer / maxChargeTime);
        float scale = Mathf.Lerp(0f, indicatorMaxScale, t);
        chargeIndicator.localScale = Vector3.one * scale;
    }
}