using Game.Mechanics.Random.Seed;
using UnityEngine;

namespace Game.Mechanics.Random.Loot
{
    [DisallowMultipleComponent]
    public class LootRandomizer : MonoBehaviour, IRandomizer
    {
        public RandomCategory Category => RandomCategory.LootPosition;

        public void Randomize(RandomizableComponent comp, ref SeededRandom rng, ulong worldSeed, RandomMode mode)
        {
            var pos = comp as LootPositionRandomizable;
            if (pos == null || pos.allowedTables == null || pos.allowedTables.Length == 0)
                return;

            var localRng = rng;
            if (mode == RandomMode.Independent)
            {
                var seed = SeedUtils.CombineSeed(worldSeed, pos.persistentId, "loot_spawn");
                localRng = new SeededRandom(seed);
            }

            // �������� �������
            var table = pos.allowedTables[localRng.NextInt(0, pos.allowedTables.Length)];
            if (table == null) return;

            // �������� �������
            var prefab = table.PickItem(ref localRng);
            if (prefab == null) return;

            Object.Instantiate(prefab, pos.transform.position, pos.transform.rotation, pos.transform);
        }
    }
}