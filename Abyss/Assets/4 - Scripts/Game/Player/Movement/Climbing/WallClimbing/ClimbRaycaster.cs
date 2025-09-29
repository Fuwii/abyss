using System.Collections.Generic;
using UnityEngine;

namespace Game.Player.Movement.Climbing.WallClimbing
{
    public class ClimbRaycaster
    {
        public bool FindClimbSurface3Cast(ClimbingContext ctx, out RaycastHit bestHit, out List<RaycastHit> allHits)
        {
            allHits = new List<RaycastHit>();
            bestHit = default;

            var minDetectionDist = 1f;
            var maxDist = Mathf.Max(ctx.WallStickDistance, minDetectionDist);
            var center = ctx.Transform.position;
            var h = ctx.PlayerHeight > 0f ? ctx.PlayerHeight : 1.8f;

            var feetPos = center + Vector3.up * (h * (0.1f - 0.5f)); // feetHeightFactor = 0.1f
            var torsoPos = center + Vector3.up * (h * (0.5f - 0.5f)); // torsoHeightFactor = 0.5f
            var headPos = center + Vector3.up * (h * (0.9f - 0.5f)); // headHeightFactor = 0.9f

            var forward = ctx.Transform.forward;
            var inputDir = ctx.Transform.forward * ctx.MoveInput.y + ctx.Transform.right * ctx.MoveInput.x;
            if (inputDir.sqrMagnitude < 0.001f) inputDir = forward;
            inputDir = inputDir.normalized;

            // feet cast
            if (Physics.Raycast(feetPos, forward, out var hFeet, maxDist, ctx.ClimbableLayers))
                allHits.Add(hFeet);
            else
                Debug.DrawRay(feetPos, forward * maxDist, Color.gray, 0.18f);

            // head cast
            if (Physics.Raycast(headPos, forward, out var hHead, maxDist, ctx.ClimbableLayers))
                allHits.Add(hHead);
            else
                Debug.DrawRay(headPos, forward * maxDist, Color.gray, 0.18f);

            // torso lateral casts when horizontal input significant
            var torsoHit = false;
            var horiz = ctx.MoveInput.x;

            if (Mathf.Abs(horiz) > 0.2f)
            {
                var lateralCastDir90 = ctx.Transform.right * Mathf.Sign(horiz);
                if (Physics.Raycast(torsoPos, lateralCastDir90, out var hLat90, maxDist * 2, ctx.ClimbableLayers))
                {
                    allHits.Add(hLat90);
                    torsoHit = true;
                    Debug.DrawRay(torsoPos, lateralCastDir90 * hLat90.distance, Color.magenta, 0.18f);
                }
                else
                {
                    Debug.DrawRay(torsoPos, lateralCastDir90 * maxDist, Color.gray, 0.18f);
                }

                if (!torsoHit)
                {
                    var lateralCastDir45 = (ctx.Transform.forward + ctx.Transform.right * Mathf.Sign(horiz)).normalized;
                    if (Physics.Raycast(torsoPos, lateralCastDir45, out var hLat45, maxDist * 2, ctx.ClimbableLayers))
                    {
                        allHits.Add(hLat45);
                        torsoHit = true;
                        Debug.DrawRay(torsoPos, lateralCastDir45 * hLat45.distance, Color.cyan, 0.18f);
                    }
                    else
                    {
                        Debug.DrawRay(torsoPos, lateralCastDir45 * maxDist, Color.gray, 0.18f);
                    }
                }
            }

            // choose bestHit if any
            if (allHits.Count > 1)
            {
                var bestScore = float.MaxValue;
                var chosen = allHits[0];
                var torsoPosForScore = center + Vector3.up * (h * (0.5f - 0.5f)); // torsoHeightFactor=0.5
                foreach (var hh in allHits)
                {
                    var dist = (hh.point - torsoPosForScore).sqrMagnitude;
                    var anglePenalty = Vector3.Angle(hh.normal, -inputDir);
                    var score = dist + anglePenalty * 0.01f;
                    if (score < bestScore)
                    {
                        bestScore = score;
                        chosen = hh;
                    }
                }

                bestHit = chosen;
                return true;
            }

            // fallback: forward spherecast from torso
            var sphereHits = Physics.SphereCastAll(torsoPos, Mathf.Max(0.05f, ctx.Rigidbody != null ? ctx.Rigidbody.transform.localScale.x * 0.5f : 0.35f),
                forward, maxDist, ctx.ClimbableLayers);
            if (sphereHits != null && sphereHits.Length > 0)
            {
                foreach (var sh in sphereHits) allHits.Add(sh);
                var best = sphereHits[0];
                var bestD = float.MaxValue;
                foreach (var sh in sphereHits)
                {
                    if (sh.distance < bestD)
                    {
                        bestD = sh.distance;
                        best = sh;
                    }
                }
                bestHit = best;
                return true;
            }

            // lateral spherecast fallback when input H significant
            if (allHits.Count == 0 && Mathf.Abs(ctx.MoveInput.x) > 0.3f)
            {
                var lateralDirCast = ctx.Transform.right * Mathf.Sign(ctx.MoveInput.x);
                var latHits = Physics.SphereCastAll(torsoPos, Mathf.Max(0.2f, 0.5f * 1.2f), lateralDirCast, maxDist, ctx.ClimbableLayers);
                if (latHits != null && latHits.Length > 0)
                {
                    foreach (var hh in latHits) allHits.Add(hh);
                    var best = latHits[0];
                    var bestD = float.MaxValue;
                    foreach (var hh in latHits)
                        if (hh.distance < bestD)
                        {
                            bestD = hh.distance;
                            best = hh;
                        }

                    bestHit = best;
                    return true;
                }
            }

            return false;
        }
    }
}