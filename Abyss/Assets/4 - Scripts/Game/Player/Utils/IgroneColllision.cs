using UnityEngine;

public class IgroneColllision : MonoBehaviour
{
    [SerializeField]
    Collider thisCollider;
    [SerializeField]
    Collider[] ignoreColliders;
    void Awake()
    {
        foreach(Collider col in ignoreColliders)
        {
            Physics.IgnoreCollision(thisCollider, col,true);
        }
    }
}
