using Core.Singleton;
using Game.Mechanics.Interactables.Tools;
using System;
using System.Collections.Generic;
using UnityEngine;

public class ItemSystem : Singleton<ItemSystem>
{
    private readonly Dictionary<Type, ItemHandler> _handlers = new();

    protected override void Awake()
    {
        base.Awake();
        Debug.LogWarning(gameObject.name);
        RegisterHandler(typeof(ItemData), new ItemHandler());
        RegisterHandler(typeof(WeaponData), new WeaponHandler());
        RegisterHandler(typeof(ConsumableData), new ConsumableHandler());
        RegisterHandler(typeof(BackpackItemData), new BackpackHandler());
    }

    public void RegisterHandler(Type dataType, ItemHandler handler)
    {
        if (dataType == null || handler == null)
        {
            Debug.LogWarning("Attempted to register null handler or data type.");
            return;
        }

        _handlers[dataType] = handler;
    }

    private ItemHandler GetHandler(ItemData data)
    {
        if (data == null)
            return _handlers[typeof(ItemData)];

        var t = data.GetType();
        return _handlers.TryGetValue(t, out var h)
            ? h
            : _handlers[typeof(ItemData)];
    }

    public void HandlePickup(GameObject player, ItemInstance instance)
        => GetHandler(instance.itemData).OnPickup(player, instance);

    public void HandleSelected(GameObject player, ItemInstance instance, Transform handTransform)
        => GetHandler(instance.itemData).OnSelected(player, instance, handTransform);

    public void HandleUse(GameObject player, ItemInstance instance)
        => GetHandler(instance.itemData).OnUse(player, instance);
    public void HandleDeselected(GameObject player, ItemInstance instance)
        => GetHandler(instance.itemData).OnDeselected(player, instance);

    public void HandleDropped(GameObject player, ItemInstance instance, float force)
        => GetHandler(instance.itemData).OnDropped(player, instance, force);
}
