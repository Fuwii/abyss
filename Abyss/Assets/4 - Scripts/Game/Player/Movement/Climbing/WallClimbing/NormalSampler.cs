using System.Collections.Generic;
using UnityEngine;

namespace Game.Player.Movement.Climbing.WallClimbing
{
    public class NormalSampler
    {
        // Tunables � ���������� ��� ���� ����
        private const float NormalSmoothSpeed = 20f;        // ���������������� "��������" �����������
        private const float MaxNormalAnglePerSec = 400f;   // ���� �������� �������� ��������/���
        private const float EdgeAngleThreshold = 45f;      // ����, ����������� "������"

        // ��������� / ����������
        private const float SupportAngleDeg = 12f;         // ����, ������ �������� ��� ��������� ���������� primary
        private const float SupportWeightFactor = 1.05f;   // ��������� ������� ������ ���� ��������� primary, ��� ���������
        private const float SwitchAngleDeg = 18f;          // ���� �������� ���������� ������� � ����� �����
        private const int SwitchFrames = 3;                // ���������� FixedUpdate ������ ��� ������������

        private Vector3 _smoothedWallNormal = Vector3.forward;
        private Vector3 _acceptedNormal = Vector3.forward; // "currently accepted" normal (���������� ��������� �� ��)
        private Vector3 _pendingNormal = Vector3.zero;
        private int _pendingFrames = 0;

        // ������� ������������� (����� ������������ ��� �������������� ������������, �������)
        private readonly Queue<Vector3> _recentNormals = new Queue<Vector3>();
        private int _historySize = 6;
        private float _clusterAngleDeg = 14f;

        public Vector3 SampleAndSmooth(List<RaycastHit> allHits, RaycastHit primaryHit, ClimbingContext ctx, ClimbConfig config, out Vector3 lateralDir, out bool edgeDetected)
        {
            lateralDir = Vector3.zero;
            edgeDetected = false;

            // ������
            if (allHits == null || allHits.Count == 0)
            {
                // fallback � ���������� primary �����
                _acceptedNormal = primaryHit.normal.normalized;
                _smoothedWallNormal = _acceptedNormal;
                return _smoothedWallNormal;
            }

            var primaryNormal = primaryHit.normal.normalized;
            var localAvg = ComputeLocalAveragedNormal(allHits, primaryHit, posRadius: 0.14f);

            // ������ ���������� ������� (��� edge detection)
            var sampledNormals = BuildUniqueNormals(allHits, primaryHit);

            // ���������� ���������� edge (��� ������)
            Vector3 edgeLateral;
            var resolvedFromMulti = ResolveMultipleNormals(sampledNormals, ctx, out edgeDetected, out edgeLateral);
            if (edgeDetected)
            {
                lateralDir = edgeLateral;
            }

            // ������� ���� ��������� primary � "���������"
            float primaryWeight = 0f;
            float othersWeight = 0f;
            foreach (var h in allHits)
            {
                float w = 1f / Mathf.Max(0.001f, h.distance); // ����� = �������
                var ang = Vector3.Angle(h.normal.normalized, primaryNormal);
                if (ang <= SupportAngleDeg)
                    primaryWeight += w;
                else
                    othersWeight += w;
            }

            // �������� ���������: ���� primary ��������� ���������� > others * factor -> candidate = primary
            Vector3 candidate;
            if (primaryWeight >= othersWeight * SupportWeightFactor)
            {
                candidate = primaryNormal;
            }
            else
            {
                // ���� ���������� ����� � ����� ������������ ����������� �������, ����� ��������� average
                if (edgeDetected)
                    candidate = resolvedFromMulti.normalized;
                else
                    candidate = localAvg.normalized;
            }

            // ���� �������� ������ � ������� �������� ������� � ��������� �����
            var angleToAccepted = Vector3.Angle(_acceptedNormal, candidate);
            if (angleToAccepted <= SwitchAngleDeg)
            {
                _acceptedNormal = candidate;
                _pendingFrames = 0;
                _pendingNormal = Vector3.zero;
            }
            else
            {
                // �������� ������ � ������� ������������ SwitchFrames
                if (_pendingNormal == Vector3.zero || Vector3.Angle(_pendingNormal, candidate) > 6f)
                {
                    // ����� pending
                    _pendingNormal = candidate;
                    _pendingFrames = 1;
                }
                else
                {
                    _pendingFrames++;
                }

                if (_pendingFrames >= SwitchFrames)
                {
                    // �������������
                    _acceptedNormal = _pendingNormal;
                    _pendingNormal = Vector3.zero;
                    _pendingFrames = 0;
                }
                // ����� � ���� ������ ������ _acceptedNormal
            }

            // ��������� ������� (����� ������������ �����)
            _recentNormals.Enqueue(candidate);
            if (_recentNormals.Count > _historySize) _recentNormals.Dequeue();

            // ������ ������������ _smoothedWallNormal � _acceptedNormal
            var maxDeltaRad = Mathf.Deg2Rad * (MaxNormalAnglePerSec * Time.fixedDeltaTime);
            _smoothedWallNormal = Vector3.RotateTowards(_smoothedWallNormal, _acceptedNormal, maxDeltaRad, 0f);

            // ���������������� ����������� (����� ������, ��� RotateTowards ��� �� ����)
            float alpha = 1f - Mathf.Exp(-NormalSmoothSpeed * Time.fixedDeltaTime);
            _smoothedWallNormal = Vector3.Slerp(_smoothedWallNormal, _acceptedNormal, alpha).normalized;

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
                for (var i = 0; i < sampledNormals.Count; i++)
                {
                    if (Vector3.Angle(sampledNormals[i], nrm) < 8f)
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

            if (sampledNormals == null || sampledNormals.Count == 0)
                return Vector3.forward;

            if (sampledNormals.Count == 1)
                return sampledNormals[0];

            // find two most divergent normals
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
                Vector3 edgeHoriz = Vector3.ProjectOnPlane(edge, Vector3.up);
                if (edgeHoriz.sqrMagnitude > 1e-6f)
                {
                    lateralDir = edgeHoriz.normalized;
                }
                else
                {
                    lateralDir = Vector3.Cross(sampledNormal, Vector3.up);
                    if (lateralDir.sqrMagnitude < 1e-6f)
                        lateralDir = Vector3.Cross(Vector3.up, sampledNormal); 
                    lateralDir = Vector3.ProjectOnPlane(lateralDir, sampledNormal).normalized;
                }

                if (lateralDir.sqrMagnitude < 1e-6f)
                {
                    lateralDir = Vector3.Cross(sampledNormal, Vector3.up).normalized;
                }

                return sampledNormal;

            }

            // otherwise average
            var acc = Vector3.zero;
            foreach (var n in sampledNormals) acc += n;
            return acc.normalized;
        }
    }
}
