/// \file SpikeTrap.cs
/// \brief Reactive spike trap: animates on proximity and damages with a short warmup delay
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Animator))]
public sealed class SpikeTrap : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField, Min(1)] private int damage = 15;               
    [SerializeField, Min(0f)] private float damageTickInterval = 0.4f; 
    [SerializeField, Min(0f)] private float warmupDelay = 0.2f;

    [Header("Animator")]
    [SerializeField] private string isPlayerNearParam = "isPlayerNear";

    private Animator animator;
    private int paramHash;

    private float nextTickTime;
    private float enteredTime;
    private bool playerInside;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        paramHash = Animator.StringToHash(isPlayerNearParam);

        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent<PlayerHealth>(out _)) return;

        playerInside = true;
        enteredTime = Time.time;
        nextTickTime = Time.time + warmupDelay; 
        animator.SetBool(paramHash, true);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!playerInside) return;
        if (!other.TryGetComponent<PlayerHealth>(out var hp)) return;

        animator.SetBool(paramHash, true);

        // after warmup, apply periodic damage
        if (Time.time >= nextTickTime)
        {
            hp.Damage(damage);
            nextTickTime += damageTickInterval;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.TryGetComponent<PlayerHealth>(out _)) return;

        playerInside = false;
        animator.SetBool(paramHash, false);
    }
}