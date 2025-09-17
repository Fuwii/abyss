using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct FruitEntry
{
    public GameObject prefab;
    public float weight; // 0 = not in pool
}

public class TreeRandomizable : RandomizableComponent
{
    public override RandomCategory Category => RandomCategory.Tree;

    [Header("Spawn Positions")]
    public List<Transform> fruitPositions = new List<Transform>();

    [Header("Spawn rules")]
    [Range(0f, 1f)] public float fruitChance = 0.5f;
    public int minFruit = 0;
    public int maxFruit = 3;

    [Header("Fruits to spawn")]
    public FruitEntry[] fruitTable;
}
