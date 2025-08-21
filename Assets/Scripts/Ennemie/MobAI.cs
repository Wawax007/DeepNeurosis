    using System.Collections;
    using UnityEngine; 
    using UnityEngine.AI;
    using System.Collections.Generic;
    using PlayerScripts;

    /// <summary>
    /// IA de l’ennemi: patrouille, détection (vue/ouïe), poursuite, recherche et attaque,
    /// avec navigation NavMesh et gestion de sons de pas.
    /// </summary>
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
        public float chaseSpeed = 7.5f;
        public float suspicionSpeed = 4.5f;
        public float patrolSpeed = 2f;
        private float chaseStartTime = -999f;
        private const float chaseGrace = 0.4f;

        [Header("Attack Settings")]
        public float attackDelay = 0f;
        public float attackRange = 2.2f;
        
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

        public float aggressiveDetectionBonus = 10f;
        
        private float doorInteractionDistance = 2.5f;
        private float doorRetryCooldown = 2f;
        private float lastDoorInteractTime = -999f;
        private Transform lastInteractedDoor = null;
        private Vector3? currentBlockedTarget = null;
        private Transform doorTarget = null;
        private bool waitingForDoor = false;
        
        [Header("Footstep SFX")]
        public AudioClip[] footstepClips;
        public float footstepIntervalWalk = 1.2f;
        public float footstepIntervalRun = 0.6f;
        private float footstepTimer = 0f;
        private AudioSource audioSource;
        
        public MobState currentState = MobState.Patrolling;
        
        [Header("Stuck Detection")]
        private float stuckTimer = 0f;
        
        [Header("Hearing cache")]
        private float lastHeardTime   = -999f;
        private bool  currentlyHearing = false;
        public  float hearingMemory   = 0.5f;
        
        private float attackCooldown = 2f;
        private float lastAttackTime = -Mathf.Infinity;
        private NavMeshPath reusablePath;
        
        [Header("Footstep spam guard")]
        private float minFootstepGap = 0.15f;
        private float lastFootstepRealTime = -999f;

        
        private bool wasRunning = false;

        void Start()
        {
            reusablePath = new NavMeshPath();
            agent        = GetComponent<NavMeshAgent>();
            animator     = GetComponent<Animator>();

            agent.acceleration     = 45f;  
            agent.angularSpeed     = 720f;
            agent.autoRepath       = true;  
            agent.stoppingDistance = 0.3f;

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
            
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1f;
                audioSource.playOnAwake = false;
                
                audioSource.volume = 1f; // Plus sourd
                audioSource.spatialBlend = 1f;
                audioSource.playOnAwake = false;
                audioSource.minDistance = 1.5f;
                audioSource.maxDistance = 10f;
                audioSource.rolloffMode = AudioRolloffMode.Linear;

                var lowPass = gameObject.GetComponent<AudioLowPassFilter>();
                if (lowPass == null)
                    lowPass = gameObject.AddComponent<AudioLowPassFilter>();

                lowPass.cutoffFrequency = 800;
                
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
                        Debug.Log("Joueur détecté en Suspicious, passage en Chasing");
                        lastKnownPosition = player.position;
                        currentState      = MobState.Chasing;
                        chaseStartTime    = Time.time;
                        agent.isStopped   = false;
                        agent.ResetPath();
                        agent.speed       = chaseSpeed;
                        agent.autoBraking = false;
                        agent.stoppingDistance = 0.3f;
                        SetPathToPlayerOrDoor();
                        return;
                    }
                    break;

                case MobState.Suspicious:
                    if (CanDetectPlayer())
                    {
                        Debug.Log("Joueur détecté en Suspicious, passage en Chasing");
                        lastKnownPosition = player.position;
                        currentState      = MobState.Chasing;
                        chaseStartTime    = Time.time;
                        agent.isStopped   = false;
                        agent.ResetPath();
                        agent.speed       = chaseSpeed;
                        agent.autoBraking = false;
                        agent.stoppingDistance = 0.3f;
                        SetPathToPlayerOrDoor();
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
                        chaseStartTime         = Time.time;
                        agent.speed = chaseSpeed;
                        return; 
                    }
                    SearchForPlayer();
                    break;

                case MobState.Attacking:
                    AttackPlayer();
                    break;
            }
            
            HandleFootsteps();
            UpdateAnimator();
        }
        
        private void HandleFootsteps()
        {
            if (agent == null || audioSource == null) return;
            if (currentState == MobState.Attacking)   return;

            bool isMoving  = !agent.isStopped && agent.velocity.magnitude > 0.1f;
            bool isRunning = agent.speed >= chaseSpeed - 0.1f;

            if (!isMoving)
            {
                footstepTimer = 0f;
                return;
            }

            if (isRunning != wasRunning)
                footstepTimer = 0f;

            footstepTimer -= Time.deltaTime;
            float interval = isRunning ? footstepIntervalRun : footstepIntervalWalk;
            if (footstepTimer <= 0f)
            {
                footstepTimer = interval;
                PlayRandomFootstep(isRunning);
            }

            wasRunning = isRunning;
        }


        private void PlayRandomFootstep(bool running)
        {
            if (Time.time - lastFootstepRealTime < minFootstepGap)
                return;
            lastFootstepRealTime = Time.time;

            int idx = Random.Range(0, footstepClips.Length);
            if (audioSource.isPlaying) audioSource.Stop();
            audioSource.pitch = Random.Range(0.95f, 1.05f);
            audioSource.PlayOneShot(footstepClips[idx]);
        }

        
        private void UpdateAnimator()
        {
            float currentSpeed = agent.velocity.magnitude;
            animator.SetFloat("Speed", currentSpeed);
        }
        
        private void Patrol()
        {
            agent.isStopped = false;
            agent.speed     = patrolSpeed;

            if (IsStuck() && doorTarget == null)
            {
                TryMoveTowardsClosestBlockingDoor(patrolPoints[currentPatrolIndex].position);
                return;
            }

            if (!agent.pathPending && agent.remainingDistance < 0.5f)
            {
                GoToNextPatrolPoint();
            }
        }


        /// <summary>
        /// Calcule un chemin vers le prochain point de patrouille.
        /// Si le chemin est invalide ou partiel, tente un détour via la porte la plus proche.
        /// </summary>
        private void GoToNextPatrolPoint()
        {
            if (patrolPoints.Length == 0) return;

            Vector3 targetPos = patrolPoints[currentPatrolIndex].position;

            NavMeshPath path = reusablePath;          // chemin réutilisable (évite GC)
            agent.CalculatePath(targetPos, path);

            if (path.status != NavMeshPathStatus.PathComplete)
            {
                Debug.Log("🔒 Chemin incomplet vers le point, porte à trouver…");
                TryMoveTowardsClosestBlockingDoor(targetPos);
                return;
            }

            agent.destination = targetPos;
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
            Debug.Log($"Patrouille vers le point {currentPatrolIndex}");
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
        
        private Transform PickBestDoorTowards(Vector3 targetPos)
        {
            float bestScore = Mathf.Infinity;
            Transform bestDoor = null;

            Collider[] hits = Physics.OverlapSphere(transform.position, 50f);   // rayon de recherche
            foreach (var hit in hits)
            {
                if (!hit.CompareTag("Door")) continue;

                // ① distance NavMesh jusqu'à la porte
                if (!NavMesh.CalculatePath(transform.position, hit.transform.position, NavMesh.AllAreas, reusablePath))
                    continue;
                float toDoor = PathLength(reusablePath);

                // ② distance NavMesh de la porte jusqu'à la cible (joueur)
                if (!NavMesh.CalculatePath(hit.transform.position, targetPos, NavMesh.AllAreas, reusablePath))
                    continue;
                float doorToTarget = PathLength(reusablePath);

                float score = toDoor + doorToTarget;
                if (score < bestScore)
                {
                    bestScore = score;
                    bestDoor  = hit.transform;
                }
            }
            return bestDoor;
        }

        /// longueur totale d’un NavMeshPath
        private float PathLength(NavMeshPath p)
        {
            float d = 0f;
            for (int i = 1; i < p.corners.Length; i++)
                d += Vector3.Distance(p.corners[i - 1], p.corners[i]);
            return d;
        }
        
        private bool SetPathToPlayerOrDoor()
        {
            NavMeshPath path = reusablePath;
            agent.CalculatePath(player.position, path);

            if (path.status == NavMeshPathStatus.PathComplete)
            {
                agent.SetPath(path);
                doorTarget = null;          // plus de porte en attente
                return true;                // on peut courir directement
            }

            // Ici : chemin partiel *ou* invalide ⟹ on cherche la porte
            TryMoveTowardsClosestBlockingDoor(player.position);
            return false;
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

            if (currentState == MobState.Chasing)
            {
                agent.SetDestination(player.position);   // relance immédiate
            }
            else if (currentBlockedTarget.HasValue)
            {
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
            agent.speed     = chaseSpeed;

            /* -------------------------------------------------
               0) au tout début de la chasse, on oublie l’ancienne porte
               ------------------------------------------------- */
            if (Time.time - chaseStartTime < 0.1f)   // 1er ou 2ᵉ frame du state
            {
                doorTarget     = null;
                waitingForDoor = false;
            }

            /* -------------------------------------------------
               1) anti-blocage : on ignore IsStuck pendant 0,4 s
               ------------------------------------------------- */
            if (Time.time - chaseStartTime > chaseGrace)
            {
                if (IsStuck() && doorTarget == null)
                {
                    TryMoveTowardsClosestBlockingDoor(player.position);
                    return;
                }
            }

            /* -------------------------------------------------
               2) porte déjà repérée ?
               ------------------------------------------------- */
            if (doorTarget != null)
            {
                agent.SetDestination(doorTarget.position);

                if (!waitingForDoor)
                    StartCoroutine(CheckDoorDistanceAndOpen());
                return;
            }

            /* -------------------------------------------------
               3) chemin vers le joueur
               ------------------------------------------------- */
            if (!SetPathToPlayerOrDoor()) return;

            /* -------------------------------------------------
               4) transitions Attacking / Searching
               ------------------------------------------------- */
            float dist = Vector3.Distance(transform.position, player.position);

            if (!agent.pathPending && dist <= attackRange + 0.5f)
            {
                currentState = MobState.Attacking;
                return;
            }

            bool sees   = CanSeePlayer();
            bool hears  = CanHearPlayer();

            if (!sees && !hears)
            {
                isPlayerSeen      = false;
                lastKnownPosition = player.position;
                currentState      = MobState.Searching;
                PrepareInitialSearch();
            }
            else
            {
                lastKnownPosition = player.position;
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

            currentState           = MobState.Attacking;
            agent.isStopped        = true;
            agent.autoBraking      = true;
            agent.stoppingDistance = 2f;    
            
            if (distanceToPlayer > attackRange + 1f)
            {
                Debug.Log("Joueur hors de portée, retour en Chasing");
                currentState = MobState.Chasing;
                agent.speed = chaseSpeed;
                return;
            }

            if (Time.time - lastAttackTime < attackCooldown) return;

            lastAttackTime = Time.time;

            // Regarder le joueur
            Vector3 direction = (player.position - transform.position).normalized;
            direction.y = 0;
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);

            agent.isStopped = true;
            animator.SetTrigger("AttackTrigger");
            player.GetComponent<FirstPersonController>()?.SetCameraLock(true);
            CameraShake.Instance.Shake(0.5f, 0.5f);
            TriggerPlayerDeath();
        }


        private void TriggerPlayerDeath()
        {
            if (player == null) return;

            FirstPersonController playerController = player.GetComponent<FirstPersonController>();
            if (playerController != null && !playerController.isHidden)
            {
                DeathManager deathManager = FindObjectOfType<DeathManager>();
                if (deathManager != null && !deathManager.isDying)
                {
                    deathManager.TriggerDeath();
                }
            }

            currentState = MobState.Patrolling;
            agent.isStopped = false;
        }


        
        private void EndAttack()
        {
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
            Vector3 origin  = transform.position + eyeLevelOffset;
            Vector3 target  = player.position   + eyeLevelOffset;
            Vector3 dir     = (target - origin).normalized;

            if (Vector3.Angle(transform.forward, dir) > viewAngle * .5f) return false;
            if (Vector3.Distance(origin, target) > viewDistance)         return false;

            if (Physics.Raycast(origin, dir, out RaycastHit hit, viewDistance, ~obstacleMask))
            {
                if (hit.transform.CompareTag("Player")) return !player.GetComponent<FirstPersonController>().isHidden;

                if (hit.transform.CompareTag("Door"))
                {
                    doorTarget = hit.transform;
                    return false;
                }

            }
            return false;
        }


        private bool IsStuck(float seconds = 0.15f)
        {   
            if (agent.velocity.sqrMagnitude < 0.01f && !agent.pathPending)
            {
                stuckTimer += Time.deltaTime;
                return stuckTimer > seconds;
            }

            stuckTimer = 0f;
            return false;
        }

        
        private bool CanHearPlayer()
        {
            float now = Time.time;

            if (now - lastHeardTime < hearingMemory)
                return true;

            AudioSource playerAudio = player.GetComponent<AudioSource>();
            if (playerAudio != null && playerAudio.isPlaying)
            {
                float distance = Vector3.Distance(transform.position, player.position);
                if (distance < hearingRadius)
                {
                    FirstPersonController pc = player.GetComponent<FirstPersonController>();
                    if (pc != null && pc.isHidden) return false;

                    if (!currentlyHearing)
                        Debug.Log("Joueur entendu par le mob");

                    currentlyHearing = true;
                    lastHeardTime    = now;
                    return true;
                }
            }

            currentlyHearing = false;
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
        
        private Coroutine chaseRefresh;
        void OnEnable()  => chaseRefresh = StartCoroutine(RefreshChasePath());
        void OnDisable() => StopCoroutine(chaseRefresh);

        private IEnumerator RefreshChasePath()
        {
            var wait = new WaitForSeconds(0.25f);
            while (true)
            {
                if (currentState == MobState.Chasing && !waitingForDoor)
                    agent.SetDestination(player.position);
                yield return wait;
            }
        }
    }
