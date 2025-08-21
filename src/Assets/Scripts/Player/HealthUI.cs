/// \file HealthUI.cs 
using UnityEngine;
using UnityEngine.UI;

public sealed class HealthUI : MonoBehaviour
{
    [SerializeField] private Image fill;   // UI Image (filled) for health
    [SerializeField] private TMPro.TextMeshProUGUI label; 

    private PlayerHealth bound;

    public void Bind(PlayerHealth hp)
    {
        if (bound != null)
            bound.Changed -= OnChanged;

        bound = hp;
        if (bound != null)
        {
            bound.Changed += OnChanged;
            OnChanged(bound.Current, bound.Max);
        }
    }

    private void OnDestroy()
    {
        if (bound != null)
            bound.Changed -= OnChanged;
    }

    private void OnChanged(int current, int max)
    {
        float pct = max <= 0 ? 0f : (float)current / max;

        if (fill) 
            fill.fillAmount = pct;
        if (label) 
            label.text = Mathf.RoundToInt(pct * 100f) + "%";
    }
}