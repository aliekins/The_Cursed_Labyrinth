using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/**
 * @file HeartsUIController.cs
 * @brief Orchestrates heart bar binding and hit feedback (pulse+tint).
 * @ingroup PlayerHP
 */

[RequireComponent(typeof(HeartsBar))]
public class HeartsUIController : MonoBehaviour
{
    #region config
    [Header("Player binding")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private bool autoFindPlayer = true;

    [Header("Pulse settings")]
    [SerializeField, Min(1)] private int pulses = 2;
    [SerializeField] private float pulseUp = 0.06f;
    [SerializeField] private float pulseDown = 0.12f;
    [SerializeField, Range(0f, 1f)] private float colorIntensity = 0.6f; // how strong the tint is
    [SerializeField] private Color hitColor = new Color(1f, 0.2f, 0.2f, 1f);
    [SerializeField] private float scalePunch = 1.06f; // 1 = no scale, 1.06 = 6% pop

    private HeartsBar heartsBar;
    private Coroutine pulseCo;
    private int lastHP;
    private Image[] heartImgs;
    private Color[] baseColors;
    #endregion

    #region cycle
    void Awake()
    {
        heartsBar = GetComponent<HeartsBar>();
    }

    void OnEnable()
    {
        // Cache heart images and base colors
        heartImgs = heartsBar.Hearts;
        if (heartImgs != null && (baseColors == null || baseColors.Length != heartImgs.Length))
        {
            baseColors = new Color[heartImgs.Length];
            for (int i = 0; i < heartImgs.Length; i++)
                baseColors[i] = heartImgs[i] ? heartImgs[i].color : Color.white;
        }

        TryBindExistingPlayer();

        if (autoFindPlayer && !playerHealth)
            StartCoroutine(WaitAndBindPlayer());
    }

    void OnDisable()
    {
        Unsubscribe();
        if (pulseCo != null) { StopCoroutine(pulseCo); pulseCo = null; }
        ResetVisuals();
    }
    #endregion

    #region binding
    void TryBindExistingPlayer()
    {
        if (!playerHealth) playerHealth = FindFirstObjectByType<PlayerHealth>();

        if (playerHealth)
        {
            Subscribe();
            heartsBar.SetHealth(playerHealth.Current, playerHealth.Max);
        }
    }

    IEnumerator WaitAndBindPlayer()
    {
        while (!playerHealth)
        {
            playerHealth = FindFirstObjectByType<PlayerHealth>();

            if (playerHealth) break;
            yield return null;
        }
        Subscribe();
        heartsBar.SetHealth(playerHealth.Current, playerHealth.Max);
    }
    #endregion

    #region (un)subscription
    void Subscribe()
    {
        Unsubscribe();
        if (!playerHealth) return;
        lastHP = playerHealth.Current;
        playerHealth.Changed += OnHealthChanged;
    }

    void Unsubscribe()
    {
        if (playerHealth) playerHealth.Changed -= OnHealthChanged;
    }
    #endregion

    #region helpers
    void OnHealthChanged(int current, int max)
    {
        // Always update fills
        heartsBar.SetHealth(current, max);

        // Pulse all hearts only when taking damage
        if (current < lastHP && heartImgs != null && heartImgs.Length > 0)
        {
            if (pulseCo != null) StopCoroutine(pulseCo);
            pulseCo = StartCoroutine(PulseHearts());
        }

        lastHP = current;
    }

    IEnumerator PulseHearts()
    {
        // up (tint + pop)
        for (int p = 0; p < pulses; p++)
        {
            float t = 0f;
            while (t < pulseUp)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / pulseUp);
                float scale = Mathf.Lerp(1f, scalePunch, k);
                for (int i = 0; i < heartImgs.Length; i++)
                {
                    var img = heartImgs[i];
                    if (!img) continue;
                    img.rectTransform.localScale = new Vector3(scale, scale, 1f);
                    var baseCol = baseColors[i];
                    img.color = Color.Lerp(baseCol, Color.Lerp(baseCol, hitColor, colorIntensity), k);
                }
                yield return null;
            }

            // down (return to normal)
            float td = 0f;
            while (td < pulseDown)
            {
                td += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(td / pulseDown);
                float scale = Mathf.Lerp(scalePunch, 1f, k);
                for (int i = 0; i < heartImgs.Length; i++)
                {
                    var img = heartImgs[i];
                    if (!img) continue;
                    img.rectTransform.localScale = new Vector3(scale, scale, 1f);
                    var baseCol = baseColors[i];
                    // ease color back to base
                    img.color = Color.Lerp(Color.Lerp(baseCol, hitColor, colorIntensity), baseCol, k);
                }
                yield return null;
            }
        }

        // ensure exact reset
        ResetVisuals();
        pulseCo = null;
    }

    void ResetVisuals()
    {
        if (heartImgs == null) return;
        for (int i = 0; i < heartImgs.Length; i++)
        {
            var img = heartImgs[i];
            if (!img) continue;
            img.rectTransform.localScale = Vector3.one;
            if (baseColors != null && i < baseColors.Length)
                img.color = baseColors[i];
        }
    }
    #endregion
}