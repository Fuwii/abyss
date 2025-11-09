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
        Debug.Log("hand changed");
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
        var bp = playerInventory.backpackItem;

        if (bp != null)
        {
            if (backpackIcon != null)
            {
                backpackIcon.sprite = bp.icon;
                backpackIcon.enabled = true;
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
