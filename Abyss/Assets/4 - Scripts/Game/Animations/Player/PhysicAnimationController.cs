using UnityEngine;

public class PhysicAnimationController : MonoBehaviour
{
    SyncPhysicsObject[] syncPhysicsObjects;
    void Awake()
    {
        syncPhysicsObjects = GetComponentsInChildren<SyncPhysicsObject>();
    }

    void LateUpdate()
    {
        foreach(SyncPhysicsObject obj in syncPhysicsObjects)
        {
            obj.UpdateJointFromAnimation();
        }
    }
}
