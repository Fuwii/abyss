using UnityEngine;

namespace Game.Mechanics.Random
{
    public abstract class RandomizableComponent : MonoBehaviour
    {
        [Tooltip("Dont change it for random")]
        public long persistentId;

        public abstract RandomCategory Category { get; }
    }
}