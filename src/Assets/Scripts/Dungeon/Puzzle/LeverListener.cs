using System;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public sealed class LeverListener : MonoBehaviour
{
    public enum BookColor { Blue, Green, Red, Orange, Purple }

    [Header("Setup")]
    public BookColor color;
    public ToggleSpriteOnUse toggle; // auto-filled if left empty

    private SpriteRenderer sr;
    private bool isDownPrev;

    public event Action<LeverListener> PulledDown; // fired once when A->B transition happens

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (!toggle) toggle = GetComponent<ToggleSpriteOnUse>();
        if (!toggle)
            Debug.LogError($"[LeverListener] ToggleSpriteOnUse missing on {name}");
    }

    private void Update()
    {
        if (!toggle || !sr) return;

        bool isDownNow = (sr.sprite == toggle.spriteB);
        if (isDownNow && !isDownPrev)
        {
            PulledDown?.Invoke(this);
        }
        isDownPrev = isDownNow;
    }

    public void ResetUp()
    {
        if (toggle && sr)
        {
            sr.sprite = toggle.spriteA;
            isDownPrev = false;
        }
    }

    public void LockInteraction(bool locked)
    {
        if (toggle) toggle.enabled = !locked;

        var col = GetComponent<Collider2D>();
        if (col) col.enabled = !locked;
    }
}
