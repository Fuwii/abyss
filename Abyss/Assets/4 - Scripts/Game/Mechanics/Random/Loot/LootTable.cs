using Game.Mechanics.Random.Seed;
using UnityEngine;

namespace Game.Mechanics.Random.Loot
{
    [CreateAssetMenu(menuName = "Loot/LootTable")]
    public class LootTable : ScriptableObject
    {
        public LootEntry[] entries;

        public GameObject PickItem(ref SeededRandom rng)
        {
            if (entries == null || entries.Length == 0) return null;

            var total = 0f;
            foreach (var e in entries) total += Mathf.Max(0, e.weight);
            if (total <= 0f) return null;

            var r = (float)rng.NextDouble() * total;
            var acc = 0f;
            foreach (var e in entries)
            {
                acc += Mathf.Max(0, e.weight);
                if (r <= acc)
                    return e.prefab;
            }
            return entries[entries.Length - 1].prefab;
        }
    }
}
