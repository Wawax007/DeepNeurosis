using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class MobAI : MonoBehaviour
{
    public enum MobState
    {
        Patrolling,
        Suspicious,
        Chasing,
        Searching,
        Attacking
    }

    [Header("References")]
    private NavMeshAgent agent;
    private Animator animator; // Référence à l'Animator
    public Transform player;

    [Header("Patrol Settings")]
    public Transform[] patrolPoints;
    private int currentPatrolIndex = 0;

    [Header("Detection Settings")]
    public float viewDistance = 15f;
    public float viewAngle = 70f;
    public float hearingRadius = 10f;
    public LayerMask obstacleMask;
    public Vector3 eyeLevelOffset = new Vector3(0f, 1.8f, 0f);

    [Header("Speeds")]
    public float chaseSpeed = 5f;
    public float patrolSpeed = 2f;
    public float suspicionSpeed = 3f;

    [Header("Search Settings")]
    private Vector3 lastKnownPosition;
    private bool isPlayerSeen = false;
    private float searchTimer = 0f;
    private float searchDuration = 10f;
    private List<Vector3> searchPoints = new List<Vector3>();
    private int currentSearchIndex = 0;

    [Header("Suspicion Settings")]
    public float suspicionTime = 2f;
    private float suspiciousTimer = 0f;

    [Header("Attack Settings")]
    private bool isAttacking = false;
    public float attackDelay = 2f;

    public MobState currentState = MobState.Patrolling;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>(); // Initialise l'Animator
        agent.speed = patrolSpeed;
        GoToNextPatrolPoint();
    }

    void Update()
    {
        switch (currentState)
        {
            case MobState.Patrolling:
                Patrol();
                if (CanDetectPlayer())
                {
                    currentState = MobState.Suspicious;
                    suspiciousTimer = 0f;
                    agent.speed = suspicionSpeed;
                }
                break;

            case MobState.Suspicious:
                BeSuspicious();
                break;

            case MobState.Chasing:
                ChasePlayer();
                break;

            case MobState.Searching:
                SearchForPlayer();
                break;

            case MobState.Attacking:
                AttackPlayer();
                break;
        }

        UpdateAnimator(); // Synchronise l'animation avec la vitesse de déplacement
    }

    private void UpdateAnimator()
    {
        // Met à jour l'animation en fonction de la vitesse actuelle
        float currentSpeed = agent.velocity.magnitude;
        animator.SetFloat("Speed", currentSpeed);
    }

    private void Patrol()
    {
        agent.isStopped = false;
        agent.speed = patrolSpeed;

        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            GoToNextPatrolPoint();
        }
    }

    private void GoToNextPatrolPoint()
    {
        if (patrolPoints.Length == 0) return;
        agent.destination = patrolPoints[currentPatrolIndex].position;
        currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
    }

    private void BeSuspicious()
    {
        agent.isStopped = true;
        animator.SetFloat("Speed", 0); // Animation Idle
        suspiciousTimer += Time.deltaTime;

        transform.Rotate(Vector3.up, 60f * Time.deltaTime);

        if (CanSeePlayer())
        {
            lastKnownPosition = player.position;
            currentState = MobState.Chasing;
            agent.speed = chaseSpeed;
        }
        else if (suspiciousTimer >= suspicionTime)
        {
            currentState = MobState.Searching;
            PrepareSearchArea();
        }
    }

    private void ChasePlayer()
    {
        agent.isStopped = false;
        agent.speed = chaseSpeed;
        agent.destination = player.position;

        if (!CanSeePlayer())
        {
            lastKnownPosition = player.position;
            currentState = MobState.Searching;
            PrepareSearchArea();
        }
    }

    private void PrepareSearchArea()
    {
        searchTimer = 0f;
        searchPoints.Clear();
        for (int i = 0; i < 3; i++)
        {
            Vector3 randomOffset = Random.insideUnitSphere * 3f;
            randomOffset.y = 0;
            searchPoints.Add(lastKnownPosition + randomOffset);
        }
        currentSearchIndex = 0;

        if (searchPoints.Count > 0)
        {
            agent.destination = searchPoints[currentSearchIndex];
        }
    }

    private void SearchForPlayer()
    {
        agent.speed = patrolSpeed;
        searchTimer += Time.deltaTime;

        if (CanSeePlayer())
        {
            currentState = MobState.Chasing;
            return;
        }

        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            currentSearchIndex++;
            if (currentSearchIndex < searchPoints.Count)
            {
                agent.destination = searchPoints[currentSearchIndex];
            }
            else
            {
                ResetToPatrol();
            }
        }

        if (searchTimer >= searchDuration)
        {
            ResetToPatrol();
        }
    }

    private void ResetToPatrol()
    {
        currentState = MobState.Patrolling;
        GoToNextPatrolPoint();
    }

    private void AttackPlayer()
    {
        if (isAttacking) return;

        isAttacking = true;
        agent.isStopped = true;
        animator.SetTrigger("AttackTrigger"); // Animation d'attaque
        Invoke(nameof(EndAttack), attackDelay);
    }

    private void EndAttack()
    {
        isAttacking = false;
        if (CanSeePlayer())
        {
            currentState = MobState.Chasing;
        }
        else
        {
            currentState = MobState.Searching;
            PrepareSearchArea();
        }
    }

    private bool CanDetectPlayer()
    {
        return CanSeePlayer();
    }

    private bool CanSeePlayer()
    {
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, directionToPlayer);
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (angle < viewAngle / 2 && distanceToPlayer < viewDistance)
        {
            if (Physics.Raycast(transform.position, directionToPlayer, out RaycastHit hit, viewDistance, ~obstacleMask))
            {
                return hit.transform.CompareTag("Player");
            }
        }
        return false;
    }
    // DEBUG VISUEL
    private void OnDrawGizmosSelected()
    {
        // Audition
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, hearingRadius);

        // Vision
        Vector3 viewLeft = Quaternion.Euler(0, -viewAngle / 2, 0) * transform.forward * viewDistance;
        Vector3 viewRight = Quaternion.Euler(0, viewAngle / 2, 0) * transform.forward * viewDistance;

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position + eyeLevelOffset, transform.position + eyeLevelOffset + viewLeft);
        Gizmos.DrawLine(transform.position + eyeLevelOffset, transform.position + eyeLevelOffset + viewRight);

        // Dernière position vue
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(lastKnownPosition, 0.2f);

        // Points de recherche
        Gizmos.color = Color.green;
        foreach (var sp in searchPoints)
        {
            Gizmos.DrawWireSphere(sp, 0.3f);
        }
    }
}
