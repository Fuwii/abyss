using UnityEngine;

public static class Utilities
{
    // Maps a square input [-1,1]^2 -> circle preserving magnitude direction (simple)
    public static Vector2 SquareToCircle(Vector2 input)
    {
        if (input.sqrMagnitude >= 1f) return input.normalized;
        return input;
    }

    // Safe approx equals for vectors
    public static bool IsNearlyZero(Vector3 v, float eps = 1e-4f) =>
        v.sqrMagnitude <= eps * eps;
}
