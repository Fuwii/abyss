using Game.Mechanics.Interactables.Tools;
using UnityEngine;

public interface IItemHandler
{
    void OnPickup(GameObject player, ItemInstance instance);
    void OnSelected(GameObject player, ItemInstance instance,Transform handTransform);
    void OnUse(GameObject player, ItemInstance instance);
    void OnDropped(GameObject player, ItemInstance instance, float force=0.1f);
    void OnDeselected(GameObject player, ItemInstance instance);
}

