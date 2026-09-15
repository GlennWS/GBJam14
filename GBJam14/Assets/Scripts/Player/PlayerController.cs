using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // Movement
    [SerializeField] private float moveSpeed = 5f;
    Vector2 movementInput;
    Vector2 movementDir;

    public bool isMoving { get; private set; }
    public bool inputEnabled { get; set; } = true;
    private Rigidbody2D rb;
    private SpriteRenderer sr;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        // static class to get direction?
        //movementInput = inputEnabled ?

        if (movementInput.x != 0f && movementInput.y != 0f)
        {
            if (movementDir.x != 0f)
            {
                movementInput.y = 0f;
            }
            else
            {
                movementInput.x = 0f;
            }
        }

        movementDir = movementInput;
        isMoving = movementDir != Vector2.zero;
    }

    private void FixedUpdate()
    {
        
    }
}