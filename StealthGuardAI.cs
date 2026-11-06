using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// A standalone, finite-state stealth guard that patrols, sees a target,
/// hears reported noises, chases, searches, and resumes patrolling.
/// Requires a baked NavMesh. Add a NavMeshAgent to the guard and assign
/// target/patrol points in the Inspector. No player script is required.
/// Use StealthGuardAI.BroadcastNoise(worldPosition, loudness) from other scripts.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(NavMeshAgent))]
public sealed class StealthGuardAI : MonoBehaviour
{
    public enum GuardState { Patrol, Investigate, Chase, Search, Return }

    [Header("References")]
    [SerializeField] private Transform target;
    [SerializeField] private Transform[] patrolPoints = new Transform[0];
    [SerializeField] private Transform eyes;

    [Header("Vision")]
    [SerializeField, Min(0.1f)] private float sightRange = 15f;
    [SerializeField, Range(1f, 360f)] private float fieldOfView = 100f;
    [SerializeField] private LayerMask visionLayers = ~0;
    [SerializeField, Min(0.02f)] private float senseInterval = 0.15f;
    [SerializeField, Min(0.01f)] private float suspicionGainPerSecond = 1.5f;
    [SerializeField, Min(0f)] private float suspicionDecayPerSecond = 0.25f;
    [SerializeField, Range(0.01f, 1f)] private float detectionThreshold = 0.75f;
    [SerializeField, Min(0f)] private float loseSightGraceSeconds = 2.0f;
    [SerializeField, Min(0f)] private float eyeHeight = 1.6f;
    [SerializeField, Min(0f)] private float targetHeightOffset = 1.0f;

    [Header("Hearing (Explicit Noise Events)")]
    [SerializeField, Min(0.1f)] private float hearingRadius = 14f;
    [SerializeField, Range(0f, 1f)] private float minimumAudibleStrength = 0.07f;
    [SerializeField, Min(0f)] private float hearingSuspicionGain = 0.6f;

    [Header("Behaviour")]
    [SerializeField, Min(0.1f)] private float patrolSpeed = 2.5f;
    [SerializeField, Min(0.1f)] private float chaseSpeed = 5.5f;
    [SerializeField, Min(0f)] private float waypointTolerance = 0.5f;
    [SerializeField, Min(0f)] private float investigateSeconds = 4f;
    [SerializeField, Min(0f)] private float searchSeconds = 10f;
    [SerializeField, Min(0.1f)] private float searchRadius = 7f;
    [SerializeField, Min(0.05f)] private float chaseRepathSeconds = 0.25f;
    [SerializeField] private bool showDebugGizmos = true;

    private static readonly List<StealthGuardAI> ActiveGuards = new List<StealthGuardAI>();

    private NavMeshAgent agent;
    private GuardState state = GuardState.Patrol;
    private int patrolIndex;
    private float suspicion;
    private float senseTimer;
    private float stateTime;
    private float nextRepathTime;
    private float lastSeenTime = -100000f;
    private Vector3 lastKnownPosition;
    private bool canSeeTarget;
    private bool hasInvestigationPosition;
    private bool initialized;

    public GuardState CurrentState { get { return state; } }
    public float Suspicion01 { get { return suspicion; } }
    public bool CanSeeTarget { get { return canSeeTarget; } }
    public Vector3 LastKnownPosition { get { return lastKnownPosition; } }

