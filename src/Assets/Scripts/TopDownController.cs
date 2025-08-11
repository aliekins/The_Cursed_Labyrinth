using UnityEngine;

/// <summary>
/// Top-down 2D character controller
/// using the legacy <see cref="Input"/> API (WASD/Arrow keys) and 2D physics.
/// </summary>
/// <remarks>
/// <para>
/// This controller:
/// </para>
/// <list type="bullet">
///   <item><description>Reads keyboard input directly (no Unity Input System asset required).</description></item>
///   <item><description>Locks movement to the dominant axis to prevent diagonal motion.</description></item>
///   <item><description>Moves via <see cref="Rigidbody2D.linearVelocity"/>.</description></item>
///   <item><description>Drives an <see cref="Animator"/> with parameters <c>IsMoving</c>, <c>MoveX</c>, <c>MoveY</c>.</description></item>
///   <item><description>Flips the <see cref="SpriteRenderer"/> on X so one set of left-facing clips can serve left and right.</description></item>
/// </list>
/// <para>
/// Expected Animator setup:
/// </para>
/// <list type="bullet">
///   <item><description>Two 2D (freeform cartesian) Blend trees: <c>IdleBT</c> and <c>WalkBT</c>.</description></item>
///   <item><description>Each tree has motions positioned at: Left(-1,0), Right(1,0), Back(0,1), Front(0,-1).</description></item>
///   <item><description>Transitions: Idle to Walk driven by <c>IsMoving</c>, no Exit Time, duration ~0–0.05.</description></item>
/// </list>
/// <para>
/// Layers/physics:
/// </para>
/// <list type="bullet">
///   <item><description>Attach <see cref="Rigidbody2D"/> (Dynamic), set Gravity Scale = 0, Freeze Z Rotation, Interpolate = Interpolate.</description></item>
///   <item><description>Ensure walls use 2D colliders (e.g., TilemapCollider2D) and the Layer Collision Matrix allows Player vs Walls.</description></item>
/// </list>
/// </remarks>
/// <example>
/// Minimal usage:
/// // On the Player GameObject:
/// // - Add Rigidbody2D, SpriteRenderer, Animator.
/// // - Assign an Animator Controller with parameters IsMoving(bool), MoveX(float), MoveY(float).
/// // - Add this script.
/// </example>

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
public class TopDownController : MonoBehaviour
{
    /// <summary>
    /// Movement speed in Unity units per second
    /// </summary>
    [SerializeField]
    [Tooltip("Movement speed in units/second.")]
    private float moveSpeed = 4f;

    // Cached components (assigned in Awake)
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Animator animator;

    /// <summary>
    /// Last non-zero facing direction. Used to keep the correct idle facing when input stops
    /// Defaults to <c>Vector2.down</c> (front)
    /// </summary>
    private Vector2 lastDir = Vector2.down;

    /// <summary>
    /// Axis-locked movement direction computed each frame (Update) and applied in FixedUpdate
    /// </summary>
    private Vector2 moveDir = Vector2.zero;

    /// <summary>
    /// Cache component references
    /// </summary>
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
    }

    /// <summary>
    /// Read input, compute axis-locked direction, drive Animator parameters and sprite flipping
    /// </summary>
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

        animator.SetBool("IsMoving", moveDir.sqrMagnitude > 0f);
        animator.SetFloat("MoveX", animDir.x);
        animator.SetFloat("MoveY", animDir.y);

        // Horizontal flip 
        if (Mathf.Abs(animDir.x) > 0.01f)
            sr.flipX = animDir.x > 0f; 
    }

    /// <summary>
    /// Apply physics movement using <see cref="Rigidbody2D.linearVelocity"/>.
    /// </summary>
    private void FixedUpdate()
    {
        rb.linearVelocity = moveDir * moveSpeed;
    }
}