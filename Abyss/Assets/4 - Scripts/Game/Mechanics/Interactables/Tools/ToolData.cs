using UnityEngine;

namespace Game.Mechanics.Interactables.Tools
{
    [CreateAssetMenu(menuName = "Items/Tool", fileName = "New Tool")]
    public class ToolData : ScriptableObject
    {
        public string itemName = "Tool";
        public Sprite icon;
        public int weight = 1;
        public int price = 1;
    }
}