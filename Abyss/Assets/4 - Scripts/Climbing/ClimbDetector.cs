using UnityEngine;

public static class ClimbDetector
{
    // Try a single raycast
    public static bool TryRay(Vector3 origin, Vector3 direction, float distance, LayerMask mask, out RaycastHit hit)
    {
        return Physics.Raycast(origin, direction, out hit, distance, mask);
    }

    // Returns averaged wall normal or Vector3.zero if none
    public static Vector3 GetWallNormal(Transform body, Transform cameraTransform, float climbDetectionDistance, LayerMask climbableLayers)
    {
        Vector3 forward = body.forward;

        if (TryRay(body.position, forward, climbDetectionDistance, climbableLayers, out RaycastHit hit))
            return hit.normal;

        Vector3 elevated = body.position + Vector3.up * 0.5f;
        if (TryRay(elevated, forward, climbDetectionDistance, climbableLayers, out hit))
            return hit.normal;

        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        Vector3 up = Vector3.Cross(right, forward).normalized;
        Vector3[] dirs = new Vector3[]
        {
            forward + right * 0.3f,
            forward - right * 0.3f,
            forward + up * 0.3f,
            forward - up * 0.3f
        };

        Vector3 total = Vector3.zero;
        int count = 0;
        foreach (var d in dirs)
        {
            if (TryRay(body.position, d.normalized, climbDetectionDistance, climbableLayers, out hit))
            {
                total += hit.normal;
                count++;
            }
        }

        return count > 0 ? (total / count).normalized : Vector3.zero;
    }

    // Decide if the player can start climbing based on forward ray and camera fallback
    public static bool CanClimb(Transform body, Transform cameraTransform, float climbDetectionDistance, LayerMask climbableLayers, float minWallAngleDeg, float lookAlignmentThreshold)
    {
        Vector3 dir = body.forward;

        if (TryRay(body.position, dir, climbDetectionDistance, climbableLayers, out RaycastHit hit))
        {
            float angle = Vector3.Angle(hit.normal, Vector3.up);
            float lookAlignment = Vector3.Dot(-hit.normal, dir);
            return angle >= minWallAngleDeg && lookAlignment > lookAlignmentThreshold;
        }

        if (cameraTransform != null && TryRay(cameraTransform.position, dir, climbDetectionDistance, climbableLayers, out hit))
        {
            float angle = Vector3.Angle(hit.normal, Vector3.up);
            float lookAlignment = Vector3.Dot(-hit.normal, dir);
            return angle >= minWallAngleDeg && lookAlignment > lookAlignmentThreshold;
        }

        return false;
    }
}
