using System;
using UnityEngine;

namespace Game.Mechanics.Interactables.Tools
{
    [Serializable]
    public class ItemInstance
    {
        public ItemData itemData;
        public int remainingUses;

        [NonSerialized] public GameObject runtimeHeldObject;

        public ItemInstance() { }

        public ItemInstance(ItemData data)
        {
            itemData = data;
            remainingUses = data != null ? data.uses : 0;
        }

        public void UseOne()
        {
            if (itemData == null || itemData.uses <= 0) return;

            remainingUses = Mathf.Max(0, remainingUses - 1);
        }

        public bool IsBroken => itemData != null && itemData.uses > 0 && remainingUses <= 0;
    }
}
