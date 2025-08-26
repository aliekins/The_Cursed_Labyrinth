using UnityEngine;

public class Item 
{
    public enum ItemType
    {
        Sword,
        HealthPotion,
        Book1, Book2, Book3, Book4, Book5
    }

    public ItemType itemType;
    public int quantity;
}
