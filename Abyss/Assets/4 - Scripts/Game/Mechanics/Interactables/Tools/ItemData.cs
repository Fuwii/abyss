using UnityEngine;

namespace Game.Mechanics.Interactables.Tools
{
    public enum Rarity { Common, Uncommon, Rare, Epic, Legendary }
    public enum ItemCategory { Generic, Consumable, Weapon}

    [CreateAssetMenu(menuName = "Items/BaseItem", fileName = "New Item")]
    public class ItemData : ScriptableObject
    {
        [Header("Base Info")]
        public string itemName = "Item";
        public Sprite icon;
        public int weight = 1;
        public int price = 1;
        public Rarity rarity = Rarity.Common;

        [Header("Durability / Uses")]
        public int uses = 0;

        [Header("For animns and drop")]
        public GameObject itemPrefab;

        public ItemCategory category = ItemCategory.Generic;

        public virtual void OnPickup(GameObject player, ItemInstance instance) { }
        public virtual void OnSelected(GameObject player, ItemInstance instance) { }
        public virtual void OnUse(GameObject player, ItemInstance instance) { }
        public virtual void OnDropped(GameObject player, ItemInstance instance) { }
    }
}
