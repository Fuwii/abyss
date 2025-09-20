using System.Collections.Generic;
using UnityEngine;

public class ClimbRaycaster
{
    public bool FindClimbSurface3Cast(ClimbingContext ctx, out RaycastHit bestHit, out List<RaycastHit> allHits)
    {
        allHits = new List<RaycastHit>();
        bestHit = default;

        float maxDist = ctx.WallStickDistance;
        Vector3 center = ctx.Transform.position;
        float h = ctx.PlayerHeight > 0f ? ctx.PlayerHeight : 1.8f;

        Vector3 feetPos = center + Vector3.up * (h * (0.1f - 0.5f));   // feetHeightFactor = 0.1f
        Vector3 torsoPos = center + Vector3.up * (h * (0.5f - 0.5f));  // torsoHeightFactor = 0.5f
        Vector3 headPos = center + Vector3.up * (h * (0.9f - 0.5f));   // headHeightFactor = 0.9f

        Vector3 forward = ctx.Transform.forward;
        Vector3 inputDir = (ctx.Transform.forward * ctx.InputV + ctx.Transform.right * ctx.InputH);
        if (inputDir.sqrMagnitude < 0.001f) inputDir = forward;
        inputDir = inputDir.normalized;

        // feet cast
        if (Physics.Raycast(feetPos, forward, out RaycastHit hFeet, maxDist, ctx.ClimbableLayers))
            allHits.Add(hFeet);
        else
            Debug.DrawRay(feetPos, forward * maxDist, Color.gray, 0.18f);

        // head cast
        if (Physics.Raycast(headPos, forward, out RaycastHit hHead, maxDist, ctx.ClimbableLayers))
            allHits.Add(hHead);
        else
            Debug.DrawRay(headPos, forward * maxDist, Color.gray, 0.18f);

        // torso lateral casts when horizontal input significant
        bool torsoHit = false;
        float horiz = ctx.InputH;
        if (!torsoHit && Mathf.Abs(horiz) > 0.2f)
        {
            Vector3 lateralCastDir90 = ctx.Transform.right * Mathf.Sign(horiz);
            if (Physics.Raycast(torsoPos, lateralCastDir90, out RaycastHit hLat90, maxDist * 2, ctx.ClimbableLayers))
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
                Vector3 lateralCastDir45 = (ctx.Transform.forward + ctx.Transform.right * Mathf.Sign(horiz)).normalized;
                if (Physics.Raycast(torsoPos, lateralCastDir45, out RaycastHit hLat45, maxDist * 2, ctx.ClimbableLayers))
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
        if (allHits.Count > 0)
        {
            float bestScore = float.MaxValue;
            RaycastHit chosen = allHits[0];
            Vector3 torsoPosForScore = center + Vector3.up * (h * (0.5f - 0.5f)); // torsoHeightFactor=0.5
            foreach (var hh in allHits)
            {
                float dist = (hh.point - torsoPosForScore).sqrMagnitude;
                float anglePenalty = Vector3.Angle(hh.normal, -inputDir);
                float score = dist + anglePenalty * 0.01f;
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
        RaycastHit[] sphereHits = Physics.SphereCastAll(torsoPos, Mathf.Max(0.05f, ctx.Rigidbody != null ? ctx.Rigidbody.transform.localScale.x * 0.5f : 0.35f),
            forward, maxDist, ctx.ClimbableLayers);
        if (sphereHits != null && sphereHits.Length > 0)
        {
            foreach (var sh in sphereHits) allHits.Add(sh);
            RaycastHit best = sphereHits[0];
            float bestD = float.MaxValue;
            foreach (var sh in sphereHits)
            {
                if (sh.distance < bestD) { bestD = sh.distance; best = sh; }
            }
            bestHit = best;
            return true;
        }

        // lateral spherecast fallback when input H significant
        if (allHits.Count == 0 && Mathf.Abs(ctx.InputH) > 0.3f)
        {
            Vector3 lateralDirCast = ctx.Transform.right * Mathf.Sign(ctx.InputH);
            RaycastHit[] latHits = Physics.SphereCastAll(torsoPos, Mathf.Max(0.2f, 0.5f * 1.2f), lateralDirCast, maxDist, ctx.ClimbableLayers);
            if (latHits != null && latHits.Length > 0)
            {
                foreach (var hh in latHits) allHits.Add(hh);
                RaycastHit best = latHits[0];
                float bestD = float.MaxValue;
                foreach (var hh in latHits) if (hh.distance < bestD) { bestD = hh.distance; best = hh; }
                bestHit = best;
                return true;
            }
        }

        return false;
    }
}
