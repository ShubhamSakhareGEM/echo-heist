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

    [Header("Movement")]
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float chaseSpeed = 3f;
    [SerializeField] private float arriveDistance = 0.1f;

    [Header("Search")]
    [SerializeField] private float searchDuration = 3f;

    private GuardState currentState = GuardState.Patrol;
    private Transform currentPatrolTarget;

    private Vector2 lastKnownPlayerPosition;
    private float searchTimer;

    private void Start()
    {
        currentPatrolTarget = pointA;
    }

    private void Update()
    {
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

    private void Patrol()
    {
        if (guardVision.CanSeePlayer)
        {
            ChangeState(GuardState.Chase);
            return;
        }

        MoveTowards(currentPatrolTarget.position, patrolSpeed);

        if (Vector2.Distance(
                transform.position,
                currentPatrolTarget.position) <= arriveDistance)
        {
            currentPatrolTarget =
                currentPatrolTarget == pointA ? pointB : pointA;
        }
    }

    private void Chase()
    {
        if (guardVision.CanSeePlayer)
        {
            lastKnownPlayerPosition = player.position;

            MoveTowards(player.position, chaseSpeed);
        }
        else
        {
            searchTimer = searchDuration;
            ChangeState(GuardState.Search);
        }
    }

    private void Search()
    {
        if (guardVision.CanSeePlayer)
        {
            ChangeState(GuardState.Chase);
            return;
        }

        MoveTowards(lastKnownPlayerPosition, patrolSpeed);

        if (Vector2.Distance(
                transform.position,
                lastKnownPlayerPosition) <= arriveDistance)
        {
            searchTimer -= Time.deltaTime;

            if (searchTimer <= 0f)
            {
                ChangeState(GuardState.Patrol);
            }
        }
    }

    private void MoveTowards(Vector2 target, float speed)
    {
        Vector2 direction =
            (target - (Vector2)transform.position).normalized;

        if (direction != Vector2.zero)
        {
            transform.up = direction;
        }

        transform.position = Vector2.MoveTowards(
            transform.position,
            target,
            speed * Time.deltaTime
        );
    }

    private void ChangeState(GuardState newState)
    {
        if (currentState == newState)
            return;

        currentState = newState;

        Debug.Log("GUARD STATE: " + currentState);
    }
}