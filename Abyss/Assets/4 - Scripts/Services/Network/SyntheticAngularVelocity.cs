using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

[DefaultExecutionOrder(9999)]
public class SyntheticAngularVelocity : NetworkBehaviour
{
    private Rigidbody _rb;
    private Quaternion _prevRotation;
    private NetworkRigidbody _netRB;

    public override void OnNetworkSpawn()
    {
        _rb = GetComponent<Rigidbody>();
        _netRB = GetComponent<NetworkRigidbody>();
        _prevRotation = transform.rotation;

        if (!IsOwner)
        {
            if (_netRB != null) _netRB.enabled = false;
            _rb.freezeRotation = false;
            _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }
    }

    void LateUpdate()
    {
        if (IsOwner) return;

        Quaternion currentRot = transform.rotation;
        Quaternion deltaRot = currentRot * Quaternion.Inverse(_prevRotation);
        _prevRotation = currentRot;

        deltaRot.ToAngleAxis(out float angle, out Vector3 axis);
        if (angle > 180f) angle -= 360f;

        if (Mathf.Abs(angle) > 0.01f && Time.deltaTime > 0)
        {
            Vector3 calculatedVel = axis * (angle * Mathf.Deg2Rad) / Time.deltaTime;
            _rb.angularVelocity = calculatedVel;

        }
        else
        {
            _rb.angularVelocity = Vector3.zero;
        }
    }
}