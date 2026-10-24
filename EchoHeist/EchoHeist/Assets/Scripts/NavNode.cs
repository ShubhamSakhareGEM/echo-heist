using System.Collections.Generic;
using UnityEngine;

public class NavNode : MonoBehaviour
{
    [SerializeField] private List<NavNode> neighbors =
        new List<NavNode>();

    public IReadOnlyList<NavNode> Neighbors => neighbors;

    public Vector2 Position => transform.position;

    private void OnDrawGizmos()
    {
        // Draw node
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(transform.position, 0.12f);

        // Draw connections
        Gizmos.color = Color.green;

        foreach (NavNode neighbor in neighbors)
        {
            if (neighbor != null)
            {
                Gizmos.DrawLine(
                    transform.position,
                    neighbor.transform.position
                );
            }
        }
    }
}