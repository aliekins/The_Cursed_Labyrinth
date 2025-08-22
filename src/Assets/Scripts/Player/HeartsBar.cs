using UnityEngine;
using UnityEngine.UI;

public class HeartsBar : MonoBehaviour
{
    [SerializeField] private Image[] hearts; 
    [SerializeField] private int healthPerHeart = 10;
    public Image[] Hearts => hearts;
    public void SetHealth(int current, int max)
    {
        if (hearts == null || hearts.Length == 0) return;

        int hph = Mathf.Max(1, healthPerHeart);
        int maxHearts = Mathf.CeilToInt(max / (float)hph);

        for (int i = 0; i < hearts.Length; i++)
        {
            var img = hearts[i];
            if (!img) continue;

            img.enabled = true;
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillOrigin = (int)Image.OriginHorizontal.Left;

            float fill;
            if (i >= maxHearts)
            {
                fill = 0f;
            }
            else
            {
                int start = i * hph;
                int remaining = Mathf.Clamp(current - start, 0, hph);
                fill = (float)remaining / hph;
            }

            img.fillAmount = fill;
        }
    }
}