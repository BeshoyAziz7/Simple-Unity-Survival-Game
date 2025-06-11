using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ZombieAI : MonoBehaviour
{
    // Movement and behavior settings
    [Header("Movement Settings")]
    [SerializeField] private float baseSpeed = 3.5f;          // Default speed
    [SerializeField] private float baseAttackCooldown = 1f;   // Default attack cooldown
    [SerializeField] private float baseVisionRange = 15f;     // Default vision range
    [SerializeField] private float nightVisionRange = 20f;    // Increased range at night
    [SerializeField] private float nightSpeedMultiplier = 1.5f; // Faster at night
    [SerializeField] private float nightAttackRate = 0.5f;    // More aggressive at night

    [Header("Zombie Stats")]
    [SerializeField] private int damage = 10;                 // Damage dealt per attack
    [SerializeField] private float attackRange = 1.5f;        // Distance within which zombie can attack
    [SerializeField] private float chaseStoppingDistance = 1.2f; // Stopping distance during chase
    [SerializeField] private float patrolStoppingDistance = 0.1f; // Stopping distance during patrol

    [Header("Vision Settings")]
    [SerializeField] private float visionAngle = 90f;         // Vision cone angle
    [SerializeField] private float lostPlayerMemoryTime = 5f; // Time to remember player's last position
    [SerializeField] private LayerMask visionObstacleMask;    // Obstacles blocking vision

    [Header("Patrol Settings")]
    [SerializeField] private float idleTime = 3f;             // Time spent idling
    [SerializeField] private float patrolRadius = 10f;        // Patrol radius around start position

    [Header("Debug Settings")]
    [SerializeField] private bool showVisionCone = true;      // Show vision cone in editor

    // Components and references
    private Transform player;
    private NavMeshAgent agent;
    private Animator animator;
    private DayNightCycle dayNightCycle;
    private PlayerHealth currentTarget;

    // State variables
    private ZombieState currentState = ZombieState.Idle;
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float stateTimer = 0f;
    private float playerLostTimer = 0f;
    private bool playerInSight = false;
    private Vector3 lastKnownPlayerPos;
    private float lastAttackTime;

    public enum ZombieState
    {
        Idle,
        Patrol,
        Chase,
        Attack
    }

    void Start()
    {
        // Initialize components
        dayNightCycle = FindObjectOfType<DayNightCycle>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        startPosition = transform.position;

        if (agent != null)
        {
            agent.speed = baseSpeed;
            agent.acceleration = 8f;
            agent.angularSpeed = 120f; // Ensure smooth turning
            agent.stoppingDistance = patrolStoppingDistance;
            agent.isStopped = false;
        }

        stateTimer = idleTime;
        lastAttackTime = -baseAttackCooldown; // Allow immediate attack if needed
    }

    void Update()
    {
        // Ensure critical components exist
        if (player == null || agent == null || !agent.isActiveAndEnabled)
        {
            if (player == null)
            {
                player = GameObject.FindGameObjectWithTag("Player")?.transform;
            }
            if (player == null || agent == null || !agent.isActiveAndEnabled) return;
        }

        // Update behavior based on day/night cycle
        bool isNight = dayNightCycle != null && dayNightCycle.IsNightTime();
        agent.speed = isNight ? baseSpeed * nightSpeedMultiplier : baseSpeed;
        float currentAttackCooldown = isNight ? baseAttackCooldown * nightAttackRate : baseAttackCooldown;
        float currentVisionRange = isNight ? nightVisionRange : baseVisionRange;

        // Update vision and state
        CheckVision(currentVisionRange);
        UpdateState(currentAttackCooldown);

        // Update animation
        if (animator != null)
        {
            animator.SetBool("isWalking", agent.velocity.magnitude > 0.1f);
        }

        // Update rotation to face movement direction
        if (agent.velocity.magnitude > 0.1f)
        {
            Vector3 lookDirection = agent.velocity.normalized;
            lookDirection.y = 0;
            if (lookDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDirection), Time.deltaTime * 10f);
            }
        }
    }

    void CheckVision(float visionRange)
    {
        playerInSight = false;

        Vector3 directionToPlayer = player.position - transform.position;
        float distanceToPlayer = directionToPlayer.magnitude;

        if (distanceToPlayer <= visionRange)
        {
            float angle = Vector3.Angle(transform.forward, directionToPlayer);
            if (angle <= visionAngle / 2)
            {
                if (!Physics.Raycast(transform.position + Vector3.up * 1f, directionToPlayer.normalized, distanceToPlayer, visionObstacleMask))
                {
                    playerInSight = true;
                    lastKnownPlayerPos = player.position;
                    playerLostTimer = lostPlayerMemoryTime;

                    if (currentState != ZombieState.Chase && currentState != ZombieState.Attack)
                    {
                        currentState = ZombieState.Chase;
                        agent.stoppingDistance = chaseStoppingDistance;
                    }
                }
            }
        }

        if (!playerInSight && playerLostTimer > 0)
        {
            playerLostTimer -= Time.deltaTime;
            if (playerLostTimer <= 0 && currentState == ZombieState.Chase)
            {
                currentState = ZombieState.Patrol;
                SetNewPatrolTarget();
            }
        }
    }

    void UpdateState(float currentAttackCooldown)
    {
        switch (currentState)
        {
            case ZombieState.Idle:
                HandleIdleState();
                break;
            case ZombieState.Patrol:
                HandlePatrolState();
                break;
            case ZombieState.Chase:
                HandleChaseState();
                break;
            case ZombieState.Attack:
                // Attack state is handled in OnTriggerStay and coroutine
                break;
        }
    }

    void HandleIdleState()
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0)
        {
            currentState = ZombieState.Patrol;
            SetNewPatrolTarget();
            if (animator != null)
            {
                animator.SetBool("isWalking", true);
            }
        }
    }

    void HandlePatrolState()
    {
        if (agent.isStopped)
        {
            agent.isStopped = false;
            agent.stoppingDistance = patrolStoppingDistance;
        }

        if (!agent.pathPending && agent.remainingDistance < patrolStoppingDistance + 0.1f)
        {
            currentState = ZombieState.Idle;
            stateTimer = idleTime;
            agent.isStopped = true;
        }

        if (!agent.pathPending && agent.pathStatus == NavMeshPathStatus.PathInvalid)
        {
            SetNewPatrolTarget();
        }
    }

    void HandleChaseState()
    {
        if (agent.isStopped)
        {
            agent.isStopped = false;
            agent.stoppingDistance = chaseStoppingDistance;
        }

        if (playerInSight)
        {
            agent.SetDestination(player.position);
        }
        else if (playerLostTimer > 0)
        {
            agent.SetDestination(lastKnownPlayerPos);
        }
    }

    void SetNewPatrolTarget()
    {
        Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
        randomDirection.y = 0;
        randomDirection += startPosition;
        NavMeshHit hit;

        if (NavMesh.SamplePosition(randomDirection, out hit, patrolRadius, NavMesh.AllAreas))
        {
            targetPosition = hit.position;
            NavMeshPath path = new NavMeshPath();
            if (agent.CalculatePath(targetPosition, path) && path.status == NavMeshPathStatus.PathComplete)
            {
                agent.SetDestination(targetPosition);
                agent.isStopped = false;
                currentState = ZombieState.Patrol;
                agent.stoppingDistance = patrolStoppingDistance;
            }
            else
            {
                // Retry with slightly reduced radius
                patrolRadius = Mathf.Max(1.0f, patrolRadius * 0.8f);
                SetNewPatrolTarget();
            }
        }
        else
        {
            // Retry with reduced radius
            patrolRadius = Mathf.Max(1.0f, patrolRadius * 0.8f);
            SetNewPatrolTarget();
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player") && currentState == ZombieState.Chase)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, other.transform.position);
            float currentAttackCooldown = (dayNightCycle != null && dayNightCycle.IsNightTime()) ? baseAttackCooldown * nightAttackRate : baseAttackCooldown;

            if (distanceToPlayer <= attackRange && Time.time >= lastAttackTime + currentAttackCooldown)
            {
                PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    currentState = ZombieState.Attack;
                    agent.isStopped = true;
                    if (animator != null)
                    {
                        animator.SetTrigger("Attack");
                    }
                    currentTarget = playerHealth;
                    lastAttackTime = Time.time;

                    StartCoroutine(ResumeChaseAfterAttack());
                }
            }
        }
    }

    IEnumerator ResumeChaseAfterAttack()
    {
        yield return new WaitForSeconds(1.0f); // Match animation length

        if (playerInSight || playerLostTimer > 0)
        {
            currentState = ZombieState.Chase;
            agent.isStopped = false;
            agent.stoppingDistance = chaseStoppingDistance;
        }
        else
        {
            currentState = ZombieState.Patrol;
            agent.isStopped = false;
            SetNewPatrolTarget();
        }
    }

    void AttackHit()
    {
        if (currentTarget != null && Vector3.Distance(transform.position, currentTarget.transform.position) <= attackRange)
        {
            currentTarget.TakeDamage(damage);
        }
    }

    public void ForceIdleState()
    {
        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.isStopped = true;
            agent.stoppingDistance = patrolStoppingDistance;
        }

        currentState = ZombieState.Idle;
        stateTimer = idleTime;
    }

    public void ReevaluateStateAfterHit()
    {
        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.isStopped = false;
        }

        CheckVision(dayNightCycle != null && dayNightCycle.IsNightTime() ? nightVisionRange : baseVisionRange);

        if (playerInSight)
        {
            currentState = ZombieState.Chase;
            agent.stoppingDistance = chaseStoppingDistance;
        }
        else
        {
            currentState = ZombieState.Patrol;
            SetNewPatrolTarget();
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!showVisionCone) return;

        bool isNight = dayNightCycle != null && dayNightCycle.IsNightTime();
        float currentVisionRange = isNight ? nightVisionRange : baseVisionRange;

        // Draw vision range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, currentVisionRange);

        // Draw vision cone
        Vector3 leftLimit = Quaternion.AngleAxis(-visionAngle / 2, Vector3.up) * transform.forward;
        Vector3 rightLimit = Quaternion.AngleAxis(visionAngle / 2, Vector3.up) * transform.forward;

        Gizmos.color = playerInSight ? Color.red : Color.green;
        Gizmos.DrawRay(transform.position, leftLimit * currentVisionRange);
        Gizmos.DrawRay(transform.position, rightLimit * currentVisionRange);
        Gizmos.DrawRay(transform.position, transform.forward * currentVisionRange);

        // Draw vision cone arc
        Vector3 previousPoint = transform.position + leftLimit * currentVisionRange;
        for (int i = 1; i <= 20; i++)
        {
            float angle = -visionAngle / 2 + (visionAngle / 20) * i;
            Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * transform.forward;
            Vector3 currentPoint = transform.position + direction * currentVisionRange;
            Gizmos.DrawLine(previousPoint, currentPoint);
            previousPoint = currentPoint;
        }

        // Draw last known player position
        if (playerLostTimer > 0)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(lastKnownPlayerPos, 0.5f);
        }
    }
}