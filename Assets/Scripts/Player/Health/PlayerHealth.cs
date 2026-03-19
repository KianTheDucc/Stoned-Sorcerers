using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    public float maxHealth = 100f;
    [SerializeField] public float CurrentHealth = 100f;

    [Header("Knockback")]
    [SerializeField] private float knockbackDecay = 10f; // how quickly knockback slows down

    private CharacterController _cc;
    private Vector3 _knockbackVelocity;

    private void Awake()
    {
        CurrentHealth = maxHealth;
        _cc = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (_knockbackVelocity.magnitude > 0.1f)
        {
            _cc.Move(_knockbackVelocity * Time.deltaTime);
            _knockbackVelocity = Vector3.MoveTowards(_knockbackVelocity, Vector3.zero, knockbackDecay * Time.deltaTime);
        }
    }

    public void TakeDamage(float amount)
    {
        CurrentHealth = Mathf.Max(CurrentHealth - amount, 0f);
        if (CurrentHealth <= 0f)
            Die();
    }

    public void ApplyKnockback(Vector3 force)
    {
        _knockbackVelocity = force;
    }

    private void Die()
    { 
        Debug.Log("Player died.");
    }
}