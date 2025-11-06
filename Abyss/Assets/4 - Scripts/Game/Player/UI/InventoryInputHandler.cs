using UnityEngine;
using Game.Player.Input;

[RequireComponent(typeof(PlayerInventory))]
public class InventoryInputHandler : MonoBehaviour
{
    private PlayerInventory inv;

    private void Awake()
    {
        inv = GetComponent<PlayerInventory>();
    }

    private void OnEnable()
    {
        InputManager.OnSlotPressed += HandleSlotPressed;
        InputManager.OnDropPressed += HandleDropPressed;
        InputManager.OnLeftClickStarted += HandleLeftClick;
    }

    private void OnDisable()
    {
        InputManager.OnSlotPressed -= HandleSlotPressed;
        InputManager.OnDropPressed -= HandleDropPressed;
        InputManager.OnLeftClickStarted -= HandleLeftClick;
    }

    private void HandleSlotPressed(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex <= 3)
        {
            inv.EquipMainToHand(slotIndex); 
        }
        else if (slotIndex == 4)
        {
            if (inv.backpackWorn)
            {
            }
            else
            {
                
            }
        }
    }

    private void HandleLeftClick()
    {
        inv.UseHand(gameObject);
    }

    private void HandleDropPressed()
    {
        inv.DropFromMain(); 
    }
}
