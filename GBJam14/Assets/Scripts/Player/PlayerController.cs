using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public enum Facing { Down, Up, Left, Right }

    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private int pixelsPerUnit = 8;

    public Facing CurrentFacing { get; private set; } = Facing.Down;
    public bool IsMoving { get; private set; }
    public bool InputEnabled { get; set; } = true;

    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer sr;
    private Vector2 moveDir;

    private static int MoveX = Animator.StringToHash("MoveX");
    private static int MoveY = Animator.StringToHash("MoveY");
    private static int Moving = Animator.StringToHash("Moving");

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
    }

    private void Update()
    {
        Vector2 input = InputEnabled ? GBInput.Direction : Vector2.zero;

        if (input.x != 0f && input.y != 0f)
        {
            if (moveDir.x != 0f)
            {
                input.y = 0f;
            }
            else
            {
                input.x = 0f;
            }
        }

        moveDir = input;
        IsMoving = moveDir != Vector2.zero;

        if (IsMoving)
        {
            if (moveDir.y < 0f)
            {
                CurrentFacing = Facing.Down;
            }
            else if (moveDir.y > 0f)
            {
                CurrentFacing = Facing.Up;
            }
            else if (moveDir.x < 0f)
            {
                CurrentFacing = Facing.Left;
            }
            else
            {
                CurrentFacing = Facing.Right;
            }
        }
        if (anim != null)
        {
            Vector2 face = CurrentFacing switch
            {
                Facing.Down => Vector2.down,
                Facing.Up => Vector2.up,
                Facing.Left => Vector2.left,
                Facing.Right => Vector2.right,
                _ => Vector2.down
            };
            anim.SetFloat(MoveX, face.x);
            anim.SetFloat(MoveY, face.y);
            anim.SetBool(Moving, IsMoving);
        }
    }

    private void FixedUpdate()
    {
        if (IsMoving)
        {
            rb.MovePosition(rb.position + moveDir * moveSpeed * Time.fixedDeltaTime);
        }
        else
        {
            rb.position = Snap(rb.position);
        }
    }

    private Vector2 Snap(Vector2 p)
    {
        float s = pixelsPerUnit;
        return new Vector2(Mathf.Round(p.x * s) / s, Mathf.Round(p.y * s) / s);
    }
}