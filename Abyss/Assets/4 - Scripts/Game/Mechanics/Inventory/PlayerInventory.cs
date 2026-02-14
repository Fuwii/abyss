using Game.Mechanics.Interactables.Tools;
using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerInventory : NetworkBehaviour
{
    public Transform handMount;
    public int mainSlotsCount = 4; 

    public event Action OnInventoryChanged;
    public event Action OnBackpackChanged;
    public event Action OnHandChanged;

    public NetworkList<NetworkItem> netSlots;
    [SerializeField] public List<ItemInstance> inventorySlots;
    public NetworkVariable<int> netSelectedSlot = new NetworkVariable<int>(-1);
    public int selectedSlot => netSelectedSlot.Value;

    int backpackIndex => mainSlotsCount + 1;
    public bool backpackWorn => inventorySlots != null && inventorySlots.Count > backpackIndex && inventorySlots[backpackIndex] != null;
    public NetworkVariable<NetworkObjectReference> wornBackpack;
    public Transform backpackMount;

    void Awake()
    {
        netSlots = new NetworkList<NetworkItem>();
        int total = mainSlotsCount + 2;
        inventorySlots = new List<ItemInstance>(total);
        for (int i = 0; i < total; i++) inventorySlots.Add(null);
    }
    public override void OnNetworkSpawn()
    {
        netSelectedSlot.OnValueChanged += OnSelectedSlotChanged;

        UpdateHandVisual();
        if (IsServer)
        {
            if (netSlots.Count == 0)
            {
                int totalSlots = mainSlotsCount + 2;
                for (int i = 0; i < totalSlots; i++)
                {
                    netSlots.Add(new NetworkItem { ItemID = -1, RemainingUses = 0 });
                }
            }
        }
        netSlots.OnListChanged += OnNetworkListChanged;
        SyncLocalInventory();
    }
    private void OnSelectedSlotChanged(int previousValue, int newValue)
    {
        if (previousValue >= 0 && previousValue < inventorySlots.Count)
        {
            var oldItem = inventorySlots[previousValue];
            if (oldItem != null)
            {
                ItemSystem.Instance.HandleDeselected(gameObject, oldItem);
            }
        }
        UpdateHandVisual();
    }
    private void UpdateHandVisual()
    {
        ClearHandVisual();

        int currentIndex = netSelectedSlot.Value;

        if (currentIndex >= 0 && currentIndex < inventorySlots.Count)
        {
            var it = inventorySlots[currentIndex];
            if (it != null && it.itemData != null)
            {
                Debug.Log("Handle selected");
                ItemSystem.Instance.HandleSelected(gameObject, it, handMount, IsOwner);
            }
        }
    }
    [ServerRpc(RequireOwnership = false)]
    public void RequestPickupServerRpc(NetworkObjectReference itemRef, ServerRpcParams rpcParams = default)
    {
        if (!itemRef.TryGet(out NetworkObject itemNetObj)) return;

        var itemBehaviour = itemNetObj.GetComponent<ItemBehaviour>();
        if (itemBehaviour == null) return;

        var instance = itemBehaviour.itemInstance;
        if (instance == null) return;

        // Сразу привязываем объект на сервере (чтобы Handler его видел)
        instance.worldObject = itemNetObj;

        short id = itemBehaviour.data.id;
        int uses = instance.remainingUses;

        // СОЗДАЕМ ТОВАР С ССЫЛКОЙ (чтобы клиент потом оживил worldObject)
        var pickedItem = new NetworkItem
        {
            ItemID = id,
            RemainingUses = uses,
            WorldObjRef = itemNetObj
        };

        // 1. ПРИОРИТЕТ: Рюкзак
        if (itemBehaviour.data is BackpackItemData)
        {
            netSlots[backpackIndex] = pickedItem;
            ItemSystem.Instance.HandlePickup(gameObject, instance);
            FinalizePickup(itemNetObj);
            return;
        }

        // 2. ПРИОРИТЕТ: Обычные слоты (от 1 до mainSlotsCount)
        for (int i = 1; i <= mainSlotsCount; i++)
        {
            if (netSlots[i].ItemID == -1)
            {
                netSlots[i] = pickedItem;
                FinalizePickup(itemNetObj);
                return;
            }
        }

        // 3. ПРИОРИТЕТ: Слот руки (0) — только если остальное занято
        if (netSlots[0].ItemID == -1)
        {
            netSlots[0] = pickedItem;
            FinalizePickup(itemNetObj);
            return;
        }

        Debug.Log("Server: No space for item");
    }

    // Вспомогательный метод, чтобы не дублировать код выключения объекта
    private void FinalizePickup(NetworkObject itemNetObj)
    {
        itemNetObj.gameObject.SetActive(false);
        itemNetObj.TrySetParent(transform); // Цепляем к игроку, чтобы объект перемещался с ним
    }

    private void OnNetworkListChanged(NetworkListEvent<NetworkItem> changeEvent)
    {
        SyncLocalInventory(); 

        if (changeEvent.Index == netSelectedSlot.Value)
        {
            UpdateHandVisual();
        }
        if (IsOwner)
    {
        OnInventoryChanged?.Invoke();
        if (changeEvent.Index == 0) OnHandChanged?.Invoke();
        if (changeEvent.Index == backpackIndex) OnBackpackChanged?.Invoke();
    }
}
    void SyncLocalInventory()
    {
        for (int i = 0; i < netSlots.Count; i++)
        {
            var netItem = netSlots[i];

            if (netItem.ItemID == -1)
            {
                if (inventorySlots[i] != null)
                {
                    ItemSystem.Instance.HandleDropped(gameObject, inventorySlots[i], 0);
                    inventorySlots[i] = null;
                }
                continue;
            }

            // Если предмета не было или он изменился (МОМЕНТ ПОДБОРА)
            if (inventorySlots[i] == null || inventorySlots[i].itemData.id != netItem.ItemID)
            {
                var data = ItemDatabase.GetItem(netItem.ItemID);
                var inst = new ItemInstance(data) { remainingUses = netItem.RemainingUses };

                if (netItem.WorldObjRef.TryGet(out NetworkObject netObj))
                {
                    inst.worldObject = netObj;

                    // ВОТ ТУТ: Принудительно гасим объект у клиента при подборе
                    if (netObj != null)
                    {
                        netObj.gameObject.SetActive(false);
                    }
                }

                inventorySlots[i] = inst;

                if (i == backpackIndex)
                {
                    ItemSystem.Instance.HandlePickup(gameObject, inst);
                }
            }
        }
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
        if (!IsOwner) return; 

        RequestSetSelectedSlotServerRpc(index);
    }
    [ServerRpc]
    private void RequestSetSelectedSlotServerRpc(int index)
    {
        
        if (index == -1)
        {
            netSelectedSlot.Value = -1;
            return;
        }

        if (index < 0 || index >= netSlots.Count) return;

        if (netSelectedSlot.Value == index)
        {
            netSelectedSlot.Value = -1;
        }
        else
        {
            netSelectedSlot.Value = index;
        }
        Debug.Log("Index " + index);
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
        if (!IsOwner) return;

        int slot = selectedSlot;
        if (slot < 0) return;
        RequestDropServerRpc(slot, force);

        DeselectCurrent();
    }

    [ServerRpc]
    private void RequestDropServerRpc(int slotIndex, float force)
    {
        if (slotIndex < 0 || slotIndex >= netSlots.Count) return;

        var netItem = netSlots[slotIndex];
        if (netItem.ItemID == -1) return;

        // На сервере у нас есть доступ к тому же инстансу
        var inst = inventorySlots[slotIndex];
        if (inst == null) return;

        if (inst.worldObject != null)
        {
            var netObj = inst.worldObject;

            // 1. Сначала отцепляем от игрока (Важно!)
            netObj.TryRemoveParent();

            // 2. Включаем (это синхронизируется)
            netObj.gameObject.SetActive(true);

            // 3. Вызываем хендлер, где включается физика
            ItemSystem.Instance.HandleDropped(gameObject, inst, force);
        }
        // ВАЖНО: Очищаем сетевой слот, чтобы он не дублировался при подборе!
        netSlots[slotIndex] = new NetworkItem { ItemID = -1, RemainingUses = 0, WorldObjRef = default };

        // Если это был рюкзак, сбрасываем переменную wornBackpack
        if (slotIndex == backpackIndex)
        {
            wornBackpack.Value = default;
        }
    }

    public ItemInstance GetSlot(int i) => (i >= 0 && i < inventorySlots.Count) ? inventorySlots[i] : null;

    void DeselectCurrent()
    {
        if (!IsOwner) return;

        RequestSetSelectedSlotServerRpc(-1);
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
    // ---------- Backpack helpers ----------
    public NetworkContainer GetBackpack()
    {
        if (!wornBackpack.Value.TryGet(out var obj)) return null;
        return obj.GetComponent<NetworkContainer>();
    }
    public int GetBackpackSize()
    {
        var bp = GetBackpack();
        return bp != null ? bp.Capacity : 0;
    }
    [ServerRpc(RequireOwnership = false)]
    public void PutIntoBackpackServerRpc(
    NetworkObjectReference backpackRef,
    int slot,
    NetworkItem item)
    {
        if (!backpackRef.TryGet(out var obj)) return;

        var bp = obj.GetComponent<NetworkContainer>();
        if (bp == null) return;

        bp.TryPut(slot, item);
    }
    //new
    public ItemInstance GetBackpackContentsAt(int index)
    {
        var bp = GetBackpack();
        if (bp == null) return null;

        if (index < 0 || index >= bp.Contents.Count) return null;

        var netItem = bp.Contents[index];
        if (netItem.ItemID == -1) return null;

        var data = ItemDatabase.GetItem(netItem.ItemID);
        return new ItemInstance(data)
        {
            remainingUses = netItem.RemainingUses
        };
    }
    //move slots
    [ServerRpc(RequireOwnership = false)]
    public void MoveItemServerRpc(InventorySource from, int fromSlot, InventorySource to, int toSlot)
    {
        NetworkItem sourceItem = GetNetworkItem(from, fromSlot);
        if (sourceItem.ItemID == -1) return;
        NetworkItem targetItem = GetNetworkItem(to, toSlot);
        SetNetworkItem(to, toSlot, sourceItem);
        SetNetworkItem(from, fromSlot, targetItem);
        if (IsOwner)
        {
            OnInventoryChanged?.Invoke();
            if (from == InventorySource.Backpack || to == InventorySource.Backpack)
                OnBackpackChanged?.Invoke();
        }
    }

    private NetworkItem GetNetworkItem(InventorySource source, int slot)
    {
        if (source == InventorySource.PlayerInventory)
            return netSlots[slot];
        else
            return GetBackpack()?.Contents[slot] ?? new NetworkItem { ItemID = -1, RemainingUses = 0 };
    }

    private void SetNetworkItem(InventorySource source, int slot, NetworkItem item)
    {
        if (source == InventorySource.PlayerInventory)
            netSlots[slot] = item;
        else
        {
            var bp = GetBackpack();
            if (bp != null)
                bp.Contents[slot] = item;
        }
    }
    public enum InventorySource
    {
        PlayerInventory,
        Backpack
    }
}

