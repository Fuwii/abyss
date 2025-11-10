using Game.Mechanics.Interactables.Tools;
using System;
using System.Collections.Generic;

[Serializable]
public class BackpackComponent : IItemComponent
{
    public int capacity;
    public List<ItemInstance> contents;

    public BackpackComponent(int capacity)
    {
        this.capacity = capacity;
        contents = new List<ItemInstance>(capacity);
        for (int i = 0; i < capacity; i++) contents.Add(null);
    }
}

