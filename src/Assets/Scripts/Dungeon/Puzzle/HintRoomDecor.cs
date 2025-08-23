using UnityEngine;

public class HintRoomDecor : MonoBehaviour, IPuzzleRoom
{
    public TokenType token;
    public PuzzleTokenPickup pickup; // assign in prefab
    public SpriteRenderer banner;    // optional themed sprite holder
    public Sprite featherSprite, skullSprite, gemSprite, leafSprite;

    public void Init(Room room, DungeonMapIndex.RoomIndex ri)
    {
        if (pickup) pickup.type = token;
        if (!banner) return;
        banner.sprite = token switch
        {
            TokenType.Feather => featherSprite,
            TokenType.Skull => skullSprite,
            TokenType.Gem => gemSprite,
            _ => leafSprite
        };
    }
}