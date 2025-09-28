using Game.Mechanics.Random.Seed;
using UnityEngine;

namespace Game.Mechanics.Random.Tree
{
    [DisallowMultipleComponent]
    public class TreeRandomizer : MonoBehaviour, IRandomizer
    {
        public RandomCategory Category => RandomCategory.Tree;

        public void Randomize(RandomizableComponent comp, ref SeededRandom rng, ulong worldSeed, RandomMode mode)
        {
            var tree = comp as TreeRandomizable;
            if (tree == null) return;

            var localRng = rng;
            if (mode == RandomMode.Independent)
            {
                var seed = SeedUtils.CombineSeed(worldSeed, tree.persistentId, "tree_main");
                localRng = new SeededRandom(seed);
            }

            if (localRng.NextDouble() >= tree.fruitChance) return;

            var count = localRng.NextInt(tree.minFruit, tree.maxFruit + 1);
            var shuffled = localRng.ShuffleIndices(tree.fruitPositions.Count);

            for (var i = 0; i < Mathf.Min(count, shuffled.Count); i++)
            {
                var posIndex = shuffled[i];
                var pos = tree.fruitPositions[posIndex];

                var prefab = PickFromFruitTable(tree, ref localRng);
                if (prefab != null)
                    Object.Instantiate(prefab, pos.position, Quaternion.identity, pos);
            }
        }

        GameObject PickFromFruitTable(TreeRandomizable tree, ref SeededRandom rng)
        {
            var table = tree.fruitTable;

            if (table == null || table.Length == 0)
                return null;

            var total = 0f;

            foreach (var e in table)
            {
                total += e.weight;
            }

            if (total <= 0f)
                return table[0].prefab;

            var r = (float)rng.NextDouble() * total;
            var acc = 0f;

            foreach (var e in table)
            {
                acc += e.weight;
                if (r <= acc) return e.prefab;
            }

            return table[^1].prefab;
        }
    }
}