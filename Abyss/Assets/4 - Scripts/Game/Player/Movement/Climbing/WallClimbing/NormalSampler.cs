using System.Collections.Generic;
using UnityEngine;

namespace Game.Player.Movement.Climbing.WallClimbing
{
    public class NormalSampler
    {
        private const float NormalSmoothSpeed = 30f;
        private const float MaxNormalAnglePerSec = 300f;
        private const float EdgeAngleThreshold = 45f;

        private Vector3 _smoothedWallNormal = Vector3.forward;

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

            var maxDeltaRad = Mathf.Deg2Rad * (MaxNormalAnglePerSec * Time.fixedDeltaTime);
            _smoothedWallNormal = Vector3.RotateTowards(_smoothedWallNormal, sampledNormal, maxDeltaRad, 0f);
            _smoothedWallNormal = Vector3.Slerp(_smoothedWallNormal, sampledNormal, 1f - Mathf.Exp(-NormalSmoothSpeed * Time.fixedDeltaTime));

            return _smoothedWallNormal;
        }

        private List<Vector3> BuildUniqueNormals(List<RaycastHit> allHits, RaycastHit primaryHit)
        {
            var sampledNormals = new List<Vector3>();
            foreach (var h in allHits)
            {
                var nrm = h.normal.normalized;
                var similar = false;
                foreach (var ex in sampledNormals)
                {
                    if (Vector3.Angle(ex, nrm) < 8f)
                    {
                        similar = true;
                        break;
                    }
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
            var bestAng = 0f;
            for (var i = 0; i < sampledNormals.Count; i++)
            for (var j = i + 1; j < sampledNormals.Count; j++)
            {
                var ang = Vector3.Angle(sampledNormals[i], sampledNormals[j]);
                if (ang > bestAng)
                {
                    bestAng = ang;
                    a = i;
                    b = j;
                }
            }

            if (bestAng >= EdgeAngleThreshold)
            {
                edgeDetected = true;
                var nA = sampledNormals[a];
                var nB = sampledNormals[b];
                var sampledNormal = (nA + nB).normalized;

                var edge = Vector3.Cross(nA, nB);
                if (edge.sqrMagnitude > 1e-6f)
                {
                    var edgeHoriz = Vector3.ProjectOnPlane(edge, Vector3.up);
                    if (edgeHoriz.sqrMagnitude > 1e-6f)
                    {
                        lateralDir = edgeHoriz.normalized;
                    }
                    else
                    {
                        var tmpWallUp = Vector3.ProjectOnPlane(Vector3.up, sampledNormal);
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
                    var tmpWallUp = Vector3.ProjectOnPlane(Vector3.up, sampledNormal);
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
            var acc = Vector3.zero;
            foreach (var n in sampledNormals) acc += n;
            return acc.normalized;
        }
    }
}