/// @file SpikeTrap.cs
/// @brief Reactive spike trap: animates on proximity and applies periodic damage.
/// @ingroup Objects
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Animator))]
public sealed class SpikeTrap : MonoBehaviour
{
    #region Inspector
    [Header("Damage")]
    [SerializeField, Min(1)] private int damage = 15;               
    [SerializeField, Min(0f)] private float damageTickInterval = 0.4f; 
    [SerializeField, Min(0f)] private float warmupDelay = 0.4f;

    [Header("Animator")]
    [SerializeField] private string isPlayerNearParam = "isPlayerNear";

    [Header("Audio")]
    [SerializeField] private AudioClip damageSFX;
    [SerializeField] private bool sfxOnEnter = false;
    #endregion

    #region params
    private Animator animator;
    private int paramHash;

    private float nextTickTime;
    private bool playerInside;
    #endregion

    #region cycle
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
        nextTickTime = Time.time + warmupDelay;
        animator.SetBool(paramHash, true);

        if (sfxOnEnter && damageSFX && !BiomeTransitionOverlay.IsActive)
            SfxController.Play(damageSFX);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!playerInside) return;
        if (!other.TryGetComponent<PlayerHealth>(out var hp)) return;

        animator.SetBool(paramHash, true);

        if (BiomeTransitionOverlay.IsActive)
        {
            nextTickTime = Time.time + 0.15f;
            return;
        }

        if (Time.time >= nextTickTime)
        {
            hp.Damage(damage);
            if (damageSFX)
                SfxController.Play(damageSFX);

            nextTickTime += damageTickInterval;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.TryGetComponent<PlayerHealth>(out _)) return;

        playerInside = false;
        animator.SetBool(paramHash, false);
    }
    #endregion
}