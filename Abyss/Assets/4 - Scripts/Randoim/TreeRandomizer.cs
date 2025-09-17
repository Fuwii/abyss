using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class TreeRandomizer : MonoBehaviour, IRandomizer
{
    public RandomCategory Category => RandomCategory.Tree;

    public void Randomize(RandomizableComponent comp, ref SeededRandom rng, long worldSeed, RandomMode mode)
    {
        var tree = comp as TreeRandomizable;
        if (tree == null) return;

        SeededRandom localRng = rng;
        if (mode == RandomMode.Independent)
        {
            ulong seed = SeedUtils.CombineSeed(worldSeed, tree.persistentId, "tree_main");
            localRng = new SeededRandom(seed);
        }

        if (localRng.NextDouble() >= tree.fruitChance) return;

        int count = localRng.NextInt(tree.minFruit, tree.maxFruit + 1);
        var shuffled = localRng.ShuffleIndices(tree.fruitPositions.Count);

        for (int i = 0; i < Mathf.Min(count, shuffled.Count); i++)
        {
            int posIndex = shuffled[i];
            var pos = tree.fruitPositions[posIndex];

            GameObject prefab = PickFromFruitTable(tree, ref localRng);
            if (prefab != null)
                Object.Instantiate(prefab, pos.position, Quaternion.identity, pos);
        }
    }

    GameObject PickFromFruitTable(TreeRandomizable tree, ref SeededRandom rng)
    {
        var table = tree.fruitTable;
        if (table == null || table.Length == 0) return null;
        float total = 0f;
        foreach (var e in table) total += e.weight;
        if (total <= 0f) return table[0].prefab;
        float r = (float)rng.NextDouble() * total;
        float acc = 0f;
        foreach (var e in table)
        {
            acc += e.weight;
            if (r <= acc) return e.prefab;
        }
        return table[table.Length - 1].prefab;
    }
}
