using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ZombieAI_Runner : MonoBehaviour
{
    // Movement and behavior settings
    [Header("Movement Settings")]
    [SerializeField] private float baseSpeed = 3.5f;          // Default speed
    [SerializeField] private float baseAttackCooldown = 1f;   // Default attack cooldown
    [SerializeField] private float baseVisionRange = 15f;     // Default vision range
    [SerializeField] private float nightVisionRange = 20f;    // Increased range at night
    [SerializeField] private float nightSpeedMultiplier = 1.5f; // Faster at night
    [SerializeField] private float nightAttackRate = 0.5f;    // More aggressive at night

    [Header("Runner Stats")]
    [SerializeField] private int damage = 8;                  // Lower damage for runner
    [SerializeField] private float attackRange = 1.8f;        // Distance within which zombie can attack
    [SerializeField] private float chaseStoppingDistance = 1.2f; // Stopping distance during chase
    [SerializeField] private float patrolStoppingDistance = 0.1f; // Stopping distance during patrol
    [SerializeField] private float runSpeed = 7f;             // Fast movement speed
    [SerializeField] private float acceleration = 15f;        // Quick acceleration
    [SerializeField] private float angularSpeed = 360f;       // Fast turning

    [Header("Vision Settings")]
    [SerializeField] private float visionAngle = 120f;        // Wider field of view
    [SerializeField] private float detectionRange = 30f;      // Range at which zombie can detect player through walls
    [SerializeField] private LayerMask visionObstacleMask;    // Obstacles blocking vision

    [Header("Patrol Settings")]
    [SerializeField] private float idleTime = 1f;             // Short idle time
    [SerializeField] private float patrolRadius = 15f;        // Wider patrol radius

    [Header("Debug Settings")]
    [SerializeField] private bool showVisionCone = true;      // Show vision cone in editor
    [SerializeField] private bool showAttackRange = true;     // Show attack range in editor
    [SerializeField] private bool showDetectionRange = true;  // Show global detection range

    // Components and references
    private Transform player;
    private NavMeshAgent agent;
    private Animator animator;
    private DayNightCycle dayNightCycle;
    private PlayerHealth currentTarget;

    // State variables
    private ZombieState currentState = ZombieState.Patrol;
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float stateTimer = 0f;
    private bool hasDetectedPlayer = false;                  // Once set to true, never reset
    private bool playerInSight = false;
    private Vector3 lastKnownPlayerPos;
    private float lastAttackTime;
    private bool isSearching = false;
    private float searchUpdateTimer = 0f;

    public enum ZombieState
    {
        Idle,
        Patrol,
        Chase,
        Attack,
        Stagger,
        Search
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
            agent.speed = runSpeed;
            agent.acceleration = acceleration;
            agent.angularSpeed = angularSpeed;
            agent.stoppingDistance = patrolStoppingDistance;
            agent.isStopped = false;
            agent.autoBraking = false; // Prevents slowing down before destination
            agent.updateRotation = false; // We'll handle rotation manually for smoother movement
        }

        stateTimer = idleTime;
        lastAttackTime = -baseAttackCooldown; // Allow immediate attack if needed
        TransitionToPatrolState();
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
        agent.speed = isNight ? runSpeed * nightSpeedMultiplier : runSpeed;
        float currentAttackCooldown = isNight ? baseAttackCooldown * nightAttackRate : baseAttackCooldown;
        float currentVisionRange = isNight ? nightVisionRange : baseVisionRange;

        // Update vision and state
        CheckVision(currentVisionRange);
        UpdateState(currentAttackCooldown);

        // Update animations
        UpdateAnimations();

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

        // Debug path visualization
        if (Debug.isDebugBuild)
        {
            Debug.DrawLine(transform.position, agent.destination, Color.blue);
        }
    }

    void UpdateAnimations()
    {
        if (animator != null)
        {
            bool isRunning = currentState == ZombieState.Chase || currentState == ZombieState.Search;
            bool isWalking = currentState == ZombieState.Patrol && agent.velocity.magnitude > 0.1f;
            bool isAttacking = currentState == ZombieState.Attack;

            animator.SetBool("isRunning", isRunning);
            animator.SetBool("isWalking", isWalking);
            animator.SetBool("isAttacking", isAttacking);
        }
    }

    void CheckVision(float visionRange)
    {
        playerInSight = false;

        if (player == null) return;

        Vector3 directionToPlayer = player.position - transform.position;
        float distanceToPlayer = directionToPlayer.magnitude;

        // Direct line of sight detection
        if (distanceToPlayer <= visionRange)
        {
            float angle = Vector3.Angle(transform.forward, directionToPlayer);
            if (angle <= visionAngle / 2)
            {
                Vector3 eyePosition = transform.position + Vector3.up * 1.6f;
                Vector3 targetPosition = player.position + Vector3.up * 1.0f;
                Vector3 directionToTarget = targetPosition - eyePosition;

                if (!Physics.Raycast(eyePosition, directionToTarget.normalized, distanceToPlayer, visionObstacleMask))
                {
                    playerInSight = true;
                    hasDetectedPlayer = true;
                    lastKnownPlayerPos = player.position;

                    if (currentState != ZombieState.Chase && currentState != ZombieState.Attack && currentState != ZombieState.Stagger)
                    {
                        TransitionToChaseState();
                    }
                }
            }
        }

        // Sound awareness: detect player within 30% of vision range regardless of angle
        if (!playerInSight && distanceToPlayer < visionRange * 0.3f)
        {
            Vector3 eyePosition = transform.position + Vector3.up * 1.6f;
            Vector3 targetPosition = player.position + Vector3.up * 1.0f;
            Vector3 directionToTarget = targetPosition - eyePosition;

            if (!Physics.Raycast(eyePosition, directionToTarget.normalized, distanceToPlayer, visionObstacleMask))
            {
                playerInSight = true;
                hasDetectedPlayer = true;
                lastKnownPlayerPos = player.position;

                if (currentState != ZombieState.Chase && currentState != ZombieState.Attack && currentState != ZombieState.Stagger)
                {
                    TransitionToChaseState();
                }
            }
        }

        // Global detection for persistence once player has been spotted
        if (hasDetectedPlayer && !playerInSight)
        {
            if (distanceToPlayer <= detectionRange)
            {
                // Update last known position even through walls once detected
                lastKnownPlayerPos = player.position;

                // If we're not already searching or chasing, start searching
                if (currentState != ZombieState.Search && currentState != ZombieState.Chase && 
                    currentState != ZombieState.Attack && currentState != ZombieState.Stagger)
                {
                    TransitionToSearchState();
                }
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
                HandleAttackState();
                break;
            case ZombieState.Search:
                HandleSearchState();
                break;
            case ZombieState.Stagger:
                // Handled in coroutine
                break;
        }
    }

    void HandleIdleState()
    {
        // If player has been detected, zombie should never idle
        if (hasDetectedPlayer)
        {
            TransitionToSearchState();
            return;
        }

        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0)
        {
            TransitionToPatrolState();
        }
    }

    void HandlePatrolState()
    {
        // If player has been detected, zombie should never patrol normally
        if (hasDetectedPlayer)
        {
            TransitionToSearchState();
            return;
        }

        if (agent.isStopped)
        {
            agent.isStopped = false;
            agent.stoppingDistance = patrolStoppingDistance;
        }

        if (!agent.pathPending && agent.remainingDistance <= patrolStoppingDistance + 0.1f)
        {
            TransitionToIdleState();
        }

        if (!agent.pathPending && agent.pathStatus == NavMeshPathStatus.PathInvalid)
        {
            TransitionToPatrolState(); // Retry with new target
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
            // Direct chase - we can see the player
            agent.SetDestination(player.position);
        }
        else if (hasDetectedPlayer)
        {
            // If we can't see player but have detected them before, go to search state
            TransitionToSearchState();
        }

        // Check if close enough to attack
        if (player != null && Vector3.Distance(transform.position, player.position) <= attackRange)
        {
            float currentAttackCooldown = (dayNightCycle != null && dayNightCycle.IsNightTime()) ? 
                baseAttackCooldown * nightAttackRate : baseAttackCooldown;
                
            if (Time.time >= lastAttackTime + currentAttackCooldown)
            {
                TransitionToAttackState();
            }
        }
    }

    void HandleSearchState()
    {
        // In search state, the zombie is relentlessly pursuing the player's last known position
        // and constantly updating when the player is detected

        if (!isSearching)
        {
            isSearching = true;
            agent.stoppingDistance = chaseStoppingDistance;
            agent.isStopped = false;
            agent.SetDestination(lastKnownPlayerPos);
        }

        // Update the search path periodically or if we get close to the destination
        searchUpdateTimer -= Time.deltaTime;
        if (searchUpdateTimer <= 0 || 
            (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance))
        {
            // If we can see the player directly, transition to chase
            if (playerInSight)
            {
                TransitionToChaseState();
                return;
            }

            // Always update to the most recent player position if within detection range
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            if (distanceToPlayer <= detectionRange)
            {
                lastKnownPlayerPos = player.position;
                agent.SetDestination(lastKnownPlayerPos);
            }
            
            // If we've reached the last known position and don't see the player,
            // start searching in a widening spiral pattern
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                Vector3 searchPoint = GetNextSearchPoint();
                agent.SetDestination(searchPoint);
            }
            
            searchUpdateTimer = 1.0f; // Update search path every second
        }
    }

    Vector3 GetNextSearchPoint()
    {
        // Generate points in an expanding spiral pattern around the last known position
        const int maxAttempts = 5;
        const float searchRadius = 20f;
        
        for (int i = 0; i < maxAttempts; i++)
        {
            // Create a random point within the search radius
            Vector3 randomDirection = Random.insideUnitSphere * searchRadius;
            randomDirection.y = 0;
            Vector3 point = lastKnownPlayerPos + randomDirection;
            
            NavMeshHit hit;
            if (NavMesh.SamplePosition(point, out hit, searchRadius, NavMesh.AllAreas))
            {
                NavMeshPath path = new NavMeshPath();
                if (agent.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete)
                {
                    return hit.position;
                }
            }
        }
        
        // If all attempts failed, return to last known position
        return lastKnownPlayerPos;
    }

    void HandleAttackState()
    {
        if (!agent.isStopped)
        {
            agent.isStopped = true;
        }

        if (player != null)
        {
            Vector3 lookDirection = player.position - transform.position;
            lookDirection.y = 0;
            if (lookDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDirection), Time.deltaTime * 10f);
            }
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
                agent.stoppingDistance = patrolStoppingDistance;
                agent.isStopped = false;
            }
            else
            {
                // Retry with reduced radius
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

    void TransitionToIdleState()
    {
        // If player has been detected, never idle
        if (hasDetectedPlayer)
        {
            TransitionToSearchState();
            return;
        }

        currentState = ZombieState.Idle;
        stateTimer = idleTime;

        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.isStopped = true;
            agent.stoppingDistance = patrolStoppingDistance;
        }
    }

    void TransitionToPatrolState()
    {
        // If player has been detected, never patrol normally
        if (hasDetectedPlayer)
        {
            TransitionToSearchState();
            return;
        }

        currentState = ZombieState.Patrol;

        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.stoppingDistance = patrolStoppingDistance;
            agent.isStopped = false;
            SetNewPatrolTarget();
        }
    }

    void TransitionToChaseState()
    {
        currentState = ZombieState.Chase;
        isSearching = false;

        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.stoppingDistance = chaseStoppingDistance;
            agent.isStopped = false;
            if (player != null)
            {
                agent.SetDestination(player.position);
            }
        }
    }

    void TransitionToSearchState()
    {
        currentState = ZombieState.Search;
        isSearching = false; // Will be set to true when handling state
        searchUpdateTimer = 0f; // Force immediate update

        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.stoppingDistance = chaseStoppingDistance;
            agent.isStopped = false;
            agent.SetDestination(lastKnownPlayerPos);
        }
    }

    void TransitionToAttackState()
    {
        currentState = ZombieState.Attack;
        lastAttackTime = Time.time;

        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.isStopped = true;
        }

        if (animator != null)
        {
            animator.SetTrigger("FastAttack");
        }

        if (player != null)
        {
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                currentTarget = playerHealth;
            }
        }

        StartCoroutine(ResumeChaseAfterAttack());
    }

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player") && currentState != ZombieState.Attack && currentState != ZombieState.Stagger)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, other.transform.position);
            float currentAttackCooldown = (dayNightCycle != null && dayNightCycle.IsNightTime()) ? 
                baseAttackCooldown * nightAttackRate : baseAttackCooldown;

            if (distanceToPlayer <= attackRange && Time.time >= lastAttackTime + currentAttackCooldown)
            {
                PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    hasDetectedPlayer = true;
                    TransitionToAttackState();
                }
            }
        }
    }

    IEnumerator ResumeChaseAfterAttack()
    {
        yield return new WaitForSeconds(0.5f); // Match animation length

        if (currentState == ZombieState.Attack)
        {
            if (playerInSight)
            {
                TransitionToChaseState();
            }
            else if (hasDetectedPlayer)
            {
                TransitionToSearchState();
            }
            else
            {
                TransitionToPatrolState();
            }
        }
    }

    void FastAttackHit()
    {
        if (currentTarget != null && Vector3.Distance(transform.position, currentTarget.transform.position) <= attackRange * 1.2f)
        {
            currentTarget.TakeDamage(damage);

            // Chance for rapid follow-up attack if player is very close
            float distanceToTarget = Vector3.Distance(transform.position, currentTarget.transform.position);
            float currentAttackCooldown = (dayNightCycle != null && dayNightCycle.IsNightTime()) ? 
                baseAttackCooldown * nightAttackRate : baseAttackCooldown;
                
            if (Random.value < 0.3f && distanceToTarget <= attackRange * 0.8f && 
                Time.time >= lastAttackTime + currentAttackCooldown)
            {
                lastAttackTime = Time.time;
                if (animator != null)
                {
                    animator.SetTrigger("FastAttack");
                }
            }
        }
    }

    public void ForceStaggerState()
    {
        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.isStopped = true;
        }

        currentState = ZombieState.Stagger;

        if (animator != null)
        {
            animator.SetTrigger("Stagger");
        }

        StartCoroutine(RecoverFromStagger());
    }

    IEnumerator RecoverFromStagger()
    {
        yield return new WaitForSeconds(0.3f); // Quick recovery

        ReevaluateStateAfterHit();
    }

    public void ReevaluateStateAfterHit()
    {
        CheckVision(dayNightCycle != null && dayNightCycle.IsNightTime() ? nightVisionRange : baseVisionRange);

        if (playerInSight)
        {
            TransitionToChaseState();
        }
        else if (hasDetectedPlayer)
        {
            TransitionToSearchState();
        }
        else
        {
            TransitionToPatrolState();
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!showVisionCone && !showAttackRange && !showDetectionRange) return;

        bool isNight = dayNightCycle != null && dayNightCycle.IsNightTime();
        float currentVisionRange = isNight ? nightVisionRange : baseVisionRange;

        if (showVisionCone)
        {
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
        }

        if (showDetectionRange)
        {
            // Draw detection range (range where zombie can sense player even through walls)
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f); // Orange
            Gizmos.DrawWireSphere(transform.position, detectionRange);
        }

        if (showAttackRange)
        {
            // Draw attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);

            // Draw stopping distance
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, agent != null ? agent.stoppingDistance : chaseStoppingDistance);
        }

        // Draw patrol zone
        Gizmos.color = new Color(0.3f, 0.3f, 0.9f, 0.2f);
        Gizmos.DrawWireSphere(startPosition, patrolRadius);

        // Draw last known player position
        if (hasDetectedPlayer)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(lastKnownPlayerPos, 0.5f);
        }
    }
}