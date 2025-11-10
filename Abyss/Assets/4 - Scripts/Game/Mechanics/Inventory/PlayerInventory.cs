using Game.Mechanics.Interactables.Tools;
using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [Header("Hand mount (where runtime item prefab attaches)")]
    public Transform handMount;

    [Header("Inventory settings")]
    public int mainSlotsCount = 4;
    private int selectedSlot = -1;
    public event Action OnInventoryChanged;
    public event Action OnBackpackChanged;
    public event Action OnHandChanged;

    [SerializeField] public List<ItemInstance> mainSlots;
    [SerializeField] public ItemInstance handItem;
    [SerializeField] public ItemInstance backpackSlot;
    public bool backpackWorn => backpackSlot != null;
    public void NotifyInventoryChanged() => OnInventoryChanged?.Invoke();
    public void NotifyBackpackChanged() => OnBackpackChanged?.Invoke();
    public void NotifyHandChanged() => OnHandChanged?.Invoke();

    private void Awake()
    {
        mainSlots = new List<ItemInstance>(mainSlotsCount);
        for (int i = 0; i < mainSlotsCount; i++) mainSlots.Add(null);
    }

    private void ClearHandVisual()
    {
        if (handItem?.runtimeHeldObject != null)
        {
            Destroy(handItem.runtimeHeldObject);
            handItem.runtimeHeldObject = null;
        }
    }
    public bool TryPickup(ItemInstance instance)
    {
        if (instance == null || instance.itemData == null) return false;

        if (handItem != null && handItem.itemData != null)
        {
            ItemSystem.Instance.HandleDropped(gameObject, handItem, 0.5f);
            ClearHandVisual();
            handItem = null;
            OnHandChanged?.Invoke();
            OnInventoryChanged?.Invoke();
        }

        handItem = instance;
        ItemSystem.Instance.HandleSelected(gameObject, handItem, handMount);
        OnInventoryChanged?.Invoke();
        OnHandChanged?.Invoke();
        Debug.Log(backpackSlot); 
        return true;
    }
    public bool EquipMainToHand(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= mainSlots.Count) return false;
        var slotItem = mainSlots[slotIndex];

        if (slotItem == null && handItem == null) return false;

        if (handItem == null)
        {
            if (slotItem == null) return false;
            handItem = slotItem;
            mainSlots[slotIndex] = null;
            ItemSystem.Instance.HandleSelected(gameObject, handItem, handMount);
            OnInventoryChanged?.Invoke();
            OnHandChanged?.Invoke();
            return true;
        }

        if (handItem != null && (slotItem == null || slotItem.itemData == null))
        {
            ItemSystem.Instance.HandleDeselected(gameObject, handItem);
            mainSlots[slotIndex] = handItem;
            ClearHandVisual();
            handItem = null;
            OnInventoryChanged?.Invoke();
            OnHandChanged?.Invoke();
            return true;
        }

        if (handItem != null && slotItem != null)
        {
            ItemSystem.Instance.HandleDeselected(gameObject, handItem);
            var oldHand = handItem;
            handItem = slotItem;
            mainSlots[slotIndex] = oldHand;
            ItemSystem.Instance.HandleSelected(gameObject, handItem, handMount);
            OnInventoryChanged?.Invoke();
            OnHandChanged?.Invoke();
            return true;
        }

        return false;
    }
    public void DropFromHand(float force)
    {
        if (handItem == null || handItem.itemData == null) return;
        ItemSystem.Instance.HandleDropped(gameObject, handItem, force);
        ClearHandVisual();
        handItem = null;
        OnInventoryChanged?.Invoke();
        OnHandChanged?.Invoke();
    }
    public ItemInstance RemoveFromMain(int index)
    {
        if (index < 0 || index >= mainSlots.Count) return null;
        var it = mainSlots[index];
        mainSlots[index] = null;
        OnInventoryChanged?.Invoke();
        return it;
    }

    public bool TryPutIntoMain(int index, ItemInstance item)
    {
        if (index < 0 || index >= mainSlots.Count) return false;
        if (mainSlots[index] != null) return false;
        mainSlots[index] = item;
        OnInventoryChanged?.Invoke();
        return true;
    }
    public BackpackComponent GetBackpackComponent()
    {
        return backpackSlot?.GetComponent<BackpackComponent>();
    }

    public int GetBackpackSize()
    {
        var comp = GetBackpackComponent();
        return comp != null ? comp.capacity : 0;
    }

    public ItemInstance GetBackpackContentsAt(int index)
    {
        var comp = GetBackpackComponent();
        if (comp == null || index < 0 || index >= comp.contents.Count) return null;
        return comp.contents[index];
    }

    public bool TryPutIntoBackpack(int index, ItemInstance item)
    {
        var comp = GetBackpackComponent();
        if (comp == null || index < 0 || index >= comp.contents.Count) return false;
        if (comp.contents[index] != null) return false;
        comp.contents[index] = item;
        OnBackpackChanged?.Invoke();
        return true;
    }

    public ItemInstance DropFromBackpack(int index)
    {
        var comp = GetBackpackComponent();
        if (comp == null || index < 0 || index >= comp.contents.Count) return null;
        var it = comp.contents[index];
        comp.contents[index] = null;
        OnBackpackChanged?.Invoke();
        return it;
    }

    public bool SwapBackpackSlots(int a, int b)
    {
        var comp = GetBackpackComponent();
        if (comp == null) return false;
        if (a < 0 || a >= comp.contents.Count || b < 0 || b >= comp.contents.Count) return false;
        var tmp = comp.contents[a];
        comp.contents[a] = comp.contents[b];
        comp.contents[b] = tmp;
        OnBackpackChanged?.Invoke();
        return true;
    }
    public void UseHand(GameObject player)
    {
        if (handItem == null || handItem.itemData == null) return;

        ItemSystem.Instance.HandleUse(gameObject, handItem);

        if (handItem.IsBroken)
        {
            ClearHandVisual();
            handItem = null;
        }

        OnHandChanged?.Invoke();
    }
    public ItemInstance GetMainSlot(int i) => (i >= 0 && i < mainSlots.Count) ? mainSlots[i] : null;
    public ItemInstance GetBackpackSlotItem() => backpackSlot;
}
