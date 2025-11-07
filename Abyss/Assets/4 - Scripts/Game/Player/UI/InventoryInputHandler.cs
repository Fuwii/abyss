using UnityEngine;
using Game.Player.Input;

[RequireComponent(typeof(PlayerInventory))]
public class InventoryInputHandler : MonoBehaviour
{
    private PlayerInventory inv;
    private float dropStartTime;
    private bool dropHeld;
    private float maxDropForce = 2f;
    private void Awake()
    {
        inv = GetComponent<PlayerInventory>();
    }

    private void OnEnable()
    {
        InputManager.OnSlotPressed += HandleSlotPressed;
        InputManager.OnLeftClickStarted += HandleLeftClick;
        InputManager.OnDropStarted += OnDropStarted;
        InputManager.OnDropCanceled += OnDropCanceled;
    }

    private void OnDisable()
    {
        InputManager.OnSlotPressed -= HandleSlotPressed;
        InputManager.OnDropStarted -= OnDropStarted;
        InputManager.OnDropCanceled -= OnDropCanceled;
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
    private void OnDropStarted()
    {
        dropHeld = true;
        dropStartTime = Time.time;
    }

    private void OnDropCanceled()
    {
        if (!dropHeld) return;
        dropHeld = false;
        if (dropStartTime < 0f)
            return;

        float dropHoldTime = Time.time - dropStartTime;
        dropStartTime = -1f;
        float clampedForce = Mathf.Clamp(dropHoldTime, 0.1f,maxDropForce);
        inv.DropFromHand(clampedForce);
    }
}
