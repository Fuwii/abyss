using UnityEngine;

public class SyncPhysicsObject : MonoBehaviour
{
    Rigidbody rb;
    ConfigurableJoint joint;
    [SerializeField]
    Rigidbody animatedRb;
    [SerializeField]
    bool isSyncing = false;
    Quaternion startLocalRotation;
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        joint = GetComponent<ConfigurableJoint>();
        startLocalRotation = transform.localRotation;
    }
    public void UpdateJointFromAnimation()
    {
        if (!isSyncing)
        {
            return;
        }
        ConfigurableJointExtensions.SetTargetRotationLocal(joint,animatedRb.transform.localRotation,startLocalRotation);
    }
}
