using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class ToggleSpriteOnUse : MonoBehaviour
{
    [Header("Sprites")]
    [Tooltip("If left empty, the current SpriteRenderer sprite is used as A.")]
    public Sprite spriteA;
    public Sprite spriteB;

    [Header("Player Filter")]
    [Tooltip("Only objects with this tag can toggle.")]
    public string playerTag = "Player";

    private SpriteRenderer sr;
    private bool playerInside;
    private KeyCode useKey = KeyCode.E;

    private void Awake()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;

        sr = GetComponent<SpriteRenderer>();
        if (!spriteA)
            spriteA = sr.sprite;
    }

    private void Update()
    {
        if (playerInside && Input.GetKeyDown(useKey))
        {
            sr.sprite = (sr.sprite == spriteA) ? spriteB : spriteA;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
            playerInside = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
            playerInside = false;
    }
}