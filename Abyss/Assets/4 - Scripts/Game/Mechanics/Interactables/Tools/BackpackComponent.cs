using Game.Mechanics.Interactables.Tools;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BackpackComponent : IItemComponent
{
    public int capacity;
    public List<ItemInstance> contents;

    public GameObject uiPrefab; 
    public GameObject activeUIInstance;

    public BackpackComponent(int capacity, GameObject uiPrefab = null)
    {
        this.capacity = capacity;
        this.uiPrefab = uiPrefab;
        contents = new List<ItemInstance>(capacity);
        for (int i = 0; i < capacity; i++) contents.Add(null);
    }
}
