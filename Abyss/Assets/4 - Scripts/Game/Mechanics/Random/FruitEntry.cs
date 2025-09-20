using UnityEngine;

namespace Game.Mechanics.Random
{
    [System.Serializable]
    public struct FruitEntry
    {
        public GameObject prefab;
        public float weight; // 0 = not in pool
    }
}