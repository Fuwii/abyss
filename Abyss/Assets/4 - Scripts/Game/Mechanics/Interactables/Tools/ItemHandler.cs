using Game.Mechanics.Interactables.Tools;
using Unity.Netcode;
using UnityEngine;
public class ItemHandler : IItemHandler
{
    public virtual void OnPickup(GameObject player, ItemInstance instance)
    {
       
    }
    public virtual void OnDeselected(GameObject player, ItemInstance instance)
    {
        if (instance == null) return;
        if (instance.runtimeHeldObject != null)
        {
            Object.Destroy(instance.runtimeHeldObject);
            instance.runtimeHeldObject = null;
        }
    }

    public virtual void OnSelected(GameObject player, ItemInstance instance, Transform handTransform)
    {
        var data = instance.itemData;
        if (data == null || data.itemPrefab == null || handTransform == null) return;

        // Удаляем старый визуал, если он был
        if (instance.runtimeHeldObject != null)
        {
            Object.Destroy(instance.runtimeHeldObject);
        }

        // Создаем ЛОКАЛЬНУЮ копию префаба. 
        // Поскольку этот код выполнится на всех клиентах через OnNetworkListChanged,
        // все увидят предмет в руках этого игрока.
        var go = Object.Instantiate(data.itemPrefab, handTransform);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;

        // Выключаем NetworkObject на визуальной копии, чтобы он не конфликтовал 
        // (визуал в руке — это просто "пустышка", а не реальный сетевой объект)
        if (go.TryGetComponent<NetworkObject>(out var netObj))
        {
            netObj.enabled = false;
        }

        // И отключаем физику, чтобы предмет не улетел из рук
        if (go.TryGetComponent<Rigidbody>(out var rb)) rb.isKinematic = true;

        instance.runtimeHeldObject = go;
    }

    public virtual void OnUse(GameObject player, ItemInstance instance)
    {
        instance?.UseOne();
    }

    public virtual void OnDropped(GameObject player, ItemInstance instance, float force)
    {
        var data = instance?.itemData;
        if (data == null || data.itemPrefab == null) return;

        var dropPos = player.transform.position + player.transform.forward * 1.5f;
        var dropRot = Quaternion.identity;

        var dropped = Object.Instantiate(data.itemPrefab, dropPos, dropRot);

        var rb = dropped.GetComponent<Rigidbody>();
        if(rb == null)
            rb = dropped.AddComponent<Rigidbody>();
        rb.useGravity = true;
        Debug.Log(force);
        rb.AddForce(player.transform.forward * (force*10), ForceMode.VelocityChange);

        var behaviour = dropped.GetComponent<ItemBehaviour>();
        if (behaviour != null)
        {
            behaviour.data = instance.itemData;
            behaviour.itemInstance = instance;
        }
        if (instance.runtimeHeldObject != null)
        {
            Object.Destroy(instance.runtimeHeldObject);
            instance.runtimeHeldObject = null;
        }
    }
}

