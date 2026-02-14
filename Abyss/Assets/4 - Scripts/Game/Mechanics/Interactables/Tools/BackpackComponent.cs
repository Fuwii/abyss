using Game.Mechanics.Interactables.Tools;
using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[Serializable]
public class BackpackComponent : IItemComponent
{
    public int capacity;
    public GameObject uiPrefab;
    public GameObject activeUIInstance;

    public BackpackComponent(int capacity, GameObject uiPrefab)
    {
        this.capacity = capacity;
        this.uiPrefab = uiPrefab;
    }
}
