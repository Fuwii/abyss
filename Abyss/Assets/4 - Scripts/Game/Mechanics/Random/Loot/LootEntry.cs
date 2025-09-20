using UnityEngine;

namespace Game.Mechanics.Random.Loot
{
    [System.Serializable]
    public struct LootEntry
    {
        public GameObject prefab;
        public float weight;
    }
}