using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class GuardVisionCone : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GuardVision guardVision;

    [Header("Visual Quality")]
    [SerializeField]
    [Range(10, 120)]
    private int rayCount = 50;

    [Header("Colors")]
    [SerializeField]
    private Color normalColor =
        new Color(
            1f,
            0.9f,
            0.2f,
            0.16f
        );

    [SerializeField]
    private Color alertColor =
        new Color(
            1f,
            0.25f,
            0.05f,
            0.30f
        );

    private Mesh coneMesh;
    private MeshRenderer meshRenderer;
    private Material runtimeMaterial;

    private void Awake()
    {
        coneMesh = new Mesh();
        coneMesh.name = "Guard Vision Cone";

        GetComponent<MeshFilter>().mesh =
            coneMesh;

        meshRenderer =
            GetComponent<MeshRenderer>();

        // Creates an instance so changing this guard's
        // color does not modify the original material asset.
        runtimeMaterial =
            meshRenderer.material;
    }

    private void LateUpdate()
    {
        if (guardVision == null)
        {
            return;
        }

        BuildCone();
        UpdateColor();
    }

    private void BuildCone()
    {
        int vertexCount =
            rayCount + 2;

        Vector3[] vertices =
            new Vector3[vertexCount];

        int[] triangles =
            new int[rayCount * 3];

        Vector2 originWorld =
            guardVision.OriginPosition;

        // First vertex is the flashlight origin.
        vertices[0] =
            transform.InverseTransformPoint(
                originWorld
            );

        float halfAngle =
            guardVision.ViewAngle * 0.5f;

        for (int i = 0;
             i <= rayCount;
             i++)
        {
            float percentage =
                i / (float)rayCount;

            float angle =
                Mathf.Lerp(
                    -halfAngle,
                    halfAngle,
                    percentage
                );

            Vector2 direction =
                Quaternion.Euler(
                    0f,
                    0f,
                    angle
                ) *
                guardVision.Forward;

            RaycastHit2D hit =
                Physics2D.Raycast(
                    originWorld,
                    direction,
                    guardVision.ViewDistance,
                    guardVision.ObstacleMask
                );

            Vector2 endPoint;

            if (hit.collider != null)
            {
                endPoint = hit.point;
            }
            else
            {
                endPoint =
                    originWorld +
                    direction *
                    guardVision.ViewDistance;
            }

            vertices[i + 1] =
                transform.InverseTransformPoint(
                    endPoint
                );
        }

        for (int i = 0;
             i < rayCount;
             i++)
        {
            int triangleIndex =
                i * 3;

            /*
             * Reverse winding keeps the mesh facing
             * the normal 2D camera direction.
             */
            triangles[triangleIndex] = 0;
            triangles[triangleIndex + 1] = i + 2;
            triangles[triangleIndex + 2] = i + 1;
        }

        coneMesh.Clear();

        coneMesh.vertices =
            vertices;

        coneMesh.triangles =
            triangles;

        coneMesh.RecalculateBounds();
    }

    private void UpdateColor()
    {
        if (runtimeMaterial == null)
        {
            return;
        }

        runtimeMaterial.color =
            guardVision.CanSeePlayer
                ? alertColor
                : normalColor;
    }

    private void OnDestroy()
    {
        if (coneMesh != null)
        {
            Destroy(coneMesh);
        }

        if (runtimeMaterial != null)
        {
            Destroy(runtimeMaterial);
        }
    }
}