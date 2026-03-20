using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ChargeProjectile : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 14f;
    [SerializeField] private float lifetime = 5f;

    [Header("Explosion")]
    [SerializeField] private float minDamage = 10f;
    [SerializeField] private float maxDamage = 60f;
    [SerializeField] private float minExplosionRadius = 1f;
    [SerializeField] private float maxExplosionRadius = 6f;
    [SerializeField] private float minProjectileScale = 0.2f;
    [SerializeField] private float maxProjectileScale = 1.2f;
    [SerializeField] private LayerMask hitMask = ~0;

    [Header("Effects")]
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private float hitEffectDuration = 1.5f;

    private Rigidbody _rb;
    private float _chargeRatio;
    private bool _exploded;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = false;
        _rb.isKinematic = false;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    public void Launch(Vector3 direction, float chargeRatio)
    {
        _chargeRatio = chargeRatio;

        // Scale the projectile visually based on charge
        float scale = Mathf.Lerp(minProjectileScale, maxProjectileScale, chargeRatio);
        transform.localScale = Vector3.one * scale;

        _rb.linearVelocity = direction * speed;
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_exploded) return;

        // Don't explode on the player
        if (other.CompareTag("Player")) return;

        Explode();
    }

    private void Explode()
    {
        _exploded = true;

        float explosionRadius = Mathf.Lerp(minExplosionRadius, maxExplosionRadius, _chargeRatio);
        float damage = Mathf.Lerp(minDamage, maxDamage, _chargeRatio);

        // AOE — find all damageable targets in explosion radius
        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius, hitMask,
                                                QueryTriggerInteraction.Ignore);
        foreach (var col in hits)
        {
            if (col.CompareTag("Player")) continue;
            IDamageable damageable = col.GetComponentInParent<IDamageable>();
            damageable?.TakeDamage(damage);
        }

        // Spawn hit effect scaled to charge
        if (hitEffectPrefab != null)
        {
            GameObject fx = Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
            float fxScale = Mathf.Lerp(0.5f, 2f, _chargeRatio);
            fx.transform.localScale = Vector3.one * fxScale;
            Destroy(fx, hitEffectDuration);
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        // Preview max explosion radius in scene view
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, maxExplosionRadius);
    }
}