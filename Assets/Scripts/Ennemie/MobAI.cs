using System.Collections;
using UnityEngine; 
using UnityEngine.AI;
using System.Collections.Generic;
using PlayerScripts;

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
    private Animator animator;
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

    [Header("Attack Settings")]
    private bool isAttacking = false;
    public float attackDelay = 2f;

    [Header("Search Settings")]
    private Vector3 lastKnownPosition;
    private bool isPlayerSeen = false;
    private float searchTimer = 0f;
    public float searchDuration = 10f;
    private List<Vector3> searchPoints = new List<Vector3>();
    private int currentSearchIndex = -1;

    // On retire toute la logique de rotation sur place
    // On garde maxTimeToReachTarget pour abandonner si on n’atteint pas la destination
    private float searchStartTime = 0f;
    private float maxTimeToReachTarget = 3f;

    [Header("Suspicion Settings")]
    public float suspicionTime = 2f;
    private float suspiciousTimer = 0f;

    
    private float doorInteractionDistance = 2.5f;
    private float doorRetryCooldown = 2f;
    private float lastDoorInteractTime = -999f;
    private Transform lastInteractedDoor = null;
    private Vector3? currentBlockedTarget = null;
    private Transform doorTarget = null;
    private bool waitingForDoor = false;
    
    public MobState currentState = MobState.Patrolling;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        agent.speed = patrolSpeed;

        InternalPartitionGenerator[] allPartGens = FindObjectsOfType<InternalPartitionGenerator>();
        List<Vector3> allPatrolPoints = new List<Vector3>();

        foreach (var partGen in allPartGens)
        {
            allPatrolPoints.AddRange(partGen.GetAllPatrolPoints());
        }

        if (allPatrolPoints.Count == 0)
        {
            Debug.LogWarning("Aucun point de patrouille trouvé.");
        }
        else
        {
            patrolPoints = new Transform[allPatrolPoints.Count];
            for (int i = 0; i < allPatrolPoints.Count; i++)
            {
                GameObject temp = new GameObject($"PatrolPoint_{i}");
                temp.transform.position = allPatrolPoints[i];
                patrolPoints[i] = temp.transform;
            }
        }


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
                    Debug.Log("Joueur détecté en patrouille, passage en Suspicious");
                    currentState = MobState.Suspicious;
                    suspiciousTimer = 0f;
                    agent.speed = suspicionSpeed;
                    return;
                }
                break;

            case MobState.Suspicious:
                if (CanDetectPlayer())
                {
                    Debug.Log("Joueur détecté en Suspicious, passage en Chasing");
                    lastKnownPosition = player.position;
                    currentState = MobState.Chasing;
                    agent.speed = chaseSpeed;
                    return;
                }
                BeSuspicious();
                break;

            case MobState.Chasing:
                ChasePlayer();
                break;

            case MobState.Searching:
                if (CanDetectPlayer())
                {
                    Debug.Log("Joueur détecté en Searching, passage en Chasing");
                    lastKnownPosition = player.position;
                    currentState = MobState.Chasing;
                    agent.speed = chaseSpeed;
                    return; 
                }
                SearchForPlayer();
                break;

            case MobState.Attacking:
                AttackPlayer();
                break;
        }

        UpdateAnimator();
    }

    private void UpdateAnimator()
    {
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
    
        Vector3 targetPos = patrolPoints[currentPatrolIndex].position;

        NavMeshPath path = new NavMeshPath();
        agent.CalculatePath(targetPos, path);

        if (path.status != NavMeshPathStatus.PathComplete)
        {
            Debug.Log("🔒 Aucun chemin valide vers le point. Recherche de porte intermédiaire...");
            TryMoveTowardsClosestBlockingDoor(targetPos);
            return;
        }

        agent.destination = targetPos;
        currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        Debug.Log("Patrouille vers le point: " + currentPatrolIndex);
    }




    private void TryMoveTowardsClosestBlockingDoor(Vector3 patrolTarget)
    {
        currentBlockedTarget = patrolTarget; // On sauvegarde notre objectif initial
        float searchRadius = 50f;
        Collider[] hits = Physics.OverlapSphere(transform.position, searchRadius);

        Transform bestDoor = null;
        float bestDist = Mathf.Infinity;

        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Door")) continue;
            DoorInteract interact = hit.GetComponent<DoorInteract>();
            if (interact == null) continue;

            float dist = Vector3.Distance(transform.position, hit.transform.position);
            if (dist < bestDist)
            {
                bestDist = dist;
                bestDoor = hit.transform;
            }
        }

        if (bestDoor != null)
        {
            doorTarget = bestDoor;
            Debug.Log($"🚪 Mob se dirige vers la porte : {bestDoor.name}");
            agent.SetDestination(bestDoor.position);
            StartCoroutine(CheckDoorDistanceAndOpen());
        }
        else
        {
            Debug.LogWarning("Aucune porte trouvée autour du mob");
            currentBlockedTarget = null;
        }
    }
    
    private IEnumerator CheckDoorDistanceAndOpen()
    {
        waitingForDoor = true;

        while (doorTarget != null && Vector3.Distance(transform.position, doorTarget.position) > doorInteractionDistance)
            yield return null;

        if (doorTarget != null)
        {
            DoorInteract interact = doorTarget.GetComponent<DoorInteract>();
            NavMeshObstacle navObstacle = doorTarget.GetComponent<NavMeshObstacle>();
            bool doorWasOpenedByMob = false;

            if (interact != null)
            {
                if (!interact.IsOpen)
                {
                    Debug.Log("Mob détecte une porte fermée → ouverture nécessaire");
                    interact.Interact();
                    doorWasOpenedByMob = true;
                }
                else
                {
                    Debug.Log("Porte déjà ouverte → aucune interaction");
                }
            }

            if (doorWasOpenedByMob && navObstacle != null)
            {
                navObstacle.carving = false;
                Debug.Log("carving temporairement désactivé");

                yield return new WaitForSeconds(1.5f);

                navObstacle.carving = true;
                Debug.Log("carving réactivé");
            }
        }

        waitingForDoor = false;

        if (currentBlockedTarget.HasValue)
        {
            Debug.Log("Le mob reprend la patrouille vers l'objectif initial");
            agent.SetDestination(currentBlockedTarget.Value);
            currentBlockedTarget = null;
        }

        doorTarget = null;
    }


    private IEnumerator ResumePatrolAfterDoorOpened(float delay)
    {
        yield return new WaitForSeconds(delay); 
        GoToNextPatrolPoint();
    }

    
    private void BeSuspicious()
    {
        agent.isStopped = true;
        suspiciousTimer += Time.deltaTime;

        // Le mob reste simplement en place, pas besoin de rotation
        // On pourrait lui faire regarder autour ou rester immobile

        if (suspiciousTimer >= suspicionTime)
        {
            Debug.Log("Suspicion terminée, passage en Searching");
            if (!isPlayerSeen)
            {
                lastKnownPosition = transform.position;
            }
            currentState = MobState.Searching;
            PrepareInitialSearch();
            return;
        }
    }

    private void ChasePlayer()
    {
        agent.isStopped = false;
        agent.speed = chaseSpeed;
        agent.destination = player.position;
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        Debug.Log("Chasing le joueur, dist=" + distanceToPlayer + " pathStatus=" + agent.pathStatus);

        if (!agent.pathPending && distanceToPlayer <= agent.stoppingDistance)
        {
            Debug.Log("A portée d'attaque, passage en Attacking");
            currentState = MobState.Attacking;
            return;
        }

        if (!CanSeePlayer())
        {
            Debug.Log("Joueur perdu de vue, passage en Searching");
            isPlayerSeen = false;
            lastKnownPosition = player.position;
            currentState = MobState.Searching;
            PrepareInitialSearch();
            return;
        }
        else
        {
            lastKnownPosition = player.position;
            isPlayerSeen = true;
        }
    }

    private void PrepareInitialSearch()
    {
        Debug.Log("Préparation initiale de la recherche");
        searchTimer = 0f;
        searchPoints.Clear();
        currentSearchIndex = -1; 
        agent.isStopped = false;

        // On conserve la vitesse de chasse pour courir jusqu'au dernier point vu
        agent.speed = chaseSpeed;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(lastKnownPosition, out hit, 2f, NavMesh.AllAreas))
        {
            Vector3 navigablePos = hit.position;
            agent.destination = navigablePos;
            Debug.Log("Position navigable trouvée pour la recherche: " + navigablePos);
        }
        else
        {
            Debug.LogWarning("lastKnownPosition non navigable, on génère directement les points de recherche");
            GenerateSearchPoints(); // On génère directement les points si pas navigable
        }

        searchStartTime = Time.time;
    }

    private void GenerateSearchPoints()
    {
        Debug.Log("Génération de points de recherche supplémentaires (zone encore plus élargie)");
        searchPoints.Clear();
        // On élargit encore plus la zone, par exemple 15f
        for (int i = 0; i < 3; i++)
        {
            Vector3 randomOffset = Random.insideUnitSphere * 15f; 
            randomOffset.y = 0;
            searchPoints.Add(lastKnownPosition + randomOffset);
        }
        currentSearchIndex = 0;
        agent.isStopped = false;
        agent.destination = searchPoints[currentSearchIndex];
        Debug.Log("Point de recherche initial: " + searchPoints[currentSearchIndex]);
    }

    private void GoToNextSearchPoint()
    {
        currentSearchIndex++;
        if (currentSearchIndex < searchPoints.Count)
        {
            Debug.Log("Passage au point de recherche suivant: " + currentSearchIndex);
            agent.isStopped = false;
            agent.destination = searchPoints[currentSearchIndex];
        }
        else
        {
            Debug.Log("Aucun joueur trouvé, retour à la patrouille");
            ResetToPatrol();
        }
    }

    private void SearchForPlayer()
    {
        searchTimer += Time.deltaTime;

        if (searchTimer >= searchDuration)
        {
            Debug.Log("Temps de recherche écoulé, retour à la patrouille");
            ResetToPatrol();
            return;
        }

        Debug.Log("SearchForPlayer: pathStatus=" + agent.pathStatus + " remainingDistance=" + agent.remainingDistance);

        // Si après un certain temps on n'arrive pas à destination, on génère directement les points
        if (currentSearchIndex == -1) 
        {
            // On est dans la phase initiale (on va vers lastKnownPosition)
            if (!agent.pathPending && agent.remainingDistance > 0.5f && (Time.time - searchStartTime > maxTimeToReachTarget))
            {
                Debug.LogWarning("Impossible d'atteindre la destination initiale, on génère les points");
                GenerateSearchPoints();
            }
            else if (!agent.pathPending && agent.remainingDistance < 0.5f)
            {
                // Arrivé au dernier point vu
                Debug.Log("Arrivé au dernier point connu, génération des points de recherche");
                GenerateSearchPoints();
            }
        }
        else
        {
            // On est dans la phase des points de recherche
            if (!agent.pathPending && agent.remainingDistance < 0.5f)
            {
                // Arrivé à un point de recherche, passe au suivant
                GoToNextSearchPoint();
            }
        }
    }

    private void ResetToPatrol()
    {
        Debug.Log("Fin de recherche, retour à la patrouille");
        currentState = MobState.Patrolling;
        agent.isStopped = false;
        agent.speed = patrolSpeed;
        GoToNextPatrolPoint();
    }

    private void AttackPlayer()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer > agent.stoppingDistance)
        {
            Debug.Log("Joueur hors de portée, retour en Chasing");
            currentState = MobState.Chasing;
            agent.speed = chaseSpeed;
            return;
        }

        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0;
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);

        if (isAttacking) return;
        isAttacking = true;
        agent.isStopped = true;
        animator.SetTrigger("AttackTrigger");
        Invoke(nameof(EndAttack), attackDelay);
    }

    private void EndAttack()
    {
        isAttacking = false;
        agent.isStopped = false;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= agent.stoppingDistance)
        {
            currentState = MobState.Attacking;
        }
        else
        {
            currentState = MobState.Chasing;
            agent.speed = chaseSpeed;
        }
    }

    private bool CanDetectPlayer()
    {
        return CanSeePlayer() || CanHearPlayer();
    }

    private bool CanSeePlayer()
    {
        Vector3 origin = transform.position + eyeLevelOffset;
        Vector3 target = player.position + eyeLevelOffset;
        Vector3 directionToPlayer = (target - origin).normalized;
        float angle = Vector3.Angle(transform.forward, directionToPlayer);
        float distanceToPlayer = Vector3.Distance(origin, target);

        if (angle < viewAngle / 2 && distanceToPlayer < viewDistance)
        {
            if (Physics.Raycast(origin, directionToPlayer, out RaycastHit hit, viewDistance, ~obstacleMask))
            {
                if (hit.transform.CompareTag("Player"))
                {
                    // Vérifie si le joueur est caché
                    FirstPersonController playerController = player.GetComponent<FirstPersonController>();
                    if (playerController != null && playerController.isHidden)
                    {
                        Debug.Log("Le joueur est caché, l'IA ne le voit pas");
                        return false;
                    }
                    return true;
                }
            }
        }
        return false;
    }


    /// <summary>
    /// Détecte si le mob peut entendre le joueur en fonction de l'audio réel.
    /// </summary>
    private bool CanHearPlayer()
    {
        AudioSource playerAudio = player.GetComponent<AudioSource>();
        if (playerAudio != null && playerAudio.isPlaying)
        {
            float distance = Vector3.Distance(transform.position, player.position);
            if (distance < hearingRadius)
            {
                Debug.Log("Joueur entendu par le mob");
                FirstPersonController playerController = player.GetComponent<FirstPersonController>();
                if (playerController != null && playerController.isHidden)
                {
                    Debug.Log("Le joueur est caché, l'IA ne l'entend pas");
                    return false;
                }
                return true;
            }
        }
        return false;
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, hearingRadius);

        Vector3 viewLeft = Quaternion.Euler(0, -viewAngle / 2, 0) * transform.forward * viewDistance;
        Vector3 viewRight = Quaternion.Euler(0, viewAngle / 2, 0) * transform.forward * viewDistance;

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position + eyeLevelOffset, transform.position + eyeLevelOffset + viewLeft);
        Gizmos.DrawLine(transform.position + eyeLevelOffset, transform.position + eyeLevelOffset + viewRight);

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(lastKnownPosition, 0.2f);

        Gizmos.color = Color.green;
        foreach (var sp in searchPoints)
        {
            Gizmos.DrawWireSphere(sp, 0.3f);
        }
    }
}
