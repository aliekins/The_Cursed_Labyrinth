using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
public class TopDownController : MonoBehaviour
{
    [SerializeField] float moveSpeed = 4f;

    Rigidbody2D rb;
    SpriteRenderer sr;
    Animator animator;

    Vector2 lastDir = Vector2.down; // default face front
    Vector2 moveDir = Vector2.zero; 

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        // Read keys
        float x = 0f, y = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) x -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) x += 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) y -= 1f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) y += 1f;

        // Axis - lock (no diagonals)
        Vector2 dir = Vector2.zero;
        if (Mathf.Abs(x) > 0.01f || Mathf.Abs(y) > 0.01f)
        {
            if (Mathf.Abs(x) > Mathf.Abs(y)) dir = new Vector2(Mathf.Sign(x), 0f);
            else dir = new Vector2(0f, Mathf.Sign(y));
        }
        moveDir = dir;

        // Remember last facing while moving
        if (moveDir.sqrMagnitude > 0f)
        {
            lastDir = moveDir;
        }

        // Animator: always feed lastDir when idle
        Vector2 animDir = lastDir;
        if (moveDir.sqrMagnitude > 0f)
        {
            animDir = moveDir;
        }

        animator.SetBool("IsMoving", moveDir.sqrMagnitude > 0f);
        animator.SetFloat("MoveX", animDir.x);
        animator.SetFloat("MoveY", animDir.y);

        // Flip horizontally so left clips cover right
        if (Mathf.Abs(animDir.x) > 0.01f)
            sr.flipX = animDir.x > 0f; // right = flipped
    }

    void FixedUpdate()
    {
        rb.linearVelocity = moveDir * moveSpeed;
    }
}
