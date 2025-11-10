using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Mechanics.Interactables.Tools
{
    [Serializable]
    public class ItemInstance
    {
        public ItemData itemData;
        public int remainingUses;

        public GameObject runtimeHeldObject;

        public List<IItemComponent> components = new();

        public ItemInstance() { }

        public ItemInstance(ItemData data)
        {
            itemData = data;
            remainingUses = data != null ? data.uses : 0;
        }

        public T GetComponent<T>() where T : class, IItemComponent
        {
            foreach (var c in components)
                if (c is T t) return t;
            return null;
        }

        public void AddComponent(IItemComponent c) => components.Add(c);
        public void UseOne()
        {
            if (itemData == null || itemData.uses <= 0) return;

            remainingUses = Mathf.Max(0, remainingUses - 1);
        }

        public bool IsBroken => itemData != null && itemData.uses > 0 && remainingUses <= 0;
    }
}
