using Game.Mechanics.Interactables.Tools;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    public PlayerInventory playerInventory;

    [Header("Quick Slots (1-4)")]
    public ItemSlotUI[] slotUIs;       

    [Header("Item In Hand UI")]
    public ItemSlotUI handSlot;       

    [Header("Backpack Slot UI")]
    public Image backpackIcon;
    public Button backpackSlotButton;

    private void Start()
    {
        if (playerInventory == null)
        {
            Debug.LogError("InventoryUI: PlayerInventory not assigned and none found in scene.");
            enabled = false;
            return;
            
        }
        if (slotUIs != null)
        {
            for (int i = 0; i < slotUIs.Length; i++)
            {
                var slot = slotUIs[i];
                if (slot != null)
                    slot.Setup(playerInventory, i, false);
            }
        }
        if (handSlot != null)
        {
        }

        playerInventory.OnInventoryChanged += RefreshSlots;
        playerInventory.OnBackpackChanged += RefreshBackpack;
        playerInventory.OnHandChanged += RefreshHand;

        RefreshSlots();
        RefreshHand();
        RefreshBackpack();
    }

    private void OnDestroy()
    {
        if (playerInventory != null)
        {
            playerInventory.OnInventoryChanged -= RefreshSlots;
            playerInventory.OnBackpackChanged -= RefreshBackpack;
            playerInventory.OnHandChanged -= RefreshHand;
        }
    }
    public void RefreshSlots()
    {
        if (slotUIs == null) return;
        for (int i = 0; i < slotUIs.Length; i++)
        {
            var slot = slotUIs[i];
            if (slot == null) continue;

            if (slot == handSlot) continue;

            slot.Refresh();
        }
    }

    public void RefreshHand()
    {
        if (handSlot == null) return;
        var h = playerInventory.handItem;
        if (h != null && h.itemData != null && h.itemData.icon != null)
        {
            if (handSlot.icon != null)
            {
                handSlot.icon.sprite = h.itemData.icon;
                handSlot.icon.enabled = true;
            }
        }
        else
        {
            if (handSlot.icon != null)
            {
                handSlot.icon.sprite = null;
                handSlot.icon.enabled = false;
            }
        }
    }

    public void RefreshBackpack()
    {
        ItemInstance bp = null;
        bp = playerInventory.GetBackpackSlotItem();

        if (bp != null && bp.itemData != null)
        {
            if (backpackIcon != null)
            {
                backpackIcon.sprite = bp.itemData.icon;
                backpackIcon.enabled = bp.itemData.icon != null;
            }

            if (backpackSlotButton != null)
                backpackSlotButton.interactable = true;
        }
        else
        {
            if (backpackIcon != null)
            {
                backpackIcon.sprite = null;
                backpackIcon.enabled = false;
            }

            if (backpackSlotButton != null)
                backpackSlotButton.interactable = false;
        }
    }
}
