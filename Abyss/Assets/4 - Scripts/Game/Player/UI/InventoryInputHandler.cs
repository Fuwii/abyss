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
        inv.SetSelectedSlot(slotIndex);
    }

    private void HandleLeftClick()
    {
        inv.UseSelected(gameObject);
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
        float clampedForce = Mathf.Clamp(dropHoldTime, 0.1f, maxDropForce);
        inv.DropSelected(clampedForce);
    }
}
