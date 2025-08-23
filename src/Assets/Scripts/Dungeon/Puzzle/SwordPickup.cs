using UnityEngine;

public class SwordPickup : MonoBehaviour
{
    public void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        QuestState.Instance.AddSword(); // singleton
        Destroy(gameObject);
    }
}

public class GateDoor : MonoBehaviour
{
    [SerializeField] Collider2D doorCollider;
    public void SetOpen(bool open) { if (doorCollider) doorCollider.enabled = !open; }
}

public class SwordPuzzleRoom : MonoBehaviour, IPuzzleRoom
{
    [SerializeField] private GateDoor gateDoor;
    [SerializeField] private int required = 4;

    public void Init(Room room, DungeonMapIndex.RoomIndex ri)
    {
        // Optionally reposition sword pickups with ri.Interior
        QuestState.Instance.SwordsChanged += OnSwordsChanged;
        OnSwordsChanged(QuestState.Instance.Swords);
    }

    void OnDestroy() { if (QuestState.Has) QuestState.Instance.SwordsChanged -= OnSwordsChanged; }

    void OnSwordsChanged(int count) => gateDoor?.SetOpen(count >= required);
}