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

        private readonly Queue<Vector3> _recentNormals = new Queue<Vector3>();
        private int _historySize = 4;        
        private float _clusterAngleDeg = 12f;  

        public Vector3 SampleAndSmooth(List<RaycastHit> allHits, RaycastHit primaryHit, ClimbingContext ctx, ClimbConfig config, out Vector3 lateralDir, out bool edgeDetected)
        {
            lateralDir = Vector3.zero;
            edgeDetected = false;

            Vector3 localAvg = ComputeLocalAveragedNormal(allHits, primaryHit, posRadius: 0.12f);

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

            sampledNormal = Vector3.Slerp(sampledNormal, localAvg, 1f);

            var stableFromHistory = ComputeStableNormalFromHistory(sampledNormal, _smoothedWallNormal);

            var maxDeltaRad = Mathf.Deg2Rad * (MaxNormalAnglePerSec * Time.fixedDeltaTime);
            _smoothedWallNormal = Vector3.RotateTowards(_smoothedWallNormal, stableFromHistory, maxDeltaRad, 0f);
            _smoothedWallNormal = Vector3.Slerp(_smoothedWallNormal, stableFromHistory, 1f - Mathf.Exp(-NormalSmoothSpeed * Time.fixedDeltaTime));

            return _smoothedWallNormal;
        }

        private Vector3 ComputeLocalAveragedNormal(List<RaycastHit> allHits, RaycastHit primaryHit, float posRadius = 0.15f)
        {
            if (allHits == null || allHits.Count == 0) return primaryHit.normal.normalized;

            Vector3 acc = Vector3.zero;
            float wsum = 0f;
            float r2 = posRadius * posRadius;
            foreach (var h in allHits)
            {
                if ((h.point - primaryHit.point).sqrMagnitude <= r2)
                {
                    float w = 1f / Mathf.Max(0.001f, h.distance);
                    acc += h.normal.normalized * w;
                    wsum += w;
                }
            }

            if (wsum <= 1e-6f) return primaryHit.normal.normalized;
            return (acc / wsum).normalized;
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

        //cluster
        private Vector3 ComputeStableNormalFromHistory(Vector3 newSample, Vector3 previousSmoothed)
        {
            _recentNormals.Enqueue(newSample);
            if (_recentNormals.Count > _historySize) _recentNormals.Dequeue();

            var groups = new List<(Vector3 sum, int count)>();
            foreach (var n in _recentNormals)
            {
                bool placed = false;
                for (int i = 0; i < groups.Count; i++)
                {
                    var avg = groups[i].sum / groups[i].count;
                    if (Vector3.Angle(avg, n) <= _clusterAngleDeg)
                    {
                        groups[i] = (groups[i].sum + n, groups[i].count + 1);
                        placed = true;
                        break;
                    }
                }

                if (!placed)
                {
                    groups.Add((n, 1));
                }
            }
            float bestScore = float.MinValue;
            Vector3 bestAvg = newSample;
            for (int i = 0; i < groups.Count; i++)
            {
                var g = groups[i];
                var avg = (g.sum / g.count).normalized;
                float score = g.count * 100f - Vector3.Angle(avg, previousSmoothed);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestAvg = avg;
                }
            }

            return bestAvg.normalized;
        }
    }
}
