using System.Collections;
using UnityEngine;
using UnityEngine.AI;


/// Enemy AI with three states: Patrol, Chase, Search.
/// - Patrol: uses NPCPatrol to roam waypoints randomly
/// - Chase: player detected within radius AND line of sight
/// - Search: player lost, enemy returns toward last known position then resumes patrol
///

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(NPCPatrol))]
public class EnemyAI : MonoBehaviour
{
    // ─── Detection ────────────────────────────────────────────────────────────
    [Header("Detection")]
    [SerializeField] private Transform player;
    [SerializeField] private float detectionRadius = 10f;
    [SerializeField] private float fieldOfView = 110f;  // degrees
    [SerializeField] private float eyeHeight = 1.6f;  // raycast origin height
    [SerializeField] private LayerMask obstacleMask = ~0;    // what blocks line of sight

    // ─── Chase ────────────────────────────────────────────────────────────────
    [Header("Chase")]
    [SerializeField] private float chaseSpeed = 6f;
    [SerializeField] private float losePlayerTimer = 3f;   // seconds before giving up chase

    // ─── Search ───────────────────────────────────────────────────────────────
    [Header("Search")]
    [SerializeField] private float searchDuration = 5f;
    [SerializeField] private float patrolSpeed = 3.5f;

    [Header("Back Away")]
    [SerializeField] private float backAwayDistance = 4f;

    // ─── State ────────────────────────────────────────────────────────────────
    private enum State { Patrol, Chase, Search }
    private State _state = State.Patrol;

    private NavMeshAgent _agent;
    private NPCPatrol _patrol;
    private SpiderAttack _spiderAttack;
    private Vector3 _lastKnownPosition;
    private float _loseTimer;
    private float _searchTimer;
    private float _backAwayTimer;

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _patrol = GetComponent<NPCPatrol>();
        _spiderAttack = GetComponent<SpiderAttack>(); // optional — null if not a spider
    }

    private void Start()
    {
        if (player == null)
        {
            Debug.LogWarning("EnemyAI: No player assigned.", this);
            enabled = false;
            return;
        }
        EnterPatrol();
    }

    private void Update()
    {
        // Yield control to SpiderAttack during lunge/windup/stun
        if (_spiderAttack != null && _spiderAttack.IsAttacking) return;

        switch (_state)
        {
            case State.Patrol: UpdatePatrol(); break;
            case State.Chase: UpdateChase(); break;
            case State.Search: UpdateSearch(); break;
        }
    }

    // ─── Patrol ───────────────────────────────────────────────────────────────

    private void EnterPatrol()
    {
        _state = State.Patrol;
        _agent.speed = patrolSpeed;
        _patrol.enabled = true;
    }

    private void UpdatePatrol()
    {
        if (CanSeePlayer())
            EnterChase();
    }

    // ─── Chase ────────────────────────────────────────────────────────────────

    private void EnterChase()
    {
        _state = State.Chase;
        _agent.speed = chaseSpeed;
        _patrol.enabled = false;  // hand off control from patrol to us
        _loseTimer = losePlayerTimer;
    }

    private void UpdateChase()
    {
        if (!_agent.isOnNavMesh) return;

        // Spider is on cooldown after a lunge — back away and do nothing else
        if (_spiderAttack != null && _spiderAttack.IsOnCooldown)
        {
            BackAwayFromPlayer();
            return;
        }

        // Spider is ready to lunge and player is in range — stop and let SpiderAttack fire
        if (_spiderAttack != null && _spiderAttack.IsReadyToLunge)
        {
            float dist = Vector3.Distance(transform.position, player.position);
            if (dist <= _spiderAttack.LungeRange)
            {
                if (_agent.hasPath) _agent.ResetPath();
                return;
            }
        }

        // Chase the player
        if (CanSeePlayer())
        {
            _lastKnownPosition = player.position;
            _loseTimer = losePlayerTimer;
            _agent.SetDestination(player.position);
        }
        else
        {
            _loseTimer -= Time.deltaTime;
            if (_loseTimer <= 0f)
                EnterSearch();
        }
    }

    private void BackAwayFromPlayer()
    {
        if (player == null || !_agent.isOnNavMesh) return;

        _backAwayTimer -= Time.deltaTime;
        if (_backAwayTimer > 0f) return;
        _backAwayTimer = 0.5f;

        Vector3 dirAway = (transform.position - player.position).normalized;
        dirAway.y = 0f;
        Vector3 backPos = transform.position + dirAway * backAwayDistance;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(backPos, out hit, backAwayDistance, NavMesh.AllAreas))
            _agent.SetDestination(hit.position);
    }

    // ─── Search ───────────────────────────────────────────────────────────────

    private void EnterSearch()
    {
        _state = State.Search;
        _agent.speed = patrolSpeed;
        _searchTimer = searchDuration;
        if (_agent.isOnNavMesh)
            _agent.SetDestination(_lastKnownPosition);
    }

    private void UpdateSearch()
    {
        // If we spot the player again while searching, resume chase immediately
        if (CanSeePlayer())
        {
            EnterChase();
            return;
        }

        // Count down once we've arrived at the last known position
        if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance)
        {
            _searchTimer -= Time.deltaTime;
            if (_searchTimer <= 0f)
                EnterPatrol();
        }
    }

    // ─── Detection ────────────────────────────────────────────────────────────

    private bool CanSeePlayer()
    {
        if (player == null) return false;

        Vector3 origin = transform.position + Vector3.up * eyeHeight;
        Vector3 playerPos = player.position + Vector3.up * eyeHeight;
        Vector3 dirToPlayer = playerPos - origin;
        float dist = dirToPlayer.magnitude;

        // 1. Range check
        if (dist > detectionRadius) return false;

        // 2. Field of view check
        float angle = Vector3.Angle(transform.forward, dirToPlayer);
        if (angle > fieldOfView * 0.5f) return false;

        // 3. Line of sight check — raycast to player, ignore triggers
        if (Physics.Raycast(origin, dirToPlayer.normalized, out RaycastHit hit, dist,
                            obstacleMask, QueryTriggerInteraction.Ignore))
        {
            // If we hit something that isn't the player, vision is blocked
            if (!hit.transform.IsChildOf(player) && hit.transform != player)
                return false;
        }

        return true;
    }

    // ─── Gizmos ───────────────────────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = transform.position + Vector3.up * eyeHeight;

        // Detection radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(origin, detectionRadius);

        // Field of view cone
        Gizmos.color = Color.red;
        Vector3 leftBound = Quaternion.Euler(0, -fieldOfView * 0.5f, 0) * transform.forward * detectionRadius;
        Vector3 rightBound = Quaternion.Euler(0, fieldOfView * 0.5f, 0) * transform.forward * detectionRadius;
        Gizmos.DrawRay(origin, leftBound);
        Gizmos.DrawRay(origin, rightBound);

        // Last known position
        if (_state == State.Search)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(_lastKnownPosition, 0.4f);
        }
    }
}