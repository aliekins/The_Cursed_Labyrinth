using UnityEngine;

/**
 * @file HealthUI.cs
 * @brief Binds a PlayerHealth to a HeartsBar and keeps it updated.
 *        Shows small +HP/-HP text hints.
 * @ingroup PlayerHP
 */
public sealed class HealthUI : MonoBehaviour
{
    #region config
    [SerializeField] private PlayerHealth health;
    [SerializeField] private HeartsBar hearts;

    [Header("Texts")]
    [SerializeField] private bool showHpChangeToasts = true;

    [Header("SFX")]
    [SerializeField] private AudioClip damageSFX;
    [SerializeField] private AudioClip healSFX;
    [SerializeField, Range(0f, 1f)] private float volume = 1f;

    private int lastCurrent;
    private bool hasLast;
    #endregion

    #region core
    void Awake()
    {
        if (!hearts) hearts = GetComponent<HeartsBar>();
    }

    public void Bind(PlayerHealth hp)
    {
        if (health) health.Changed -= OnChanged;

        health = hp;

        if (!hearts) 
            hearts = GetComponent<HeartsBar>(); 

        if (!health || !hearts) return;

        lastCurrent = health.Current;
        hasLast = true;
        hearts.SetHealth(health.Current, health.Max);

        health.Changed += OnChanged;
    }

    public void Unbind()
    {
        if (health)
            health.Changed -= OnChanged;

        hasLast = false;
    }

    void OnChanged(int current, int max)
    {
        if (showHpChangeToasts && hasLast && !BiomeTransitionOverlay.IsActive)
        {
            int delta = current - lastCurrent;
            if (delta != 0)
            {
                PickupTextUI.ShowHP(delta);
            }
        }

        if (hasLast)
        {
            int delta = current - lastCurrent;
            if (delta < 0)
            {
                if (BiomeTransitionOverlay.IsActive) return;
                if (!damageSFX) return;
                SfxController.Play(damageSFX, volume);
            }
            else if (delta > 0)
            {
                if (BiomeTransitionOverlay.IsActive) return;
                if (!healSFX) return;
                SfxController.Play(healSFX, volume);
            }
        }

        lastCurrent = current;
        hearts.SetHealth(current, max);
    }
    #endregion
}