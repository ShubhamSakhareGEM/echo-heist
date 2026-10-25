using System.Collections.Generic;
using UnityEngine;

public class GuardAI : MonoBehaviour
{
    private enum GuardState
    {
        Patrol,
        AlertSearch,
        Chase,
        Search
    }

    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private GuardVision guardVision;
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;
    [SerializeField] private Transform artifact;

    private GameManager gameManager;

    // =========================================================
    // NAVIGATION
    // =========================================================

    [Header("Navigation")]
    [SerializeField] private NavNode[] navNodes;
    [SerializeField] private LayerMask obstacleMask;

    [SerializeField] private float nodeReachDistance = 0.15f;
    [SerializeField] private float movementClearanceRadius = 0.28f;
    [SerializeField] private float pathRebuildDistance = 0.5f;

    // =========================================================
    // MOVEMENT
    // =========================================================

    [Header("Movement")]
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float chaseSpeed = 3f;
    [SerializeField] private float arriveDistance = 0.1f;

    // =========================================================
    // PLAYER SEARCH
    // =========================================================

    [Header("Search")]
    [SerializeField] private float searchDuration = 3f;
    [SerializeField] private float searchArrivalDistance = 0.3f;

    // =========================================================
    // ALERT SEARCH AFTER ARTIFACT THEFT
    // =========================================================

    [Header("Alert Search")]
    [SerializeField] private float alertSearchSpeed = 2.4f;

    // Time spent rotating/checking at each search node.
    [SerializeField] private float alertCheckDuration = 1f;

    // At 360 with a 1-second check, guard makes one full scan.
    [SerializeField] private float alertLookTurnSpeed = 360f;

    // Small pause after theft so guard visibly snaps attention
    // toward the artifact before path movement begins.
    [SerializeField] private float artifactReactionDuration = 0.35f;

    // =========================================================
    // STATE
    // =========================================================

    private GuardState currentState = GuardState.Patrol;

    private Transform currentPatrolTarget;

    private Vector2 lastKnownPlayerPosition;
    private float searchTimer;

    // Artifact position is cached because the artifact object
    // may disappear after collection.
    private Vector2 artifactPosition;
    private bool hasArtifactPosition;

    // Ensures artifact theft triggers only once.
    private bool artifactAlertTriggered;

    // Alert-search state.
    private int alertSearchNodeIndex;
    private bool alertSweepInitialized;

    private bool investigatingArtifact;
    private float artifactReactionTimer;

    private bool checkingAlertPoint;
    private float alertCheckTimer;

    // =========================================================
    // A* PATH
    // =========================================================

    private List<NavNode> currentPath =
        new List<NavNode>();

    private int pathIndex;
    private Vector2 currentDestination;
    private bool hasPath;

    // =========================================================
    // DEBUG INFORMATION
    // =========================================================

    public string CurrentStateName =>
        currentState.ToString();

    public Vector2 LastKnownPlayerPosition =>
        lastKnownPlayerPosition;

    public bool HasSeenPlayer { get; private set; }

    public bool HasActivePath =>
        hasPath;

    public int RemainingPathNodes =>
        Mathf.Max(
            0,
            currentPath.Count - pathIndex
        );

    // =========================================================
    // UNITY
    // =========================================================

    private void Start()
    {
        currentPatrolTarget = pointA;

        gameManager =
            FindFirstObjectByType<GameManager>();

        if (artifact != null)
        {
            artifactPosition =
                artifact.position;

            hasArtifactPosition = true;
        }
        else
        {
            Debug.LogWarning(
                "GuardAI: Artifact reference is not assigned."
            );
        }
    }

    private void Update()
    {
        if (gameManager == null ||
            gameManager.GameEnded)
        {
            return;
        }

        // =====================================================
        // ARTIFACT THEFT EVENT
        // =====================================================

        if (gameManager.HasArtifact &&
            !artifactAlertTriggered)
        {
            artifactAlertTriggered = true;

            /*
             * If the guard already sees the thief,
             * chase immediately.
             *
             * Otherwise immediately turn toward
             * the stolen artifact and investigate.
             */
            if (guardVision.CanSeePlayer)
            {
                lastKnownPlayerPosition =
                    player.position;

                HasSeenPlayer = true;

                ChangeState(
                    GuardState.Chase
                );
            }
            else
            {
                BeginArtifactInvestigation();
            }
        }

        // =====================================================
        // STATE UPDATE
        // =====================================================

        switch (currentState)
        {
            case GuardState.Patrol:
                Patrol();
                break;

            case GuardState.AlertSearch:
                AlertSearch();
                break;

            case GuardState.Chase:
                Chase();
                break;

            case GuardState.Search:
                Search();
                break;
        }
    }

