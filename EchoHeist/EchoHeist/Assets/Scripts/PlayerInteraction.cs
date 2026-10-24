using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    private GameManager gameManager;

    private void Start()
    {
        gameManager = FindFirstObjectByType<GameManager>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Artifact"))
        {
            gameManager.CollectArtifact();

            Destroy(other.gameObject);
        }

        if (other.CompareTag("Exit"))
        {
            gameManager.TryEscape();
        }
    }
}