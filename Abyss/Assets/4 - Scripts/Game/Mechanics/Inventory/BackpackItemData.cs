using Game.Mechanics.Interactables.Tools;
using UnityEngine;

[CreateAssetMenu(menuName = "Items/BackpackItem", fileName = "New Backpack")]
public class BackpackItemData : ItemData
{
    [Header("Backpack")]
    public int capacity = 5; 
}