using UnityEngine;

/// <summary>
/// Handles contact damage for enemies.
/// Uses OverlapSphere each frame during active contact rather than relying
/// on Rigidbody collision events which are unreliable against CharacterControllers.
/// </summary>
public class EnemyContactDamage : MonoBehaviour
{
    [Header("Contact Damage")]
    [SerializeField] private float damage = 8f;
    [SerializeField] private float knockbackForce = 6f;
    [SerializeField] private float contactRadius = 0.8f;
    [SerializeField] private LayerMask playerMask;

    public void CheckLungeContact()
    {
        // Called directly by SpiderAttack at moment of landing
        CheckContact();
    }

    private void CheckContact()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, contactRadius,
                                                playerMask, QueryTriggerInteraction.Ignore);
        foreach (var col in hits)
        {
            if (!col.CompareTag("Player")) continue;

            PlayerHealth health = col.GetComponentInParent<PlayerHealth>();
            if (health == null || health.IsInvincible) continue;

            Vector3 knockDir = (col.transform.position - transform.position).normalized;
            knockDir.y = 0.3f;
            knockDir.Normalize();

            health.TakeDamage(damage);
            health.ApplyKnockback(knockDir * knockbackForce);
            break;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, contactRadius);
    }
}