using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
public class TopDownController : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Movement speed in units/second.")]
    private float moveSpeed = 4f;
    public Vector2 FacingDir { get; private set; } = Vector2.down;

    // Cached components (assigned in Awake)
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Animator animator;

    private Vector2 lastDir = Vector2.down;
    private Vector2 moveDir = Vector2.zero;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        // Read keyboard (WASD + Arrow keys)
        float x = 0f, y = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) x -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) x += 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) y -= 1f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) y += 1f;

        // Axis-lock 
        Vector2 dir = Vector2.zero;
        if (Mathf.Abs(x) > 0.01f || Mathf.Abs(y) > 0.01f)
        {
            if (Mathf.Abs(x) > Mathf.Abs(y))
                dir = new Vector2(Mathf.Sign(x), 0f);
            else
                dir = new Vector2(0f, Mathf.Sign(y));
        }
        moveDir = dir;

        // Remember last facing only while actively moving
        if (moveDir.sqrMagnitude > 0f)
            lastDir = moveDir;

        // Animator: when idle, keep facing lastDir
        Vector2 animDir = (moveDir.sqrMagnitude > 0f) ? moveDir : lastDir;
        FacingDir = animDir;

        animator.SetBool("IsMoving", moveDir.sqrMagnitude > 0f);
        animator.SetFloat("MoveX", animDir.x);
        animator.SetFloat("MoveY", animDir.y);

        // Horizontal flip 
        if (Mathf.Abs(animDir.x) > 0.01f)
            sr.flipX = animDir.x > 0f; 
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = moveDir * moveSpeed;
    }
}