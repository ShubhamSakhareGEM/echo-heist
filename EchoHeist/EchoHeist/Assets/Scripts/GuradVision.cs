using UnityEngine;

public class GuardVision : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform visionOrigin;

    [Header("Vision")]
    [SerializeField] private float viewDistance = 5f;

    [SerializeField]
    [Range(1f, 180f)]
    private float viewAngle = 90f;

    [SerializeField] private LayerMask obstacleMask;

    public bool CanSeePlayer { get; private set; }

    // Exposed for the visual flashlight cone.
    public float ViewDistance => viewDistance;
    public float ViewAngle => viewAngle;
    public LayerMask ObstacleMask => obstacleMask;

    public Vector2 OriginPosition =>
        visionOrigin != null
            ? (Vector2)visionOrigin.position
            : (Vector2)transform.position;

    public Vector2 Forward =>
        transform.up;

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
        if (player == null)
        {
            return false;
        }

        Vector2 origin = OriginPosition;

        Vector2 toPlayer =
            (Vector2)player.position - origin;

        float distanceToPlayer =
            toPlayer.magnitude;

        if (distanceToPlayer > viewDistance)
        {
            return false;
        }

        if (distanceToPlayer <= 0.01f)
        {
            return true;
        }

        Vector2 directionToPlayer =
            toPlayer.normalized;

        float dot =
            Vector2.Dot(
                Forward,
                directionToPlayer
            );

        float minimumDot =
            Mathf.Cos(
                viewAngle * 0.5f *
                Mathf.Deg2Rad
            );

        if (dot < minimumDot)
        {
            return false;
        }

        RaycastHit2D wallHit =
            Physics2D.Raycast(
                origin,
                directionToPlayer,
                distanceToPlayer,
                obstacleMask
            );

        return wallHit.collider == null;
    }

    private void OnDrawGizmos()
    {
        Vector2 origin =
            visionOrigin != null
                ? (Vector2)visionOrigin.position
                : (Vector2)transform.position;

        Vector3 leftDirection =
            Quaternion.Euler(
                0f,
                0f,
                viewAngle * 0.5f
            ) * transform.up;

        Vector3 rightDirection =
            Quaternion.Euler(
                0f,
                0f,
                -viewAngle * 0.5f
            ) * transform.up;

        Gizmos.color = Color.yellow;

        Gizmos.DrawRay(
            origin,
            leftDirection * viewDistance
        );

        Gizmos.DrawRay(
            origin,
            rightDirection * viewDistance
        );
    }
}