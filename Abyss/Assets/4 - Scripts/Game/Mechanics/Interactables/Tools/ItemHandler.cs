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

    public virtual void OnSelected(GameObject player, ItemInstance instance, Transform handTransform, bool isOwner)
    {
        var data = instance.itemData;
        if (data == null || data.itemPrefab == null || handTransform == null) return;

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
        if (instance == null) return;

        OnDeselected(player, instance);

        if (instance.worldObject == null) return;
        instance.worldObject.gameObject.SetActive(true);
        if (!NetworkManager.Singleton.IsServer) return;

        var obj = instance.worldObject;
        obj.gameObject.SetActive(true);

        // Позиционирование
        obj.transform.position = player.transform.position + player.transform.forward * 1.2f + Vector3.up * 0.5f;
        obj.transform.rotation = Quaternion.identity;

        // Физика
        if (obj.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.AddForce(player.transform.forward * (force * 10f), ForceMode.VelocityChange);
        }
    }

}

