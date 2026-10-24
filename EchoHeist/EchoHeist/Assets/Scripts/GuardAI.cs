using System.Collections.Generic;
using UnityEngine;

public class GuardAI : MonoBehaviour
{
    private enum GuardState
    {
        Patrol,
        Chase,
        Search
    }

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private GuardVision guardVision;
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;
    private GameManager gameManager;

    [Header("Navigation")]
    [SerializeField] private NavNode[] navNodes;
    [SerializeField] private LayerMask obstacleMask;

    [SerializeField] private float nodeReachDistance = 0.15f;

    // Approximate radius of the guard.
    // Prevents the guard's body from clipping through wall corners.
    [SerializeField] private float movementClearanceRadius = 0.28f;

    // Rebuild A* only if the target has moved enough.
    [SerializeField] private float pathRebuildDistance = 0.5f;

    [Header("Movement")]
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float chaseSpeed = 3f;
    [SerializeField] private float arriveDistance = 0.1f;

    [Header("Search")]
    [SerializeField] private float searchDuration = 3f;
    [SerializeField] private float searchArrivalDistance = 0.3f;

    private GuardState currentState = GuardState.Patrol;

    private Transform currentPatrolTarget;

    private Vector2 lastKnownPlayerPosition;

    private float searchTimer;

    private List<NavNode> currentPath = new List<NavNode>();

    private int pathIndex;

    private Vector2 currentDestination;

    private bool hasPath;

    public string CurrentStateName => currentState.ToString();

    public Vector2 LastKnownPlayerPosition =>
        lastKnownPlayerPosition;

    public bool HasSeenPlayer { get; private set; }

    public bool HasActivePath => hasPath;

    public int RemainingPathNodes =>
        Mathf.Max(0, currentPath.Count - pathIndex);

    private void Start()
    {
        currentPatrolTarget = pointA;
        gameManager =FindFirstObjectByType<GameManager>();
    }

    private void Update()
    {
        if (gameManager.GameEnded)
        {
            return;
        }
        switch (currentState)
        {
            case GuardState.Patrol:
                Patrol();
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
            lastKnownPlayerPosition = player.position;
            HasSeenPlayer = true;

            ChangeState(GuardState.Chase);

            return;
        }

        bool reachedTarget = MoveUsingPath(
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
    // CHASE
    // =========================================================

    private void Chase()
    {
        if (!guardVision.CanSeePlayer)
        {
            ChangeState(GuardState.Search);
            return;
        }

        // Continually remember the player's latest visible position.
        lastKnownPlayerPosition = player.position;

        // If the guard can physically run directly toward the player,
        // do that instead of unnecessarily using navigation nodes.
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
            // A wall blocks the direct route.
            // Use A* to navigate around it.
            MoveUsingPath(
                player.position,
                chaseSpeed
            );
        }
    }

    // =========================================================
    // SEARCH
    // =========================================================

    private void Search()
    {
        // Player reappeared while searching.
        if (guardVision.CanSeePlayer)
        {
            lastKnownPlayerPosition = player.position;

            ChangeState(GuardState.Chase);

            return;
        }

        float distanceToLastKnownPosition =
            Vector2.Distance(
                transform.position,
                lastKnownPlayerPosition
            );

        // Continue pursuing the exact last location
        // where the player was seen.
        if (distanceToLastKnownPosition >
            searchArrivalDistance)
        {
            if (HasClearMovementPath(
                    transform.position,
                    lastKnownPlayerPosition))
            {
                // No obstacle between guard and last seen location.
                // Keep running naturally instead of returning to a node.
                ClearPath();

                MoveDirect(
                    lastKnownPlayerPosition,
                    chaseSpeed
                );
            }
            else
            {
                // Direct movement is blocked.
                // Use A* only when necessary.
                MoveUsingPath(
                    lastKnownPlayerPosition,
                    chaseSpeed
                );
            }

            return;
        }

        // Guard has reached the last known player location.
        // Stay here for a few seconds before giving up.
        searchTimer -= Time.deltaTime;

        if (searchTimer <= 0f)
        {
            ChangeState(GuardState.Patrol);
        }
    }

    // =========================================================
    // A* MOVEMENT
    // =========================================================

    private bool MoveUsingPath(
        Vector2 destination,
        float speed)
    {
        // Build/rebuild path if:
        // 1. We do not currently have one.
        // 2. The target has moved significantly.
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
        if (pathIndex < currentPath.Count)
        {
            Vector2 nodePosition =
                currentPath[pathIndex].Position;

            // Safety check:
            // even if the graph was configured incorrectly,
            // never knowingly move through a wall.
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

        // Navigation nodes are complete.
        // Move from final node toward the exact destination.
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
            destination) <= arriveDistance;
    }

    // =========================================================
    // BUILD A* PATH
    // =========================================================

    private bool BuildPath(Vector2 destination)
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

        currentDestination = destination;

        /*
         * A* includes the starting node.
         *
         * We don't automatically send the guard backward to it.
         * Instead, find the furthest node along the path that the
         * guard can already reach directly.
         *
         * Example:
         *
         * N1 ---- Guard ---- N2 ---- N3
         *
         * If Guard can already reach N2, it skips N1.
         */
        pathIndex = FindBestStartingPathIndex();

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

        /*
         * Try nodes from farther along the route back toward
         * the start.
         *
         * The first node that has a physically clear route
         * becomes the guard's next target.
         */
        for (int i = currentPath.Count - 1;
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

        // Should rarely happen because startNode was already
        // confirmed reachable.
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

        foreach (NavNode node in navNodes)
        {
            if (node == null)
            {
                continue;
            }

            // Check whether an object the size of the guard
            // can physically travel between this position
            // and the node.
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

            if (distance < closestDistance)
            {
                closestDistance = distance;

                closestNode = node;
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

        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        direction.Normalize();

        // Guard's local UP direction represents forward.
        transform.up = direction;

        transform.position =
            Vector2.MoveTowards(
                transform.position,
                destination,
                speed * Time.deltaTime
            );
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

        currentState = newState;

        ClearPath();

        if (currentState == GuardState.Search)
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

        Gizmos.color = Color.magenta;

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

            previous = next;
        }

        Gizmos.DrawLine(
            previous,
            currentDestination
        );
    }
}