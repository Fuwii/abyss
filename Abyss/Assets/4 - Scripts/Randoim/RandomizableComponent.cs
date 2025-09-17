using UnityEngine;

public abstract class RandomizableComponent : MonoBehaviour
{
    [Tooltip("Dont change it for random")]
    public long persistentId = 0;

    public abstract RandomCategory Category { get; }
}
