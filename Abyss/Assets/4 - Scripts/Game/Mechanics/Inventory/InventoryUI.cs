using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    public PlayerInventory playerInventory;

    [Header("Quick Slots (1-4)")]
    public Image[] slotIcons; 

    [Header("Item In Hand UI")]
    public Image handIcon;

    [Header("Backpack Slot UI")]
    public Image backpackIcon;
    public Button backpackSlotButton;

    void Start()
    {
        if (playerInventory == null)
        {
            Debug.LogError("PlayerInventory not assigned to UI");
            return;
        }

        playerInventory.OnInventoryChanged += RefreshSlots;
        playerInventory.OnHandChanged += RefreshHand;
        playerInventory.OnBackpackChanged += RefreshBackpack;

        RefreshSlots();
        RefreshHand();
        RefreshBackpack();
    }

    void RefreshSlots()
    {
        for (int i = 0; i < slotIcons.Length; i++)
        {
            var inst = playerInventory.GetMainSlot(i);

            if (inst != null && inst.itemData != null)
            {
                slotIcons[i].sprite = inst.itemData.icon;
                slotIcons[i].enabled = true;
            }
            else
            {
                slotIcons[i].sprite = null;
                slotIcons[i].enabled = false;
            }
        }
    }

    void RefreshHand()
    {
        var h = playerInventory.handItem;

        if (h != null && h.itemData != null)
        {
            handIcon.sprite = h.itemData.icon;
            handIcon.enabled = true;
        }
        else
        {
            handIcon.sprite = null;
            handIcon.enabled = false;
        }
    }


    void RefreshBackpack()
    {
        var bp = playerInventory.backpackItem;

        if (bp != null)
        {
            backpackIcon.sprite = bp.icon;
            backpackIcon.enabled = true;
            backpackSlotButton.interactable = true;
        }
        else
        {
            backpackIcon.sprite = null;
            backpackIcon.enabled = false;
            backpackSlotButton.interactable = false;
        }
    }
}
