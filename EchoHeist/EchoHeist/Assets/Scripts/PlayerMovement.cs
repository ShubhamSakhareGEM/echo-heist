using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Vector2 movement;
    private GameManager gameManager;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        gameManager =
            FindFirstObjectByType<GameManager>();
    }

    private void Update()
    {
        if (gameManager.GameEnded)
        {
            movement = Vector2.zero;
            return;
        }

        movement.x =
            Input.GetAxisRaw("Horizontal");

        movement.y =
            Input.GetAxisRaw("Vertical");

        movement = movement.normalized;
    }

    private void FixedUpdate()
    {
        rb.linearVelocity =
            movement * moveSpeed;
    }
}