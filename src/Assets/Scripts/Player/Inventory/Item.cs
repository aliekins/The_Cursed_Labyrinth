using UnityEngine;

public class Item 
{
    public enum ItemType
    {
        HealthPotion,

        Sword,

        Book1, Book2, Book3, Book4, Book5,

        SkullDiamond = 100,
        HeartDiamond = 101,
        Crown = 102,
    }

    public ItemType itemType;
    public int quantity;
}
