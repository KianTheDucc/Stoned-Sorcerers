using System.Collections;
using UnityEngine;
using UnityEngine.AI;


[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Rigidbody))]
public class SpiderAttack : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;

    [Header("Lunge Range")]
    [SerializeField] private LayerMask groundMask = ~0;  // set this to your ground layer only
    [SerializeField] private float lungeRange = 4f;  // distance to trigger lunge
    [SerializeField] private float windUpTime = 0.4f; // pause before launching
    [SerializeField] private float lungeForce = 18f;  // launch force
    [SerializeField] private float lungeDuration = 0.5f; // how long lunge lasts before stun check

    [Header("Damage")]
    [SerializeField] private float damage = 15f;
    [SerializeField] private float knockbackForce = 10f;
    [SerializeField] private float hitRadius = 1.2f; // contact detection radius at lunge end

    [Header("Cooldown & Stun")]
    [SerializeField] private float cooldown = 3f;   // seconds before can lunge again
    [SerializeField] private float stunDuration = 1.2f; // stun time if lunge misses

    // ─── State ────────────────────────────────────────────────────────────────
    private enum LungeState { Ready, WindUp, Lunging, Stunned, Cooldown }
    private LungeState _state = LungeState.Ready;

    /// <summary>True while winding up, lunging, or stunned. EnemyAI checks this before moving.</summary>
    public bool IsAttacking => _state != LungeState.Ready && _state != LungeState.Cooldown;

    /// <summary>True only when fully ready to lunge — not cooldown, not attacking.</summary>
    public bool IsReadyToLunge => _state == LungeState.Ready;

    /// <summary>Exposed so EnemyAI knows when to stop chasing and let the lunge trigger.</summary>
    public float LungeRange => lungeRange;

    private NavMeshAgent _agent;
    private Rigidbody _rb;
    private EnemyAI _enemyAI;
    private Vector3 _lungeDirection;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _rb = GetComponent<Rigidbody>();
        _enemyAI = GetComponent<EnemyAI>();

        // Rigidbody setup — we take manual control during the lunge
        _rb.isKinematic = true;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void Update()
    {

        if (player == null || _state != LungeState.Ready) return;

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist <= lungeRange)
            StartCoroutine(DoLunge());
    }

    // ─── Lunge Sequence ───────────────────────────────────────────────────────

    private IEnumerator DoLunge()
    {
        // ── Wind up ──
        _state = LungeState.WindUp;
        SetAIEnabled(false);

        // Face the player during wind up
        Vector3 toPlayer = (player.position - transform.position);
        toPlayer.y = 0f;
        if (toPlayer != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(toPlayer);

        // Lock direction at the moment of launch — add upward angle for the arc
        Vector3 flat = (player.position - transform.position);
        flat.y = 0f;
        _lungeDirection = (flat.normalized + Vector3.up).normalized;

        yield return new WaitForSeconds(windUpTime);

        // ── Launch ──
        _state = LungeState.Lunging;
        _agent.enabled = false;
        _rb.isKinematic = false;
        _rb.linearVelocity = _lungeDirection * lungeForce;

        // Wait until the spider has left the ground before checking for landing
        bool leftGround = false;
        float takeoffTimer = 0f;
        while (!leftGround)
        {
            takeoffTimer += Time.deltaTime;
            bool onGround = Physics.CheckSphere(
                transform.position + Vector3.down * 0.1f, 0.3f, groundMask,
                QueryTriggerInteraction.Ignore);

            // Consider it airborne once it's off the ground, or force it after 0.3s
            if (!onGround || takeoffTimer > 0.3f)
                leftGround = true;

            yield return null;
        }

        bool hit = false;
        float lungeTimeout = 3f; // safety — force land after 3 seconds no matter what
        float lungeTimer = 0f;

        // Now wait until landed OR player hit OR timeout
        while (true)
        {
            lungeTimer += Time.deltaTime;

            // Check for player contact
            if (!hit)
            {
                Collider[] cols = Physics.OverlapSphere(transform.position, hitRadius);
                foreach (var col in cols)
                {
                    // Use tag check — make sure your Player is tagged "Player" in Unity
                    if (!col.CompareTag("Player") && col.GetComponentInParent<PlayerHealth>() == null) continue;

                    PlayerHealth playerHealth = col.GetComponentInParent<PlayerHealth>();
                    if (playerHealth != null)
                    {
                        playerHealth.TakeDamage(damage);
                        Vector3 knockDir = (player.position - transform.position).normalized;
                        knockDir.y = 0.3f;
                        playerHealth.ApplyKnockback(knockDir * knockbackForce);
                    }

                    hit = true;
                    break;
                }
            }

            // Check if landed — use low velocity as a reliable landing indicator
            bool landed = _rb.linearVelocity.magnitude < 0.5f || lungeTimer >= lungeTimeout;

            if (landed) break;

            yield return null;
        }

        // ── Land ──
        _rb.linearVelocity = Vector3.zero;
        _rb.isKinematic = true;

        // Warp BEFORE enabling the agent — prevents it snapping to old NavMesh position
        Vector3 landedPosition = transform.position;
        _agent.Warp(landedPosition);
        _agent.enabled = true;

        if (hit)
        {
            // Hit — go straight to cooldown
            yield return StartCoroutine(DoCooldown());
        }
        else
        {
            // Missed — stun first then cooldown
            yield return StartCoroutine(DoStun());
            yield return StartCoroutine(DoCooldown());
        }
    }

    // ─── Stun ─────────────────────────────────────────────────────────────────

    private IEnumerator DoStun()
    {
        _state = LungeState.Stunned;
        // Spider sits still during stun — AI stays disabled
        yield return new WaitForSeconds(stunDuration);
    }

    // ─── Cooldown ─────────────────────────────────────────────────────────────

    private IEnumerator DoCooldown()
    {
        _state = LungeState.Cooldown;
        SetAIEnabled(true);
        yield return new WaitForSeconds(cooldown);
        _state = LungeState.Ready;
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private void SetAIEnabled(bool state)
    {
        if (!state && _agent.isOnNavMesh)
            _agent.ResetPath();
    }

    // ─── Gizmos ───────────────────────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        // Lunge range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, lungeRange);

        // Hit detection radius
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, hitRadius);
    }
}