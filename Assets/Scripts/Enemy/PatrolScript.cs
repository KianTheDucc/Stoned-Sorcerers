using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Simple NPC patrol script using Unity NavMesh.
/// Picks a random waypoint, walks to it, waits, then picks another.
///
/// Setup:
///   1. Bake a NavMesh in your scene (Window -> AI -> Navigation -> Bake)
///   2. Add a NavMeshAgent component to your NPC GameObject
///   3. Add this script to the same GameObject
///   4. Create empty GameObjects as waypoints and assign them in the Inspector
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class NPCPatrol : MonoBehaviour
{
    [Header("Waypoints")]
    [SerializeField] private Transform[] waypoints;

    [Header("Behaviour")]
    [SerializeField] private float waitTimeMin = 1f;
    [SerializeField] private float waitTimeMax = 3f;
    [SerializeField] private float stoppingDistance = 0.5f;

    private NavMeshAgent _agent;
    private int _currentWaypoint = -1;
    private bool _waiting;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _agent.stoppingDistance = stoppingDistance;
    }

    private void Start()
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            Debug.LogWarning("NPCPatrol: No waypoints assigned.", this);
            return;
        }
        GoToNextWaypoint();
    }

    private void Update()
    {
        if (_waiting || waypoints.Length == 0) return;

        // Check if we've arrived
        if (!_agent.pathPending && _agent.remainingDistance <= stoppingDistance)
            StartCoroutine(WaitThenMove());
    }

    private void GoToNextWaypoint()
    {
        if (waypoints.Length == 0) return;

        // Pick a random waypoint that isn't the current one
        int next;
        do { next = Random.Range(0, waypoints.Length); }
        while (waypoints.Length > 1 && next == _currentWaypoint);

        _currentWaypoint = next;
        _agent.SetDestination(waypoints[_currentWaypoint].position);
    }

    private IEnumerator WaitThenMove()
    {
        _waiting = true;
        _agent.ResetPath();

        float waitTime = Random.Range(waitTimeMin, waitTimeMax);
        yield return new WaitForSeconds(waitTime);

        _waiting = false;
        GoToNextWaypoint();
    }

    private void OnDrawGizmosSelected()
    {
        if (waypoints == null) return;
        Gizmos.color = Color.cyan;
        foreach (var wp in waypoints)
        {
            if (wp == null) continue;
            Gizmos.DrawWireSphere(wp.position, 0.3f);
        }
    }
}