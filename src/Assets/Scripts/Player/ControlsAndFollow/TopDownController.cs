using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
public class TopDownController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField, Tooltip("Units per second")]
    private float moveSpeed = 4f;

    [SerializeField, Tooltip("How much stronger the other axis must be to steal priority")]
    private float axisHysteresis = 0.15f;

    public Vector2 FacingDir { get; private set; } = Vector2.down;

    // Cached components
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Animator animator;

    // State
    private Vector2 lastDir = Vector2.down;
    private Vector2 moveDir = Vector2.zero;
    private bool lastWasVertical = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        // Read keyboard (WASD + Arrows)
        float x = 0f, y = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) x -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) x += 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) y -= 1f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) y += 1f;

        var ax = Mathf.Abs(x);
        var ay = Mathf.Abs(y);

        bool chooseVertical;
        if (ax > ay + axisHysteresis)
            chooseVertical = false;   // horizontal wins
        else if (ay > ax + axisHysteresis)
            chooseVertical = true;    // vertical wins
        else
            chooseVertical = lastWasVertical;

        Vector2 dir = Vector2.zero;
        if (chooseVertical && ay > 0.01f)
            dir = new Vector2(0f, Mathf.Sign(y));
        else if (!chooseVertical && ax > 0.01f)
            dir = new Vector2(Mathf.Sign(x), 0f);

        moveDir = dir;

        // Remember last facing only while actively moving
        if (moveDir.sqrMagnitude > 0f)
        {
            lastDir = moveDir;
            lastWasVertical = (moveDir.y != 0f);
        }

        // Animator: when idle, keep facing lastDir
        Vector2 animDir = (moveDir.sqrMagnitude > 0f) ? moveDir : lastDir;
        FacingDir = animDir;

        animator.SetBool("IsMoving", moveDir.sqrMagnitude > 0f);
        animator.SetFloat("MoveX", animDir.x);
        animator.SetFloat("MoveY", animDir.y);

        // Horizontal flip to face right/left
        if (Mathf.Abs(animDir.x) > 0.01f)
            sr.flipX = animDir.x > 0f;
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = moveDir * moveSpeed;
    }
}