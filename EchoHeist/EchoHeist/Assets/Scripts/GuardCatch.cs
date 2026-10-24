using UnityEngine;

public class GuardCatch : MonoBehaviour
{
    private GameManager gameManager;

    private void Start()
    {
        gameManager =
            FindFirstObjectByType<GameManager>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            gameManager.PlayerCaught();
        }
    }
}