    // =========================================================
    // PATROL
    // =========================================================

    private void Patrol()
    {
        if (guardVision.CanSeePlayer)
        {
            lastKnownPlayerPosition =
                player.position;

            HasSeenPlayer = true;

            ChangeState(
                GuardState.Chase
            );

            return;
        }

        // Safety fallback:
        // once artifact is gone, never return to relaxed A/B patrol.
        if (gameManager.HasArtifact)
        {
            StartAlertSweepFromFirstNode();

            ChangeState(
                GuardState.AlertSearch
            );

            return;
        }

        bool reachedTarget =
            MoveUsingPath(
                currentPatrolTarget.position,
                patrolSpeed
            );

        if (reachedTarget)
        {
            currentPatrolTarget =
                currentPatrolTarget == pointA
                    ? pointB
                    : pointA;

            ClearPath();
        }
    }

    // =========================================================
    // ARTIFACT INVESTIGATION
    // =========================================================

    private void BeginArtifactInvestigation()
    {
        ClearPath();

        checkingAlertPoint = false;
        alertCheckTimer = 0f;

        alertSweepInitialized = false;

        if (!hasArtifactPosition)
        {
            investigatingArtifact = false;

            StartAlertSweepFromFirstNode();

            ChangeState(
                GuardState.AlertSearch
            );

            return;
        }

        investigatingArtifact = true;

        /*
         * Hold this direction briefly before moving.
         * This makes the guard visibly react to the theft.
         */
        artifactReactionTimer =
            artifactReactionDuration;

        FacePosition(
            artifactPosition
        );

        ChangeState(
            GuardState.AlertSearch
        );

        Debug.Log(
            "ARTIFACT STOLEN - GUARD TURNING TOWARD ARTIFACT"
        );
    }

    // =========================================================
    // ALERT SEARCH
    // =========================================================

    private void AlertSearch()
    {
        // Player becomes visible during any alert-search phase.
        if (guardVision.CanSeePlayer)
        {
            lastKnownPlayerPosition =
                player.position;

            HasSeenPlayer = true;

            checkingAlertPoint = false;

            ChangeState(
                GuardState.Chase
            );

            return;
        }

        // =====================================================
        // PHASE 1:
        // CHECK THE STOLEN ARTIFACT LOCATION
        // =====================================================

        if (investigatingArtifact)
        {
            InvestigateArtifactLocation();
            return;
        }

        // =====================================================
        // PHASE 2:
        // SYSTEMATIC NODE SWEEP
        // =====================================================

        if (navNodes == null ||
            navNodes.Length == 0)
        {
            return;
        }

        if (!alertSweepInitialized)
        {
            StartAlertSweepFromFirstNode();
        }

        // Stop at each node and scan around.
        if (checkingAlertPoint)
        {
            CheckCurrentLocation();

            if (alertCheckTimer <= 0f)
            {
                checkingAlertPoint = false;

                MoveToNextAlertNode();
            }

            return;
        }

        if (alertSearchNodeIndex < 0 ||
            alertSearchNodeIndex >= navNodes.Length)
        {
            alertSearchNodeIndex = 0;
        }

        NavNode targetNode =
            navNodes[alertSearchNodeIndex];

        if (targetNode == null)
        {
            MoveToNextAlertNode();
            return;
        }

        bool reachedNode =
            MoveUsingPath(
                targetNode.Position,
                alertSearchSpeed
            );

        if (reachedNode)
        {
            StartLocationCheck();

            Debug.Log(
                "GUARD CHECKING NODE: " +
                targetNode.name
            );
        }
    }

    // =========================================================
    // CHECK STOLEN ARTIFACT LOCATION
    // =========================================================

