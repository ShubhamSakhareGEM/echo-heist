using UnityEngine;

public class GuardVision : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private float viewDistance = 5f;
    [SerializeField] private float viewAngle = 90f;
    [SerializeField] private LayerMask obstacleMask;

    public bool CanSeePlayer { get; private set; }

    private bool wasSeeingPlayer;

    private void Update()
    {
        CanSeePlayer = CheckForPlayer();

        if (CanSeePlayer && !wasSeeingPlayer)
        {
            Debug.Log("PLAYER DETECTED");
        }

        if (!CanSeePlayer && wasSeeingPlayer)
        {
            Debug.Log("PLAYER LOST");
        }

        wasSeeingPlayer = CanSeePlayer;
    }

    private bool CheckForPlayer()
    {
        Vector2 directionToPlayer =
            (player.position - transform.position).normalized;

        float distanceToPlayer =
            Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer > viewDistance)
        {
            return false;
        }

        float dot =
            Vector2.Dot(transform.up, directionToPlayer);

        float minimumDot =
            Mathf.Cos(viewAngle * 0.5f * Mathf.Deg2Rad);

        if (dot < minimumDot)
        {
            return false;
        }

        RaycastHit2D wallHit = Physics2D.Raycast(
            transform.position,
            directionToPlayer,
            distanceToPlayer,
            obstacleMask
        );

        return wallHit.collider == null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        Vector3 leftDirection =
            Quaternion.Euler(0, 0, viewAngle / 2f) * transform.up;

        Vector3 rightDirection =
            Quaternion.Euler(0, 0, -viewAngle / 2f) * transform.up;

        Gizmos.DrawRay(
            transform.position,
            leftDirection * viewDistance
        );

        Gizmos.DrawRay(
            transform.position,
            rightDirection * viewDistance
        );
    }
}