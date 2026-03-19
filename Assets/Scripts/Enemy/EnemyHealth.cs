using UnityEngine;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    public float health = 100f;
    public void TakeDamage(float amount)
    {
        health -= amount;
        if (health <= 0f)
            Destroy(this.gameObject);
    }
}
