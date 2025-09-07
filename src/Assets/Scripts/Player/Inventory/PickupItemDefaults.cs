using UnityEngine;

public sealed class PickupItemDefaults : MonoBehaviour
{
    [Header("Default visuals for all pickups")]
    public ItemVisualDB visuals;

    [Header("Default pickup prefab")]
    public GameObject pickupPrefab;

    public static PickupItemDefaults Instance { get; private set; }

    private void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return; 
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}