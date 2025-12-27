using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Game.Mechanics.Interactables.Tools;

public class ItemDatabase : MonoBehaviour
{
    [SerializeField] private List<ItemData> items;

    private static Dictionary<short, ItemData> _items;
    private static bool _isInitialized;

    private void Awake()
    {
        Initialize(this);
    }

    private static void Initialize(ItemDatabase instance)
    {
        if (_isInitialized) return;

        _items = new Dictionary<short, ItemData>();

        foreach (var data in instance.items)
        {
            if (data == null)
                continue;

            if (!_items.TryAdd(data.id, data))
                Debug.LogError($"Duplicate Item ID: {data.id} on {data.name}", data);
        }

        _isInitialized = true;
        Debug.Log($"ItemDatabase: Loaded {_items.Count} items.");
    }

    public static ItemData GetItem(short id)
    {
        if (!_isInitialized)
        {
            Debug.LogError("ItemDatabase is not initialized. Make sure an ItemDatabase exists in the scene.");
            return null;
        }

        _items.TryGetValue(id, out var item);
        return item;
    }
}