    private void InvestigateArtifactLocation()
    {
        // First visibly react to the theft.
        if (artifactReactionTimer > 0f)
        {
            artifactReactionTimer -= Time.deltaTime;

            FacePosition(artifactPosition);

            return;
        }

        // Already reached artifact area.
        // Stop and scan around.
        if (checkingAlertPoint)
        {
            CheckCurrentLocation();

            if (alertCheckTimer <= 0f)
            {
                checkingAlertPoint = false;
                investigatingArtifact = false;

                StartAlertSweepFromFirstNode();

                Debug.Log(
                    "ARTIFACT CHECK COMPLETE - STARTING FULL SWEEP"
                );
            }

            return;
        }

        bool reachedArtifact;

        // IMPORTANT:
        // If artifact can be reached directly,
        // immediately reverse direction and go there.
        if (HasClearMovementPath(
                transform.position,
                artifactPosition))
        {
            ClearPath();

            MoveDirect(
                artifactPosition,
                alertSearchSpeed
            );

            reachedArtifact =
                Vector2.Distance(
                    transform.position,
                    artifactPosition
                ) <= arriveDistance;
        }
        else
        {
            // Only use A* if a wall actually blocks
            // direct movement to the artifact.
            reachedArtifact =
                MoveUsingPath(
                    artifactPosition,
                    alertSearchSpeed
                );
        }

        if (reachedArtifact)
        {
            StartLocationCheck();

            Debug.Log(
                "GUARD CHECKING STOLEN ARTIFACT LOCATION"
            );
        }
    }

    // =========================================================
    // START ALERT SWEEP FROM ARRAY ELEMENT 0
    // =========================================================

    private void StartAlertSweepFromFirstNode()
    {
        if (navNodes == null ||
            navNodes.Length == 0)
        {
            return;
        }

        alertSearchNodeIndex = 0;

        alertSweepInitialized = true;

        checkingAlertPoint = false;

        ClearPath();
    }

    // =========================================================
    // RESUME AFTER LOSING PLAYER
    // =========================================================

    private void ResumeAlertSearchAfterLastKnownPosition()
    {
        investigatingArtifact = false;
        checkingAlertPoint = false;

        if (navNodes == null ||
            navNodes.Length == 0)
        {
            return;
        }

        /*
         * Find which search node is closest to where
         * the player disappeared.
         *
         * That area has effectively just been searched.
         */
        int lastCheckedNodeIndex =
            FindClosestNavNodeIndexToPosition(
                lastKnownPlayerPosition
            );

        /*
         * Continue from the NEXT node in the Inspector
         * search order.
         *
         * Example:
         *
         * Array:
         * N1, N4, N5, N10, ..., N2
         *
         * Player disappears near N1:
         *
         * N1 has effectively been checked,
         * so next target becomes N4.
         */
        alertSearchNodeIndex =
            lastCheckedNodeIndex + 1;

        if (alertSearchNodeIndex >=
            navNodes.Length)
        {
            alertSearchNodeIndex = 0;
        }

        alertSweepInitialized = true;

        ClearPath();

        ChangeState(
            GuardState.AlertSearch
        );

        if (navNodes[alertSearchNodeIndex] != null)
        {
            Debug.Log(
                "RESUMING ALERT SEARCH AT: " +
                navNodes[alertSearchNodeIndex].name
            );
        }
    }

    // =========================================================
    // CHECK A SEARCH LOCATION
    // =========================================================

    private void StartLocationCheck()
    {
        ClearPath();

        checkingAlertPoint = true;

        alertCheckTimer =
            alertCheckDuration;
    }

    private void CheckCurrentLocation()
    {
        alertCheckTimer -=
            Time.deltaTime;

        /*
         * Rotate while stationary.
         * GuardVision uses transform.up,
         * so the flashlight/FOV also scans around.
         */
        transform.Rotate(
            0f,
            0f,
            alertLookTurnSpeed *
            Time.deltaTime
        );
    }

    // =========================================================
    // NEXT ALERT NODE
    // =========================================================

    private void MoveToNextAlertNode()
    {
        if (navNodes == null ||
            navNodes.Length == 0)
        {
            return;
        }

        /*
         * Search follows the exact Inspector array order.
         *
         * With your current setup:
         *
         * N1
         * N4
         * N5
         * N10
         * N9
         * N11
         * N7
         * N6
         * N8
         * N3
         * N2
         * then back to N1.
         */

        alertSearchNodeIndex++;

        if (alertSearchNodeIndex >=
            navNodes.Length)
        {
            alertSearchNodeIndex = 0;
        }

        ClearPath();
    }

    // =========================================================
    // FIND NODE NEAREST TO A SPECIFIC POSITION
    // =========================================================

