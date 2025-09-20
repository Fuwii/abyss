using System.Collections.Generic;
using UnityEngine;

namespace Game.Mechanics.Random.Tree
{
    public class TreeRandomizable : RandomizableComponent
    {
        public override RandomCategory Category => RandomCategory.Tree;

        [Header("Spawn Positions")]
        public List<UnityEngine.Transform> fruitPositions = new();

        [Header("Spawn rules")]
        [Range(0f, 1f)] public float fruitChance = 0.5f;
        public int minFruit;
        public int maxFruit = 3;

        [Header("Fruits to spawn")]
        public FruitEntry[] fruitTable;
    }
}
