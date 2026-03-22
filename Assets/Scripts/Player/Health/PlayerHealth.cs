using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    public float maxHealth = 100f;
    public float CurrentHealth { get; private set; }

    [Header("Knockback")]
    [SerializeField] private float knockbackDecay = 10f;

    [Header("Iframes")]
    [SerializeField] private float iframeDuration = 1f;  // seconds of invincibility after hit

    public bool IsInvincible { get; private set; }

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
        if (IsInvincible) return;

        CurrentHealth = Mathf.Max(CurrentHealth - amount, 0f);
        if (CurrentHealth <= 0f)
            Die();
        else
            StartCoroutine(IframeCoroutine());
    }

    public void ApplyKnockback(Vector3 force)
    {
        _knockbackVelocity = force;
    }

    private IEnumerator IframeCoroutine()
    {
        IsInvincible = true;
        yield return new WaitForSeconds(iframeDuration);
        IsInvincible = false;
    }

    private void Die()
    {
        Debug.Log("Player died.");
    }
}