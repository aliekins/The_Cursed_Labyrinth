using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public sealed class PickupTextUI : MonoBehaviour
{
    #region Config
    public static PickupTextUI Instance;

    [Header("Wiring")]
    [SerializeField] private RectTransform container;
    [SerializeField] private TMP_Text entryPrefab;

    [Header("Behavior")]
    [SerializeField] private int maxOnScreen = 4;
    [SerializeField] private float fadeIn = 0.10f;
    [SerializeField] private float hold = 0.80f;
    [SerializeField] private float fadeOut = 0.25f;
    [SerializeField] private float risePixels = 30f;
    #endregion

    #region core
    void Awake()
    {
        if (Instance && Instance != this) 
        { 
            Destroy(gameObject); 
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (!container) 
            container = GetComponent<RectTransform>();
    }

    public static void Show(Item.ItemType type, int qty = 1)
    {
        if (!Instance) return;

        string label = $"+{qty} {DisplayName(type)}";
        Instance.Spawn(label);
    }

    public static void ShowHP(int delta)
    {
        if (!Instance) return;

        int amount = Mathf.Abs(delta);
        string sign = delta >= 0 ? "+" : "-";
        string msg = $"{sign}{amount} HP";

        Instance.Spawn(msg);
    }
    #endregion

    #region helpers
    private static string DisplayName(Item.ItemType t)
    {
        switch (t)
        {
            case Item.ItemType.Sword: return "sword";
            case Item.ItemType.HealthPotion: return "potion";
            case Item.ItemType.Book1:
            case Item.ItemType.Book2:
            case Item.ItemType.Book3:
            case Item.ItemType.Book4:
            case Item.ItemType.Book5: return "book";
            case Item.ItemType.SkullDiamond:
            case Item.ItemType.HeartDiamond: 
            case Item.ItemType.Crown: return "cursed item";
            default: return t.ToString();
        }
    }

    private void Spawn(string text)
    {
        if (!entryPrefab || !container) return;

        // remove oldest
        while (container.childCount >= maxOnScreen)
            Destroy(container.GetChild(0).gameObject);

        var entry = Instantiate(entryPrefab, container);
        entry.text = text;

        var cg = entry.GetComponent<CanvasGroup>();
        if (!cg)
            cg = entry.gameObject.AddComponent<CanvasGroup>();

        cg.alpha = 0f;

        // reset position to bottom 
        var rt = (RectTransform)entry.transform;
        rt.anchoredPosition = Vector2.zero;

        StartCoroutine(Animate(entry, cg, rt));
    }

    private IEnumerator Animate(TMP_Text entry, CanvasGroup cg, RectTransform rt)
    {
        float t = 0f;

        // fade in
        while (t < fadeIn)
        {
            t += Time.unscaledDeltaTime;

            cg.alpha = Mathf.Clamp01(t / fadeIn);
            rt.anchoredPosition = Vector2.up * Mathf.Lerp(0f, risePixels * 0.4f, cg.alpha);

            yield return null;
        }

        // hold
        yield return new WaitForSecondsRealtime(hold);

        // fade out + rise
        t = 0f;
        while (t < fadeOut)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / fadeOut);

            cg.alpha = 1f - k;
            rt.anchoredPosition = Vector2.up * Mathf.Lerp(risePixels * 0.4f, risePixels, k);

            yield return null;
        }
        Destroy(entry.gameObject);
    }
    #endregion
}