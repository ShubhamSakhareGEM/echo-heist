using UnityEngine;

public class GuardPatrol : MonoBehaviour
{
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float arriveDistance = 0.1f;

    private Transform currentTarget;

    private void Start()
    {
        currentTarget = pointA;
    }

    private void Update()
    {
        MoveToTarget();

        if (Vector2.Distance(transform.position, currentTarget.position) <= arriveDistance)
        {
            SwitchTarget();
        }
    }

    private void MoveToTarget()
    {
        Vector2 direction =
            ((Vector2)currentTarget.position - (Vector2)transform.position).normalized;

        if (direction != Vector2.zero)
        {
            transform.up = direction;
        }

        transform.position = Vector2.MoveTowards(
            transform.position,
            currentTarget.position,
            moveSpeed * Time.deltaTime
        );
    }

    private void SwitchTarget()
    {
        currentTarget = currentTarget == pointA ? pointB : pointA;
    }
}