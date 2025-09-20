using System.Collections.Generic;
using UnityEngine;

namespace Game.Mechanics.Interactables.Consumables
{
    [CreateAssetMenu(menuName = "Items/Consumable", fileName = "New Consumable")]
    public class ConsumableData : ScriptableObject
    {
        public string itemID = "Consumable_1";
        public string itemName = "Consumable";
        public Sprite icon;
        public int weight = 1;
        public int price = 1;
        public float instantStamina;
        public List<ConsumableEffectEntry> effects = new();
    }
}