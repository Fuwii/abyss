using UnityEngine;

public class LookHandler : MonoBehaviour
{
    private Transform body;
    private Camera cam;
    private float sensitivity;
    private float pitchMin, pitchMax;

    public void Setup(Transform bodyTransform, Camera playerCamera, float mouseSens, float pMin, float pMax)
    {
        body = bodyTransform;
        cam = playerCamera;
        sensitivity = mouseSens;
        pitchMin = pMin;
        pitchMax = pMax;
    }
    public void ApplyLook(Vector2 lookDelta, ref float yaw, ref float pitch, FpsPlayerClimbing climbingSystem)
    {
        yaw += lookDelta.x * sensitivity;
        pitch += -lookDelta.y * sensitivity;
        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);

        if (climbingSystem != null && climbingSystem.state == FpsPlayerClimbing.PlayerState.CLIMBING)
        {
            if (cam) cam.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }
        else
        {
            if (body) body.rotation = Quaternion.Euler(0f, yaw, 0f);
            if (cam) cam.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }
    }
}
