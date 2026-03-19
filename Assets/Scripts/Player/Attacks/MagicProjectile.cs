using UnityEngine;


[RequireComponent(typeof(Rigidbody))]
public class MagicProjectile : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 18f;
    [SerializeField] private float lifetime = 4f;   // seconds before auto-destroy

    [Header("Combat")]
    [SerializeField] private float damage = 10f;
    [SerializeField] private LayerMask hitMask = ~0;

    [Header("Feel")]
    [SerializeField] private GameObject hitEffectPrefab;   // particle burst on impact
    [SerializeField] private float hitEffectDuration = 1f;
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0f, 0.1f, 0.15f, 1f);

    private Rigidbody _rb;
    private float _age;
    private bool _launched;
    private Vector3 _targetScale;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = false;
        _rb.isKinematic = false;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // Store the prefab's intended scale, then start from zero for the launch pop
        _targetScale = transform.localScale;
        transform.localScale = Vector3.zero;
    }

    public void Launch(Vector3 direction)
    {
        _rb.linearVelocity = direction * speed;
        _launched = true;
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        if (!_launched) return;

        // Scale up quickly on launch using the curve
        _age += Time.deltaTime;
        float t = Mathf.Clamp01(_age / 0.15f);
        transform.localScale = _targetScale * scaleCurve.Evaluate(t);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Ignore collisions with things not in the hit mask
        if ((hitMask.value & (1 << other.gameObject.layer)) == 0) return;

        // Deal damage if the target has a health component
        // Replace IHealth with whatever health interface/component you use
        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        damageable?.TakeDamage(damage);

        SpawnHitEffect();
        Destroy(gameObject);
    }

    private void SpawnHitEffect()
    {
        if (hitEffectPrefab == null) return;
        GameObject fx = Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
        Destroy(fx, hitEffectDuration);
    }
}

/// <summary>
/// Implement this on any entity that can take damage.
/// Example:
///     public class EnemyHealth : MonoBehaviour, IDamageable {
///         public void TakeDamage(float amount) => health -= amount;
///     }
/// </summary>
public interface IDamageable
{
    void TakeDamage(float amount);
}