using System.Collections.Generic;
using UnityEngine;

public class NormalSampler
{
    private Vector3 smoothedWallNormal = Vector3.forward;
    private float normalSmoothSpeed = 30f;
    private float maxNormalAnglePerSec = 300f;
    private float edgeAngleThreshold = 45f;

    public Vector3 SampleAndSmooth(List<RaycastHit> allHits, RaycastHit primaryHit, ClimbingContext ctx, ClimbConfig config, out Vector3 lateralDir, out bool edgeDetected)
    {
        lateralDir = Vector3.zero;
        edgeDetected = false;

        var sampledNormals = BuildUniqueNormals(allHits, primaryHit);

        Vector3 sampledNormal;
        if (sampledNormals.Count == 1)
        {
            sampledNormal = sampledNormals[0];
        }
        else
        {
            sampledNormal = ResolveMultipleNormals(sampledNormals, ctx, out edgeDetected, out lateralDir);
        }

        float maxDeltaRad = Mathf.Deg2Rad * (maxNormalAnglePerSec * Time.fixedDeltaTime);
        smoothedWallNormal = Vector3.RotateTowards(smoothedWallNormal, sampledNormal, maxDeltaRad, 0f);
        smoothedWallNormal = Vector3.Slerp(smoothedWallNormal, sampledNormal, 1f - Mathf.Exp(-normalSmoothSpeed * Time.fixedDeltaTime));

        return smoothedWallNormal;
    }

    private List<Vector3> BuildUniqueNormals(List<RaycastHit> allHits, RaycastHit primaryHit)
    {
        var sampledNormals = new List<Vector3>();
        foreach (var h in allHits)
        {
            Vector3 nrm = h.normal.normalized;
            bool similar = false;
            foreach (var ex in sampledNormals)
            {
                if (Vector3.Angle(ex, nrm) < 8f) { similar = true; break; }
            }
            if (!similar) sampledNormals.Add(nrm);
        }
        if (sampledNormals.Count == 0) sampledNormals.Add(primaryHit.normal.normalized);
        return sampledNormals;
    }

    private Vector3 ResolveMultipleNormals(List<Vector3> sampledNormals, ClimbingContext ctx, out bool edgeDetected, out Vector3 lateralDir)
    {
        edgeDetected = false;
        lateralDir = Vector3.zero;

        int a = 0, b = 1;
        float bestAng = 0f;
        for (int i = 0; i < sampledNormals.Count; i++)
            for (int j = i + 1; j < sampledNormals.Count; j++)
            {
                float ang = Vector3.Angle(sampledNormals[i], sampledNormals[j]);
                if (ang > bestAng) { bestAng = ang; a = i; b = j; }
            }

        if (bestAng >= edgeAngleThreshold)
        {
            edgeDetected = true;
            Vector3 nA = sampledNormals[a];
            Vector3 nB = sampledNormals[b];
            Vector3 sampledNormal = (nA + nB).normalized;

            Vector3 edge = Vector3.Cross(nA, nB);
            if (edge.sqrMagnitude > 1e-6f)
            {
                Vector3 edgeHoriz = Vector3.ProjectOnPlane(edge, Vector3.up);
                if (edgeHoriz.sqrMagnitude > 1e-6f)
                {
                    lateralDir = edgeHoriz.normalized;
                }
                else
                {
                    Vector3 tmpWallUp = Vector3.ProjectOnPlane(Vector3.up, sampledNormal);
                    if (tmpWallUp.sqrMagnitude < 1e-6f)
                        tmpWallUp = Vector3.Cross(sampledNormal, ctx.Transform.right);
                    tmpWallUp.Normalize();

                    lateralDir = Vector3.Cross(tmpWallUp, sampledNormal);
                    if (lateralDir.sqrMagnitude < 1e-6f)
                        lateralDir = Vector3.Cross(sampledNormal, ctx.Transform.right);
                    lateralDir.Normalize();
                }
            }
            else
            {
                Vector3 tmpWallUp = Vector3.ProjectOnPlane(Vector3.up, sampledNormal);
                if (tmpWallUp.sqrMagnitude < 1e-6f)
                    tmpWallUp = Vector3.Cross(sampledNormal, ctx.Transform.right);
                tmpWallUp.Normalize();

                lateralDir = Vector3.Cross(tmpWallUp, sampledNormal);
                if (lateralDir.sqrMagnitude < 1e-6f)
                    lateralDir = Vector3.Cross(sampledNormal, ctx.Transform.right);
                lateralDir.Normalize();
            }

            return sampledNormal;
        }

        // average
        Vector3 acc = Vector3.zero;
        foreach (var n in sampledNormals) acc += n;
        return acc.normalized;
    }
}