    /// <summary>Fires when a guard changes state.</summary>
    public event Action<GuardState> StateChanged;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        searchPath = new NavMeshPath();
    }

    private void OnEnable()
    {
        if (!ActiveGuards.Contains(this))
            ActiveGuards.Add(this);
    }

    private void Start()
    {
        if (agent == null || !agent.isOnNavMesh)
        {
            Debug.LogWarning(
                "StealthGuardAI requires an enabled NavMeshAgent positioned on a baked NavMesh.", this);
            enabled = false;
            return;
        }

        initialized = true;
        state = GuardState.Patrol;
        agent.isStopped = false;
        agent.speed = patrolSpeed;
        GoToPatrolPoint();
    }

    private void OnDisable()
    {
        ActiveGuards.Remove(this);
        if (agent != null && agent.enabled && agent.isOnNavMesh)
            agent.isStopped = true;
    }

    private void Update()
    {
        if (!initialized || agent == null || !agent.enabled || !agent.isOnNavMesh)
            return;

        float dt = Time.deltaTime;
        stateTime += dt;
        senseTimer += dt;

        float interval = Mathf.Max(0.02f, senseInterval);
        if (senseTimer >= interval)
        {
            float elapsed = senseTimer;
            senseTimer = 0f;
            SenseTarget(elapsed);
        }

        switch (state)
        {
            case GuardState.Patrol:
                agent.speed = patrolSpeed;
                if (Arrived())
                {
                    AdvancePatrolPoint();
                    GoToPatrolPoint();
                }
                break;

            case GuardState.Investigate:
                agent.speed = patrolSpeed;
                if (Arrived() && stateTime >= investigateSeconds)
                    EnterState(GuardState.Search);
                break;

            case GuardState.Chase:
                agent.speed = chaseSpeed;
                if (Time.time >= nextRepathTime)
                {
                    nextRepathTime = Time.time + Mathf.Max(0.05f, chaseRepathSeconds);
                    SetDestination(lastKnownPosition);
                }

                if (!canSeeTarget && Time.time - lastSeenTime >= loseSightGraceSeconds)
                    EnterState(GuardState.Search);
                break;

            case GuardState.Search:
                agent.speed = patrolSpeed;
                if (stateTime >= searchSeconds)
                {
                    EnterState(GuardState.Return);
                }
                else if (Arrived())
                {
                    SetRandomSearchDestination();
                }
                break;

            case GuardState.Return:
                agent.speed = patrolSpeed;
                if (Arrived())
                {
                    EnterState(GuardState.Patrol);
                }
                break;
        }
    }

    private void SenseTarget(float elapsed)
    {
        canSeeTarget = HasLineOfSight();
        if (canSeeTarget)
        {
            lastKnownPosition = target.position;
            hasInvestigationPosition = true;
            lastSeenTime = Time.time;
            suspicion = Mathf.Clamp01(suspicion + suspicionGainPerSecond * elapsed);

            if (suspicion >= detectionThreshold && state != GuardState.Chase)
            {
                EnterState(GuardState.Chase);
            }
            else if (state == GuardState.Patrol && suspicion > 0.2f)
            {
                EnterState(GuardState.Investigate);
            }
        }
        else
        {
            suspicion = Mathf.Clamp01(suspicion - suspicionDecayPerSecond * elapsed);
        }
    }

    private bool HasLineOfSight()
    {
        if (target == null || !target.gameObject.activeInHierarchy)
            return false;

        Vector3 origin = eyes != null ? eyes.position : transform.position + Vector3.up * eyeHeight;
        Vector3 position = target.position + Vector3.up * targetHeightOffset;
        Vector3 toTarget = position - origin;
        float distance = toTarget.magnitude;
        if (distance < 0.001f || distance > sightRange)
            return false;

        if (Vector3.Angle(transform.forward, toTarget) > fieldOfView * 0.5f)
            return false;

        Vector3 direction = toTarget / distance;
        if (Physics.Raycast(origin, direction, out RaycastHit hit, distance,
                visionLayers, QueryTriggerInteraction.Ignore))
        {
            // A collider on the target or its child is a valid hit;
            // any nearer collider in visionLayers obstructs the view.
            return hit.transform == target || hit.transform.IsChildOf(target);
        }

        // A target without Colliders can still be seen when nothing occludes it.
        return true;
    }

    /// <summary>
    /// Report a noise to this guard. Loudness 1 = normal; 2 = double hearing range.
    /// This is an abstract distance-based sensor, not sound-wave simulation.
    /// </summary>
    public void HearNoise(Vector3 worldPosition, float loudness = 1f)
    {
        if (!initialized || !isActiveAndEnabled || loudness <= 0f ||
            float.IsNaN(loudness) || float.IsInfinity(loudness))
            return;

        float maxDistance = hearingRadius * loudness;
        float distance = Vector3.Distance(transform.position, worldPosition);
        if (distance > maxDistance)
            return;

        float audibleStrength = 1f - distance / Mathf.Max(maxDistance, 0.001f);
        if (audibleStrength < minimumAudibleStrength)
            return;

        suspicion = Mathf.Clamp01(
            Mathf.Max(suspicion, audibleStrength * hearingSuspicionGain));
        lastKnownPosition = worldPosition;
        hasInvestigationPosition = true;

        // A pursued target takes precedence; a new noise redirects non-chasing guards.
        if (state != GuardState.Chase)
            EnterState(GuardState.Investigate, true);
    }

    /// <summary>Deliver a noise to all active guards. Call from footsteps, gunshots, etc.</summary>
    public static void BroadcastNoise(Vector3 position, float loudness = 1f)
    {
        // Iterate backwards so disabling an instance during a call is safe.
        for (int i = ActiveGuards.Count - 1; i >= 0; i--)
        {
            if (i < ActiveGuards.Count && ActiveGuards[i] != null)
                ActiveGuards[i].HearNoise(position, loudness);
        }
    }

    /// <summary>Reassign a target at runtime without changing any other state.</summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    private void EnterState(GuardState next, bool restart = false)
    {
        if (state == next && !restart)
            return;

        bool changed = state != next;
        state = next;
        stateTime = 0f;

        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            return;

        agent.isStopped = false;

        switch (next)
        {
            case GuardState.Patrol:
                GoToPatrolPoint();
                break;

            case GuardState.Investigate:
                if (hasInvestigationPosition)
                    SetDestination(lastKnownPosition);
                break;

            case GuardState.Chase:
                nextRepathTime = 0f;
                agent.speed = chaseSpeed;
                SetDestination(lastKnownPosition);
                break;

            case GuardState.Search:
                SetDestination(lastKnownPosition);
                break;

            case GuardState.Return:
                suspicion = 0f;
                GoToPatrolPoint();
                break;
        }

        if (changed)
            StateChanged?.Invoke(next);
    }

    private bool Arrived()
    {
        if (agent.pathPending)
            return false;

        if (agent.hasPath)
            return agent.remainingDistance <= agent.stoppingDistance + waypointTolerance;

        // If no path exists, let the state progress instead of hanging forever.
        return true;
    }

    private void AdvancePatrolPoint()
    {
        if (patrolPoints != null && patrolPoints.Length > 0)
            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
    }

    private void GoToPatrolPoint()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            agent.ResetPath();
            return;
        }

        // Skip null entries (can result from deleted scene patrol points).
        for (int i = 0; i < patrolPoints.Length; i++)
        {
            Transform point = patrolPoints[patrolIndex];
            if (point != null)
            {
                SetDestination(point.position);
                return;
            }
            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
        }
        agent.ResetPath();
    }

    private void SetRandomSearchDestination()
    {
        for (int i = 0; i < 10; i++)
        {
            Vector2 offset = UnityEngine.Random.insideUnitCircle * searchRadius;
            Vector3 candidate = lastKnownPosition + new Vector3(offset.x, 0f, offset.y);
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, agent.areaMask) &&
                agent.CalculatePath(hit.position, searchPath) &&
                searchPath.status == NavMeshPathStatus.PathComplete)
            {
                agent.SetPath(searchPath);
                return;
            }
        }

        // If no path is available, do not request it every frame.
        agent.ResetPath();
    }

    private NavMeshPath searchPath;

    private void SetDestination(Vector3 destination)
    {
        if (!agent.isOnNavMesh)
            return;

        if (NavMesh.SamplePosition(destination, out NavMeshHit hit, 2f, agent.areaMask))
            agent.SetDestination(hit.position);
    }

    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;

        Vector3 origin = eyes != null ? eyes.position : transform.position + Vector3.up * eyeHeight;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(origin, sightRange);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, hearingRadius);

        Vector3 left = Quaternion.Euler(0f, -fieldOfView * 0.5f, 0f) * transform.forward;
        Vector3 right = Quaternion.Euler(0f, fieldOfView * 0.5f, 0f) * transform.forward;
        Gizmos.color = canSeeTarget ? Color.red : Color.yellow;
        Gizmos.DrawLine(origin, origin + left * sightRange);
        Gizmos.DrawLine(origin, origin + right * sightRange);

        if (Application.isPlaying && hasInvestigationPosition)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(lastKnownPosition, 0.5f);
        }
    }

    private void OnValidate()
    {
        sightRange = Mathf.Max(0.1f, sightRange);
        senseInterval = Mathf.Max(0.02f, senseInterval);
        hearingRadius = Mathf.Max(0.1f, hearingRadius);
        searchRadius = Mathf.Max(0.1f, searchRadius);
        chaseRepathSeconds = Mathf.Max(0.05f, chaseRepathSeconds);
    }
}
