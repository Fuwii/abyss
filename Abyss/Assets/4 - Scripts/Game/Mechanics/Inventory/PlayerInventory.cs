using Game.Mechanics.Interactables.Tools;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [Header("Hand mount (where runtime item prefab attaches)")]
    public Transform handMount;
    [Header("Inventory settings")]
    public int mainSlotsCount = 4;


    // public  UI  events 
    public event Action OnInventoryChanged;
    public event Action OnBackpackChanged;
    public event Action OnHandChanged;


    // main slots
    [SerializeField]
    public List<ItemInstance> mainSlots;


    // hand (single)
    [SerializeField]
    public ItemInstance handItem;
    // backpack
    public bool backpackWorn => backpackData != null;
    public BackpackItemData backpackItem {
        get {  return backpackData; }
     }
    private BackpackItemData backpackData;
    private List<ItemInstance> backpackSlots;
    //helpers
    public ItemInstance GetMainSlot(int i) => (i >= 0 && i < mainSlots.Count) ? mainSlots[i] : null;
    public ItemInstance GetBackpackSlot(int i) => (backpackWorn && i >= 0 && i < backpackSlots.Count) ? backpackSlots[i] : null;
    public int GetBackpackSize() => backpackWorn ? backpackSlots.Count : 0;
    public BackpackItemData GetBackpackData() => backpackData;

    private void Awake()
    {
        mainSlots = new List<ItemInstance>(mainSlotsCount);
        for (int i = 0; i < mainSlotsCount; i++)
            mainSlots.Add(null);
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

        if (instance.itemData is BackpackItemData bdata)
        {
            WearBackpack(bdata);
            OnInventoryChanged?.Invoke();
            OnBackpackChanged?.Invoke();
            return true;
        }
        for (int i = 0; i < mainSlots.Count; i++)
        {
            if (mainSlots[i] == null || mainSlots[i].itemData == null)
            {
                mainSlots[i] = instance;
                OnInventoryChanged?.Invoke();
                return true;
            }
        }
        if (handItem==null || handItem.itemData==null)
        {
            handItem = instance;
            OnInventoryChanged?.Invoke();
            OnHandChanged?.Invoke();
            ItemSystem.Instance.HandleSelected(gameObject, handItem, handMount);
            return true;
        }
        return false;
    }
    public void DropFromHand(float force)
    {
        if (handItem == null || handItem.itemData == null)
            return;
        ItemSystem.Instance.HandleDropped(gameObject, handItem,force);
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

    public bool TryPutIntoBackpack(int index, ItemInstance item)
    {
        if (!backpackWorn) return false;
        if (index < 0 || index >= backpackSlots.Count) return false;
        if (backpackSlots[index] != null) return false;
        backpackSlots[index] = item;
        OnBackpackChanged?.Invoke();
        return true;
    }

    public bool SwapBackpackSlots(int a, int b)
    {
        if (!backpackWorn) return false;
        if (a < 0 || a >= backpackSlots.Count || b < 0 || b >= backpackSlots.Count) return false;
        var tmp = backpackSlots[a];
        backpackSlots[a] = backpackSlots[b];
        backpackSlots[b] = tmp;
        OnBackpackChanged?.Invoke();
        return true;
    }
    // Drop from backpack
    public ItemInstance DropFromBackpack(int index)
    {
        if (!backpackWorn) return null;
        if (index < 0 || index >= backpackSlots.Count) return null;
        var it = backpackSlots[index];
        backpackSlots[index] = null;
        OnBackpackChanged?.Invoke();
        return it;
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
        if (handItem != null && slotItem == null || slotItem.itemData == null)
        {
            ItemSystem.Instance.HandleDeselected(gameObject, handItem);
            mainSlots[slotIndex] = handItem;
            ClearHandVisual();
            handItem = null;
            Debug.Log("handitem setted null"+ handItem);
            OnInventoryChanged?.Invoke();
            OnHandChanged?.Invoke();
            return true;
        }
        if (handItem != null && slotItem != null || slotItem.itemData != null)
        {
            Debug.Log("bug");
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
    public void UseHand(GameObject player)
    {
        if (handItem == null || handItem.itemData == null)
            return;

        ItemSystem.Instance.HandleUse(player, handItem);

        if (handItem.IsBroken)
        {
            ClearHandVisual();
            handItem = null;
        }

        OnHandChanged?.Invoke();
    }

    public void WearBackpack(BackpackItemData data)
    {
        if (data == null) return;
        backpackData = data;
        backpackSlots = new List<ItemInstance>(data.capacity);
        for (int i = 0; i < data.capacity; i++)
            backpackSlots.Add(null);
    }
    public List<ItemInstance> RemoveBackpack()
    {
        var leftover = new List<ItemInstance>();
        if (!backpackWorn) return leftover;

        foreach (var it in backpackSlots)
        {
            if (it == null) continue;

            bool placed = false;
            for (int j = 0; j < mainSlots.Count; j++)
            {
                if (mainSlots[j] == null)
                {
                    mainSlots[j] = it;
                    placed = true;
                    break;
                }
            }

            if (!placed)
                leftover.Add(it);
        }

        backpackData = null;
        backpackSlots = null;
        OnInventoryChanged?.Invoke();
        OnBackpackChanged?.Invoke();
        return leftover;
    }
}
