using UnityEngine;


public class PlayerCombat : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private LayerMask groundMask;

    [Header("Attack")]
    [SerializeField] private KeyCode attackKey = KeyCode.Mouse0;
    [SerializeField] private float cooldown = 0.4f;

    private float _cooldownTimer;
    private Camera _cam;

    private void Awake()
    {
        _cam = Camera.main;
    }

    private void Update()
    {
        if (_cooldownTimer > 0f)
            _cooldownTimer -= Time.deltaTime;

        if (Input.GetKeyDown(attackKey) && _cooldownTimer <= 0f)
            TryFire();
    }

    private void TryFire()
    {
        // Raycast from the camera through the mouse position onto the ground
        Ray ray = _cam.ScreenPointToRay(Input.mousePosition);
        Vector3 targetPoint;

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundMask,
                            QueryTriggerInteraction.Ignore))
            targetPoint = hit.point;
        else
            // Fallback — project onto a flat plane at the player's height
            targetPoint = ray.GetPoint(30f);

        // Fire direction — straight toward the cursor hit point in 3D
        Vector3 origin = firePoint != null ? firePoint.position : transform.position + Vector3.up;
        Vector3 direction = (targetPoint - origin).normalized;

        // Rotate player mesh to face the target
        if (direction != Vector3.zero)
        {
            //RPGPlayerController pc = GetComponent<RPGPlayerController>();
            //// We rotate the fire point's parent (meshRoot) directly via the direction
            //// If no meshRoot, rotate the whole transform
            //transform.rotation = Quaternion.LookRotation(direction);
        }

        GameObject proj = Instantiate(projectilePrefab, origin, Quaternion.LookRotation(direction));
        MagicProjectile mp = proj.GetComponent<MagicProjectile>();
        if (mp != null) mp.Launch(direction);

        _cooldownTimer = cooldown;
    }
    //public float health = 100f;
    //public void TakeDamage(float amount)
    //{
    //    health -= amount;
    //    if (health <= 0f)
    //        Destroy(this.gameObject);
    //}
}