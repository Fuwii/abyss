using System.Collections.Generic;
using UnityEngine;

namespace Game.Player.Movement.Climbing.WallClimbing
{
    public class ClimbRaycaster
    {
        public bool FindClimbSurface3Cast(ClimbingContext ctx, out RaycastHit bestHit, out List<RaycastHit> allHits)
        {
            var hits = new List<RaycastHit>();
            bestHit = default;
            allHits = null;

            var minDetectionDist = 1f;
            var maxDist = Mathf.Max(ctx.WallStickDistance, minDetectionDist);
            var center = ctx.Transform.position;
            float playerHeight = ctx.PlayerHeight > 0f ? ctx.PlayerHeight : 1.6f;

            Vector3 neckPos = ctx.Transform.position; 
            Vector3 feetPos = neckPos + Vector3.down * playerHeight*1.2f;
            Vector3 torsoPos = neckPos + Vector3.down * (playerHeight * 0.5f);

            var forward = ctx.Transform.forward;
            var inputDir = ctx.Transform.forward * ctx.MoveInput.y + ctx.Transform.right * ctx.MoveInput.x;
            if (inputDir.sqrMagnitude < 0.001f) inputDir = forward;
            inputDir = inputDir.normalized;

            // ������ ���� (�������� �����/������)
            float bodyHalfWidth = 0.25f;
            if (ctx.Rigidbody != null)
                bodyHalfWidth = Mathf.Max(0.12f, ctx.Rigidbody.transform.localScale.x * 0.5f);
            float lateralOffset = bodyHalfWidth * 0.9f;

            void AddUniqueHit(RaycastHit hhit)
            {
                const float posEps = 0.025f;
                const float normalDistinctAngle = 20f; // ���� normals ���������� ������� � ������� ������ �����

                for (int i = 0; i < hits.Count; i++)
                {
                    var ex = hits[i];
                    if (ex.collider == hhit.collider && (ex.point - hhit.point).sqrMagnitude <= posEps * posEps)
                    {
                        // ���� ������� ����� ��������� � ��������� �������
                        var angle = Vector3.Angle(ex.normal, hhit.normal);
                        if (angle < normalDistinctAngle)
                        {
                            if (hhit.distance < ex.distance) hits[i] = hhit;
                            return;
                        }
                        // ���� ������� ���������� ����������� � ������� ��� ������ "������" � �� ����������
                    }
                }
                hits.Add(hhit);
            }


            void TripleCastTowardsSamePoint_Strict(Vector3 origin)
            {
                // ����������� �����
                if (Physics.Raycast(origin, forward, out var centerHit, maxDist, ctx.ClimbableLayers))
                {
                    AddUniqueHit(centerHit);
                    Debug.DrawRay(origin, (centerHit.point - origin), Color.green, 0.18f);

                    var leftOrigin = origin - ctx.Transform.right * lateralOffset;
                    var rightOrigin = origin + ctx.Transform.right * lateralOffset;

                    float sideAimOffset = Mathf.Clamp(bodyHalfWidth * 0.25f, 0.06f, bodyHalfWidth * 0.6f);
                    var dirToCenter = (centerHit.point - origin).normalized;
                    var inwardOffset = 0.01f;
                    float closerFactor = 0.25f;

                    var rawRightTarget = centerHit.point + ctx.Transform.right * sideAimOffset;
                    var rawLeftTarget = centerHit.point - ctx.Transform.right * sideAimOffset;

                    var rightTarget = Vector3.Lerp(rawRightTarget, centerHit.point, closerFactor) + dirToCenter * inwardOffset;
                    var leftTarget = Vector3.Lerp(rawLeftTarget, centerHit.point, closerFactor) + dirToCenter * inwardOffset;

                    Debug.DrawLine(centerHit.point, leftTarget, Color.magenta, 0.18f);
                    Debug.DrawLine(centerHit.point, rightTarget, Color.magenta, 0.18f);

                    // ����� ��� ����, ����� ������� ��� �������� "������ ������"
                    const float sideUseAngleThreshold = 45f; // <- ��������: 30..45 �������� ������ �������

                    // ����� ��������
                    var dirLeft = leftTarget - leftOrigin;
                    if (dirLeft.sqrMagnitude > 1e-6f)
                    {
                        var dirLeftN = dirLeft.normalized;
                        var distLeft = Mathf.Min(maxDist * 1.5f, dirLeft.magnitude + 0.02f);

                        if (Physics.Raycast(leftOrigin, dirLeftN, out var leftHit, distLeft, ctx.ClimbableLayers))
                        {
                            // ��������� ����� ��� ������ ���� ��� normal ����������� ���������� �� ������������
                            var ang = Vector3.Angle(leftHit.normal, centerHit.normal);
                            if (ang >= sideUseAngleThreshold)
                            {
                                AddUniqueHit(leftHit);
                                Debug.DrawRay(leftOrigin, leftHit.point - leftOrigin, Color.green, 0.18f);
                            }
                            else
                            {
                                // �� ������ ����� � �� ������, �� ���������� ����� ���
                                Debug.DrawRay(leftOrigin, dirLeftN * distLeft, Color.gray, 0.18f);
                            }
                        }
                        else
                        {
                            Debug.DrawRay(leftOrigin, dirLeftN * distLeft, Color.gray, 0.18f);
                        }
                    }

                    // ������ ��������
                    var dirRight = rightTarget - rightOrigin;
                    if (dirRight.sqrMagnitude > 1e-6f)
                    {
                        var dirRightN = dirRight.normalized;
                        var distRight = Mathf.Min(maxDist * 1.5f, dirRight.magnitude + 0.02f);

                        if (Physics.Raycast(rightOrigin, dirRightN, out var rightHit, distRight, ctx.ClimbableLayers))
                        {
                            var ang = Vector3.Angle(rightHit.normal, centerHit.normal);
                            if (ang >= sideUseAngleThreshold)
                            {
                                AddUniqueHit(rightHit);
                                Debug.DrawRay(rightOrigin, rightHit.point - rightOrigin, Color.green, 0.18f);
                            }
                            else
                            {
                                Debug.DrawRay(rightOrigin, dirRightN * distRight, Color.gray, 0.18f);
                            }
                        }
                        else
                        {
                            Debug.DrawRay(rightOrigin, dirRightN * distRight, Color.gray, 0.18f);
                        }
                    }
                }
            }


            // ��������� ������� ��������� ��� �������� � ��� ���
            TripleCastTowardsSamePoint_Strict(torsoPos);
            TripleCastTowardsSamePoint_Strict(feetPos);
            // �������/��������� (��� ������)
            var horiz = ctx.MoveInput.x;
            if (Mathf.Abs(horiz) > 0.2f)
            {
                var lateralCastDir90 = ctx.Transform.right * Mathf.Sign(horiz);
                if (Physics.Raycast(torsoPos, lateralCastDir90, out var hLat90, maxDist * 1.6f, ctx.ClimbableLayers))
                {
                    AddUniqueHit(hLat90);
                    Debug.DrawRay(torsoPos, lateralCastDir90 * hLat90.distance, Color.magenta, 0.18f);
                }
                else
                {
                    Debug.DrawRay(torsoPos, lateralCastDir90 * maxDist, Color.gray, 0.18f);
                }

                var lateralCastDir45 = (ctx.Transform.forward + ctx.Transform.right * Mathf.Sign(horiz)).normalized;
                if (Physics.Raycast(torsoPos, lateralCastDir45, out var hLat45, maxDist * 1.6f, ctx.ClimbableLayers))
                {
                    AddUniqueHit(hLat45);
                    Debug.DrawRay(torsoPos, lateralCastDir45 * hLat45.distance, Color.cyan, 0.18f);
                }
                else
                {
                    Debug.DrawRay(torsoPos, lateralCastDir45 * maxDist, Color.gray, 0.18f);
                }
            }

            // torso spherecast fallback (������� � ����� �������, ���� �� �����)
            float sphereRadius = Mathf.Max(0.08f, ctx.Rigidbody != null ? ctx.Rigidbody.transform.localScale.x * 0.45f : 0.2f);
            var sphereHits = Physics.SphereCastAll(torsoPos, sphereRadius, forward, maxDist, ctx.ClimbableLayers);
            if (sphereHits != null && sphereHits.Length > 0)
            {
                foreach (var sh in sphereHits) AddUniqueHit(sh);
            }

            // lateral sphere fallback (��� ����)
            if (hits.Count == 0 && Mathf.Abs(ctx.MoveInput.x) > 0.3f)
            {
                var lateralDir = ctx.Transform.right * Mathf.Sign(ctx.MoveInput.x);
                var latHits = Physics.SphereCastAll(torsoPos, Mathf.Max(0.2f, 0.5f * 1.2f), lateralDir, maxDist, ctx.ClimbableLayers);
                if (latHits != null && latHits.Length > 0)
                {
                    foreach (var hh in latHits) AddUniqueHit(hh);
                }
            }

            if (hits.Count == 0)
            {
                allHits = hits;
                return false;
            }

            // ����� bestHit: ����� + ��������� ����� �� ���� � inputDir
            float bestScore = float.MaxValue;
            RaycastHit chosen = hits[0];
            var torsoPosForScore = center + Vector3.up * (playerHeight * (0.5f - 0.5f));
            foreach (var hh in hits)
            {
                var dist = (hh.point - torsoPosForScore).sqrMagnitude;
                var anglePenalty = Vector3.Angle(hh.normal, -inputDir);
                var score = dist + anglePenalty * 0.02f;
                if (score < bestScore)
                {
                    bestScore = score;
                    chosen = hh;
                }
            }

            bestHit = chosen;
            allHits = hits;
            return true;
        }


    }
}