    private int FindClosestNavNodeIndexToPosition(
        Vector2 position)
    {
        if (navNodes == null ||
            navNodes.Length == 0)
        {
            return 0;
        }

        int closestIndex = 0;

        float closestDistance =
            float.PositiveInfinity;

        for (int i = 0;
             i < navNodes.Length;
             i++)
        {
            if (navNodes[i] == null)
            {
                continue;
            }

            float distance =
                Vector2.Distance(
                    position,
                    navNodes[i].Position
                );

            if (distance <
                closestDistance)
            {
                closestDistance =
                    distance;

                closestIndex = i;
            }
        }

        return closestIndex;
    }

    // =========================================================
    // CHASE
    // =========================================================

    private void Chase()
    {
        if (!guardVision.CanSeePlayer)
        {
            ChangeState(
                GuardState.Search
            );

            return;
        }

        // Remember latest visible position.
        lastKnownPlayerPosition =
            player.position;

        HasSeenPlayer = true;

        // Direct chase if no wall blocks movement.
        if (HasClearMovementPath(
                transform.position,
                player.position))
        {
            ClearPath();

            MoveDirect(
                player.position,
                chaseSpeed
            );
        }
        else
        {
            // Wall blocks direct route.
            // Navigate with A*.
            MoveUsingPath(
                player.position,
                chaseSpeed
            );
        }
    }

    // =========================================================
    // SEARCH LAST KNOWN PLAYER POSITION
    // =========================================================

    private void Search()
    {
        // Player reappears.
        if (guardVision.CanSeePlayer)
        {
            lastKnownPlayerPosition =
                player.position;

            HasSeenPlayer = true;

            ChangeState(
                GuardState.Chase
            );

            return;
        }

        float distanceToLastKnownPosition =
            Vector2.Distance(
                transform.position,
                lastKnownPlayerPosition
            );

        /*
         * Continue toward exact place where the
         * player disappeared.
         */
        if (distanceToLastKnownPosition >
            searchArrivalDistance)
        {
            if (HasClearMovementPath(
                    transform.position,
                    lastKnownPlayerPosition))
            {
                ClearPath();

                MoveDirect(
                    lastKnownPlayerPosition,
                    chaseSpeed
                );
            }
            else
            {
                MoveUsingPath(
                    lastKnownPlayerPosition,
                    chaseSpeed
                );
            }

            return;
        }

        // Reached the last known location.
        searchTimer -=
            Time.deltaTime;

        if (searchTimer <= 0f)
        {
            if (gameManager.HasArtifact)
            {
                /*
                 * Player has disappeared and artifact
                 * is already stolen.
                 *
                 * Treat this nearby node as checked,
                 * then continue with the NEXT node
                 * in the Inspector search sequence.
                 */
                ResumeAlertSearchAfterLastKnownPosition();
            }
            else
            {
                ChangeState(
                    GuardState.Patrol
                );
            }
        }
    }

    // =========================================================
    // A* MOVEMENT
    // =========================================================

    private bool MoveUsingPath(
        Vector2 destination,
        float speed)
    {
        if (!hasPath ||
            Vector2.Distance(
                currentDestination,
                destination) >
            pathRebuildDistance)
        {
            if (!BuildPath(destination))
            {
                return false;
            }
        }

        // Follow navigation nodes.
        if (pathIndex <
            currentPath.Count)
        {
            Vector2 nodePosition =
                currentPath[pathIndex]
                    .Position;

            // Safety check against wall clipping.
            if (!HasClearMovementPath(
                    transform.position,
                    nodePosition))
            {
                ClearPath();

                return false;
            }

            MoveDirect(
                nodePosition,
                speed
            );

            if (Vector2.Distance(
                    transform.position,
                    nodePosition)
                <= nodeReachDistance)
            {
                pathIndex++;
            }

            return false;
        }

        // Final movement from last path node
        // to the exact requested destination.
        if (!HasClearMovementPath(
                transform.position,
                destination))
        {
            ClearPath();

            return false;
        }

        MoveDirect(
            destination,
            speed
        );

        return Vector2.Distance(
            transform.position,
            destination)
            <= arriveDistance;
    }

    // =========================================================
    // BUILD A* PATH
    // =========================================================

