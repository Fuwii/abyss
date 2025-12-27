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
        // Сначала всегда тотальная зачистка
        ClearHandVisual();

        int currentIndex = netSelectedSlot.Value;

        // Если индекс валидный и там есть предмет — рисуем
        if (currentIndex >= 0 && currentIndex < inventorySlots.Count)
        {
            var it = inventorySlots[currentIndex];
            if (it != null && it.itemData != null)
            {
                ItemSystem.Instance.HandleSelected(gameObject, it, handMount);
            }
        }
    }
    [ServerRpc(RequireOwnership = false)]
    public void RequestPickupServerRpc(NetworkObjectReference itemRef, ServerRpcParams rpcParams = default)
    {
        // 1. Пытаемся достать объект из ссылки
        if (!itemRef.TryGet(out NetworkObject itemNetObj)) return;

        // 2. Достаем компонент предмета
        var itemBehaviour = itemNetObj.GetComponent<ItemBehaviour>();
        if (itemBehaviour == null) return;

        short id = itemBehaviour.data.id;
        int uses = itemBehaviour.itemInstance.remainingUses;

        // 3. ТВОЯ ПЕРЕДЕЛАННАЯ ЛОГИКА TryPickup
        // Важно: на сервере мы меняем NetworkList (netSlots), 
        // а клиенты обновят свои inventorySlots автоматически через OnListChanged

        if (itemBehaviour.data is BackpackItemData)
        {
            if (netSlots[backpackIndex].ItemID != -1)
            {
                // Логика "выбросить старый рюкзак", если нужно...
            }
            netSlots[backpackIndex] = new NetworkItem { ItemID = id, RemainingUses = uses };
            itemNetObj.Despawn(true); // Успех!
            return;
        }

        // Ищем пустое место в основных слотах
        for (int i = 1; i <= mainSlotsCount; i++)
        {
            if (netSlots[i].ItemID == -1) // -1 значит пусто
            {
                netSlots[i] = new NetworkItem { ItemID = id, RemainingUses = uses };
                itemNetObj.Despawn(true); // Успех!
                return;
            }
        }

        // Если места нет в слотах, пробуем взять в руки (0 слот)
        if (netSlots[0].ItemID == -1)
        {
            netSlots[0] = new NetworkItem { ItemID = id, RemainingUses = uses };
            itemNetObj.Despawn(true);
            return;
        }

        // Если дошли сюда — инвентарь реально полон, ничего не делаем.
        Debug.Log("Server: No space for item");
    }
    private void OnNetworkListChanged(NetworkListEvent<NetworkItem> changeEvent)
    {
        SyncLocalInventory(); // Обновили данные

        // ВИЗУАЛ: Обновляем руку только если изменился Тот Самый слот, который сейчас выбран
        if (changeEvent.Index == netSelectedSlot.Value)
        {
            UpdateHandVisual();
        }
        // 3. UI ОБНОВЛЯЕМ ТОЛЬКО ДЛЯ ВЛАДЕЛЬЦА
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
            if (netSlots[i].ItemID == -1)
            {
                inventorySlots[i] = null;
            }
            else
            {
                var data = ItemDatabase.GetItem(netSlots[i].ItemID);
                inventorySlots[i] = new ItemInstance(data) { remainingUses = netSlots[i].RemainingUses };
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
        if (!IsOwner) return; // Только владелец может нажать на кнопку выбора

        // Вместо прямого изменения вызываем RPC
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
}
