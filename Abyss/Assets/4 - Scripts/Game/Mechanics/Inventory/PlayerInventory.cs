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


    // public events so UI can subscribe
    public event Action OnInventoryChanged;
    public event Action OnBackpackChanged;
    public event Action OnHandChanged;


    // main slots
    public List<ItemInstance> mainSlots;


    // hand (single)
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
        for (int i = 0; i < mainSlotsCount; i++) mainSlots.Add(null);
    }
    private void ClearHandVisual()
    {
        if (handItem != null && handItem.runtimeHeldObject != null)
        {
            Destroy(handItem.runtimeHeldObject);
            handItem.runtimeHeldObject = null;
        }
    }


    private void SpawnHandVisual(ItemInstance inst)
    {
        if (inst == null || inst.itemData == null || inst.itemData.itemPrefab == null || handMount == null) return;
        inst.runtimeHeldObject = Instantiate(inst.itemData.itemPrefab, handMount);
        inst.runtimeHeldObject.transform.localPosition = Vector3.zero;
        inst.runtimeHeldObject.transform.localRotation = Quaternion.identity;
    }
    public bool TryPickup(ItemInstance instance)
    {
        if (instance == null || instance.itemData == null) return false;


        // If item is backpack item -> wear it
        var bdata = instance.itemData as BackpackItemData;
        if (bdata != null)
        {
            WearBackpack(bdata);
            OnInventoryChanged?.Invoke();
            OnBackpackChanged?.Invoke();
            return true;
        }


        // try to put into first empty main slot
        for (int i = 0; i < mainSlots.Count; i++)
        {
            if (mainSlots[i] == null)
            {
                mainSlots[i] = instance;
                OnInventoryChanged?.Invoke();
                return true;
            }
        }


        // try backpack if worn
        if (backpackWorn)
        {
            for (int i = 0; i < backpackSlots.Count; i++)
            {
                if (backpackSlots[i] == null)
                {
                    backpackSlots[i] = instance;
                    OnBackpackChanged?.Invoke();
                    return true;
                }
            }
        }


        // inventory full
        return false;
    }
    public ItemInstance DropFromMain(int index)
    {
        if (index < 0 || index >= mainSlots.Count) return null;
        var it = mainSlots[index];
        mainSlots[index] = null;
        OnInventoryChanged?.Invoke();
        return it;
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
        if (mainSlots[slotIndex] == null) return false;


        if (handItem != null)
        {
            // swap
            ClearHandVisual();
            var old = handItem;
            handItem = mainSlots[slotIndex];
            ClearHandVisual();
            SpawnHandVisual(handItem);
            mainSlots[slotIndex] = old;
        }
        else
        {
            handItem = mainSlots[slotIndex];
            mainSlots[slotIndex] = null;
            ClearHandVisual();
            SpawnHandVisual(handItem);
        }


        OnInventoryChanged?.Invoke();
        OnHandChanged?.Invoke();
        return true;
    }
    public bool PutHandToFirstFreeMain()
    {
        if (handItem == null) return false;
        for (int i = 0; i < mainSlots.Count; i++)
        {
            if (mainSlots[i] == null)
            {
                mainSlots[i] = handItem;
                ClearHandVisual();
                handItem = null;
                OnInventoryChanged?.Invoke();
                OnHandChanged?.Invoke();
                return true;
            }
        }
        return false;
    }
    public void UseHand(GameObject player)
    {
        if (handItem == null || handItem.itemData == null) return;
        handItem.itemData.OnUse(player, handItem);
        handItem.UseOne();
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
        for (int i = 0; i < data.capacity; i++) backpackSlots.Add(null);

    }
    public List<ItemInstance> RemoveBackpack()
    {
        var leftover = new List<ItemInstance>();
        if (!backpackWorn) return leftover;


        // try move items into main first
        for (int i = 0; i < backpackSlots.Count; i++)
        {
            var it = backpackSlots[i];
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


            if (!placed) leftover.Add(it);
        }


        backpackData = null;
        backpackSlots = null;
        OnInventoryChanged?.Invoke();
        OnBackpackChanged?.Invoke();
        return leftover;
    }
}
