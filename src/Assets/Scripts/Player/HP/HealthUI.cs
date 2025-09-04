using UnityEngine;
/**
 * @file HealthUI.cs
 * @brief Binds a PlayerHealth to a HeartsBar and keeps it updated.
 * @ingroup PlayerHP
 */
public sealed class HealthUI : MonoBehaviour
{
    [SerializeField] private PlayerHealth health; 
    [SerializeField] private HeartsBar hearts;   

    void Awake()
    {
        if (!hearts) hearts = GetComponent<HeartsBar>();
    }

    void OnEnable()
    {
        if (health == null || hearts == null) return;

        health.Changed += OnChanged;
        OnChanged(health.Current, health.Max);
    }

    void OnDisable()
    {
        if (health != null)
            health.Changed -= OnChanged;
    }

    void OnChanged(int current, int max) => hearts.SetHealth(current, max);
}