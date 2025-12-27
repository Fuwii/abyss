using Game.Mechanics.Interactables.Tools;
using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public Transform handMount;
    public int mainSlotsCount = 4; 

    public event Action OnInventoryChanged;
    public event Action OnBackpackChanged;
    public event Action OnHandChanged;

    [SerializeField] public List<ItemInstance> inventorySlots;
    public int selectedSlot = -1;

    int backpackIndex => mainSlotsCount + 1;
    public bool backpackWorn => inventorySlots != null && inventorySlots.Count > backpackIndex && inventorySlots[backpackIndex] != null;

    void Awake()
    {
        int total = mainSlotsCount + 2;
        inventorySlots = new List<ItemInstance>(total);
        for (int i = 0; i < total; i++) inventorySlots.Add(null);
    }

    void ClearHandVisual()
    {
        var hand = inventorySlots[0];
        if (hand?.runtimeHeldObject != null)
        {
            Destroy(hand.runtimeHeldObject);
            hand.runtimeHeldObject = null;
        }
    }

    public void SetSelectedSlot(int index)
    {
        if (index < 0 || index >= inventorySlots.Count) return;
        if (selectedSlot == index)
        {
            DeselectCurrent();
            return;
        }

        if (index != 0)
        {
            var hand = inventorySlots[0];
            if (hand != null && hand.itemData != null)
            {
                ItemSystem.Instance.HandleDropped(gameObject, hand, 0.5f);
                ClearHandVisual();
                inventorySlots[0] = null;
                OnHandChanged?.Invoke();
                OnInventoryChanged?.Invoke();
            }
        }

        DeselectCurrent();
        var it = inventorySlots[index];
        if (it == null || it.itemData == null)
        {
            selectedSlot = -1;
            return;
        }

        selectedSlot = index;
        ItemSystem.Instance.HandleSelected(gameObject, it, handMount);
    }

    public void UseSelected(GameObject player)
    {
        var sel = GetSelectedInstance();
        if (sel == null || sel.itemData == null) return;
        ItemSystem.Instance.HandleUse(gameObject, sel);
        if (sel.IsBroken) RemoveSelected();
    }

    public void DropSelected(float force)
    {
        var sel = GetSelectedInstance();
        if (sel == null || sel.itemData == null) return;
        if (selectedSlot == backpackIndex)
        {
            ItemSystem.Instance.HandleDropped(gameObject, inventorySlots[backpackIndex], force);
            inventorySlots[backpackIndex] = null;
            OnBackpackChanged?.Invoke();
            OnInventoryChanged?.Invoke();
        }
        else
        {
            ItemSystem.Instance.HandleDropped(gameObject, inventorySlots[selectedSlot], force);
            inventorySlots[selectedSlot] = null;
            OnInventoryChanged?.Invoke();
        }

        DeselectCurrent();
    }

    public ItemInstance GetSlot(int i) => (i >= 0 && i < inventorySlots.Count) ? inventorySlots[i] : null;

    public bool SwapSlots(int a, int b)
    {
        if (a < 0 || a >= inventorySlots.Count || b < 0 || b >= inventorySlots.Count) return false;
        if (a == b) return true;
        var tmp = inventorySlots[a];
        inventorySlots[a] = inventorySlots[b];
        inventorySlots[b] = tmp;
        OnInventoryChanged?.Invoke();
        return true;
    }
    public bool TryPutIntoSlot(int index, ItemInstance item)
    {
        if (index < 0 || index >= inventorySlots.Count) return false;
        if (inventorySlots[index] != null) return false;
        inventorySlots[index] = item;
        OnInventoryChanged?.Invoke();
        return true;
    }

    public ItemInstance RemoveFromSlot(int index)
    {
        if (index < 0 || index >= inventorySlots.Count) return null;
        var it = inventorySlots[index];
        inventorySlots[index] = null;
        OnInventoryChanged?.Invoke();
        return it;
    }
    public bool TryPickup(ItemInstance instance)
    {
        if (instance == null || instance.itemData == null) return false;
        if (instance.itemData is BackpackItemData backpackData)
        {
            var old = inventorySlots[backpackIndex];
            if (old != null)
            {
                ItemSystem.Instance.HandleDropped(gameObject, old, 0.5f);
                inventorySlots[backpackIndex] = null;
                if (selectedSlot == backpackIndex)
                    DeselectCurrent();
                OnBackpackChanged?.Invoke();
                OnInventoryChanged?.Invoke();
            }
            inventorySlots[backpackIndex] = instance;
            OnBackpackChanged?.Invoke();
            OnInventoryChanged?.Invoke();
            return true;
        }
        for (int i = 1; i <= mainSlotsCount; i++)
            if (inventorySlots[i] == null)
            {
                inventorySlots[i] = instance;
                OnInventoryChanged?.Invoke();
                return true;
            }
        var oldHand = inventorySlots[0];
        if (oldHand != null && oldHand.itemData != null)
        {
            ItemSystem.Instance.HandleDropped(gameObject, oldHand, 0.5f);
            ClearHandVisual();
            inventorySlots[0] = null;
            OnHandChanged?.Invoke();
            OnInventoryChanged?.Invoke();
        }

        inventorySlots[0] = instance;
        ItemSystem.Instance.HandleSelected(gameObject, inventorySlots[0], handMount);
        SetSelectedSlot(0);
        OnInventoryChanged?.Invoke();
        OnHandChanged?.Invoke();
        return true;
    }

    // ---------- Backpack helpers ----------
    public bool SwapBackpackSlots(int a, int b)
    {
        var comp = GetBackpackComponent();
        if (comp == null) return false;
        if (a < 0 || a >= comp.contents.Count || b < 0 || b >= comp.contents.Count) return false;
        if (a == b) return true;

        var tmp = comp.contents[a];
        comp.contents[a] = comp.contents[b];
        comp.contents[b] = tmp;

        OnBackpackChanged?.Invoke();
        return true;
    }

    public BackpackComponent GetBackpackComponent()
    {
        var bp = (backpackIndex >= 0 && backpackIndex < inventorySlots.Count) ? inventorySlots[backpackIndex] : null;
        return bp != null ? bp.GetComponent<BackpackComponent>() : null;
    }

    public int GetBackpackSize()
    {
        var c = GetBackpackComponent();
        return c != null ? c.capacity : 0;
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

    void DeselectCurrent()
    {
        if (selectedSlot < 0 || selectedSlot >= inventorySlots.Count) { selectedSlot = -1; return; }
        var cur = inventorySlots[selectedSlot];
        if (cur != null) ItemSystem.Instance.HandleDeselected(gameObject, cur);
        selectedSlot = -1;
    }

    ItemInstance GetSelectedInstance()
    {
        if (selectedSlot >= 0 && selectedSlot < inventorySlots.Count) return inventorySlots[selectedSlot];
        return null;
    }

    void RemoveSelected()
    {
        if (selectedSlot < 0) return;
        if (selectedSlot == backpackIndex)
        {
            inventorySlots[backpackIndex] = null;
            OnBackpackChanged?.Invoke();
            OnInventoryChanged?.Invoke();
        }
        else
        {
            inventorySlots[selectedSlot] = null;
            OnInventoryChanged?.Invoke();
        }
        DeselectCurrent();
    }
}
