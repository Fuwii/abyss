using UnityEngine;

public class GroundChecker : MonoBehaviour
{
    public Transform groundCheck;
    public LayerMask groundMask;
    public float groundCheckDistance = 0.12f;
    public float groundCheckRadius = 0.25f;
    [Range(0f, 1f)] public float minGroundNormalY = 0.65f;

    public void Setup(Transform check, LayerMask mask, float dist, float radius, float minNormalY)
    {
        groundCheck = check;
        groundMask = mask;
        groundCheckDistance = dist;
        groundCheckRadius = radius;
        minGroundNormalY = minNormalY;
    }

    public void CheckGround(out bool isGrounded, out Vector3 groundNormal)
    {
        isGrounded = false;
        groundNormal = Vector3.up;

        if (groundCheck == null)
        {
            Debug.LogWarning("Ground check transform is missing!");
            return;
        }

        if (Physics.SphereCast(groundCheck.position + Vector3.up * 0.1f, groundCheckRadius, Vector3.down,
            out RaycastHit hit, groundCheckDistance + 0.1f, groundMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.normal.y >= minGroundNormalY)
            {
                isGrounded = true;
                groundNormal = hit.normal;
                return;
            }
        }

        // Fallback ray
        Ray ray = new Ray(groundCheck.position + Vector3.up * 0.01f, Vector3.down);
        if (Physics.Raycast(ray, out hit, groundCheckDistance + 0.01f, groundMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.normal.y >= minGroundNormalY)
            {
                isGrounded = true;
                groundNormal = hit.normal;
            }
        }
    }
}
