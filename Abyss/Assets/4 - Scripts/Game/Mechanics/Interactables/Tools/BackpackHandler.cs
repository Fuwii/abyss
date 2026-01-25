using Game.Mechanics.Interactables.Tools;
using System.Globalization;
using Unity.Netcode;
using UnityEngine;

public class BackpackHandler : ItemHandler
{
    public override void OnPickup(GameObject player, ItemInstance instance)
    {
        if (instance == null) return;

        var data = instance.itemData as BackpackItemData;
        if (data == null || data.itemPrefab == null) return;

        var inv = player.GetComponent<PlayerInventory>();
        if (inv == null || inv.backpackMount == null) return;

        if (instance.runtimeBackpackVisual != null)
        {
            Object.Destroy(instance.runtimeBackpackVisual);
            instance.runtimeBackpackVisual = null;
        }
        if (NetworkManager.Singleton.IsServer)
        {
            instance.worldObject.gameObject.SetActive(false);
        }
        var visual = Object.Instantiate(data.itemPrefab, inv.backpackMount);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;

        if (visual.TryGetComponent<NetworkObject>(out var net))
            net.enabled = false;

        if (visual.TryGetComponent<Rigidbody>(out var rb))
            rb.isKinematic = true;

        instance.runtimeBackpackVisual = visual;
        inv.wornBackpack.Value = instance.worldObject; 
    }

    public override void OnSelected(GameObject player, ItemInstance instance, Transform handTransform, bool isOwner)
    {
        base.OnSelected(player, instance, handTransform, isOwner);

        if (instance == null || !isOwner) return;
        var bp = instance.GetComponent<BackpackComponent>();
        
        if (bp == null) return;
        Debug.Log("BP component:" + bp == null);
        if (bp.activeUIInstance != null) return;
        Debug.Log(bp.activeUIInstance);
        if (bp.uiPrefab == null)
        {
            return;
        }
        Canvas targetCanvas = player.GetComponentInChildren<Canvas>();
        Transform parent = targetCanvas != null ? targetCanvas.transform : null;

        bp.activeUIInstance = Object.Instantiate(bp.uiPrefab, parent);
        var ui = bp.activeUIInstance.GetComponent<BackpackUI>();
        if (ui != null)
        {
            var inv = player.GetComponent<PlayerInventory>();
            ui.playerInventory = inv;
            ui.gameObject.SetActive(true);
            ui.RefreshBackpack();
        }
    }

    public override void OnDeselected(GameObject player, ItemInstance instance)
    {
        // Сначала база (удалит предмет из руки)
        base.OnDeselected(player, instance);

        // Затем UI рюкзака
        var bp = instance.GetComponent<BackpackComponent>();
        if (bp != null && bp.activeUIInstance != null)
        {
            Object.Destroy(bp.activeUIInstance);
            bp.activeUIInstance = null;
        }
    }
    public override void OnDropped(GameObject player, ItemInstance instance, float force)
    {
        // Вызываем базу: она вызовет наш OnDeselected (закроет UI) 
        // и включит worldObject на сервере
        base.OnDropped(player, instance, force);

        // Нам остается только почистить то, что база не знает — модель на спине
        if (instance.runtimeBackpackVisual != null)
        {
            Object.Destroy(instance.runtimeBackpackVisual);
            instance.runtimeBackpackVisual = null;
        }

        // И на сервере отцепить рюкзак от игрока
        if (NetworkManager.Singleton.IsServer && instance.worldObject != null)
        {
            instance.worldObject.TryRemoveParent();

            var inv = player.GetComponent<PlayerInventory>();
            if (inv != null) inv.wornBackpack.Value = default;
        }
    }
    private void SetEquippedState(NetworkObject obj, bool equipped)
    {
        if (obj.TryGetComponent<Rigidbody>(out var rb))
            rb.isKinematic = equipped;

        if (obj.TryGetComponent<Collider>(out var col))
            col.enabled = !equipped;
    }

    private void ForceDropBackpack(NetworkObject obj)
    {
        obj.TryRemoveParent();
        SetEquippedState(obj, false);
    }

}