    private bool BuildPath(
        Vector2 destination)
    {
        NavNode startNode =
            FindClosestReachableNode(
                transform.position
            );

        NavNode goalNode =
            FindClosestReachableNode(
                destination
            );

        if (startNode == null)
        {
            Debug.LogWarning(
                "Guard could not find a reachable START navigation node."
            );

            ClearPath();

            return false;
        }

        if (goalNode == null)
        {
            Debug.LogWarning(
                "Guard could not find a reachable GOAL navigation node."
            );

            ClearPath();

            return false;
        }

        currentPath =
            AStarPathfinder.FindPath(
                startNode,
                goalNode
            );

        if (currentPath.Count == 0)
        {
            Debug.LogWarning(
                "A* could not find a path."
            );

            ClearPath();

            return false;
        }

        currentDestination =
            destination;

        /*
         * A* includes starting node.
         *
         * Skip unnecessary backwards movement by
         * choosing the furthest path node that the
         * guard can already directly reach.
         */
        pathIndex =
            FindBestStartingPathIndex();

        hasPath = true;

        return true;
    }

    // =========================================================
    // REMOVE UNNECESSARY BACKTRACKING
    // =========================================================

    private int FindBestStartingPathIndex()
    {
        if (currentPath == null ||
            currentPath.Count == 0)
        {
            return 0;
        }

        for (int i =
                 currentPath.Count - 1;
             i >= 0;
             i--)
        {
            if (HasClearMovementPath(
                    transform.position,
                    currentPath[i].Position))
            {
                return i;
            }
        }

        return 0;
    }

    // =========================================================
    // FIND CLOSEST REACHABLE NODE
    // =========================================================

    private NavNode FindClosestReachableNode(
        Vector2 position)
    {
        NavNode closestNode = null;

        float closestDistance =
            float.PositiveInfinity;

        foreach (NavNode node
                 in navNodes)
        {
            if (node == null)
            {
                continue;
            }

            if (!HasClearMovementPath(
                    position,
                    node.Position))
            {
                continue;
            }

            float distance =
                Vector2.Distance(
                    position,
                    node.Position
                );

            if (distance <
                closestDistance)
            {
                closestDistance =
                    distance;

                closestNode =
                    node;
            }
        }

        return closestNode;
    }

    // =========================================================
    // WALL / CLEARANCE CHECK
    // =========================================================

    private bool HasClearMovementPath(
        Vector2 start,
        Vector2 destination)
    {
        Vector2 direction =
            destination - start;

        float distance =
            direction.magnitude;

        if (distance <= 0.01f)
        {
            return true;
        }

        RaycastHit2D hit =
            Physics2D.CircleCast(
                start,
                movementClearanceRadius,
                direction.normalized,
                distance,
                obstacleMask
            );

        return hit.collider == null;
    }

    // =========================================================
    // BASIC MOVEMENT
    // =========================================================

    private void MoveDirect(
        Vector2 destination,
        float speed)
    {
        Vector2 direction =
            destination -
            (Vector2)transform.position;

        if (direction.sqrMagnitude <=
            0.0001f)
        {
            return;
        }

        direction.Normalize();

        // Guard's local UP represents forward.
        transform.up =
            direction;

        transform.position =
            Vector2.MoveTowards(
                transform.position,
                destination,
                speed * Time.deltaTime
            );
    }

    // =========================================================
    // FACE POSITION WITHOUT MOVING
    // =========================================================

    private void FacePosition(
        Vector2 target)
    {
        Vector2 direction =
            target -
            (Vector2)transform.position;

        if (direction.sqrMagnitude <=
            0.0001f)
        {
            return;
        }

        transform.up =
            direction.normalized;
    }

    // =========================================================
    // STATE CHANGES
    // =========================================================

    private void ChangeState(
        GuardState newState)
    {
        if (currentState == newState)
        {
            return;
        }

        currentState =
            newState;

        ClearPath();

        if (currentState ==
            GuardState.Search)
        {
            searchTimer =
                searchDuration;
        }

        Debug.Log(
            "GUARD STATE: " +
            currentState
        );
    }

    // =========================================================
    // CLEAR CURRENT PATH
    // =========================================================

    private void ClearPath()
    {
        currentPath.Clear();

        pathIndex = 0;

        hasPath = false;
    }

    // =========================================================
    // DEBUG A* PATH
    // =========================================================

    private void OnDrawGizmos()
    {
        if (currentPath == null ||
            currentPath.Count == 0 ||
            !hasPath)
        {
            return;
        }

        Gizmos.color =
            Color.magenta;

        Vector3 previous =
            transform.position;

        for (int i = pathIndex;
             i < currentPath.Count;
             i++)
        {
            Vector3 next =
                currentPath[i]
                    .transform.position;

            Gizmos.DrawLine(
                previous,
                next
            );

            previous =
                next;
        }

        Gizmos.DrawLine(
            previous,
            currentDestination
        );
    }
}