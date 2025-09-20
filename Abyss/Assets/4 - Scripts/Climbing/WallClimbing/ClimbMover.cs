using System.Collections.Generic;
using UnityEngine;

public class ClimbMover
{
    private float debugRayDuration = 0.18f;

    public void ApplyMovement(ClimbingContext ctx, RaycastHit hit, List<RaycastHit> allHits,
        Vector3 usedNormal, Vector3 lateralDir, bool edgeDetected, ClimbConfig config)
    {
        // compute wallUp and fallback wallRight
        Vector3 wallUp = Vector3.ProjectOnPlane(Vector3.up, usedNormal);
        if (wallUp.sqrMagnitude < 1e-4f)
        {
            wallUp = Vector3.Cross(usedNormal, ctx.Transform.right).normalized;
            if (wallUp.sqrMagnitude < 1e-4f) wallUp = ctx.Transform.up;
        }
        wallUp.Normalize();
        Vector3 wallRight = Vector3.Cross(wallUp, usedNormal).normalized;

        // ensure lateralDir valid and in wall plane
        if (lateralDir.sqrMagnitude < 1e-6f)
        {
            lateralDir = -wallRight;
        }
        else
        {
            if (Vector3.Dot(lateralDir, -wallRight) < 0f) lateralDir = -lateralDir;
            lateralDir = Vector3.ProjectOnPlane(lateralDir, usedNormal);
            if (lateralDir.sqrMagnitude < 1e-6f)
            {
                lateralDir = -wallRight;
            }
            else
            {
                lateralDir.Normalize();
            }
        }

        float stick = config.WallStickDistance;
        if (edgeDetected) stick *= 1.12f;

        Vector3 currentOffsetOnPlane = Vector3.ProjectOnPlane(ctx.Rigidbody.position - hit.point, usedNormal);
        Vector3 desiredBasePos = hit.point + currentOffsetOnPlane + usedNormal * stick;

        Vector2 shaped = Utilities.SquareToCircle(new Vector2(ctx.InputH, ctx.InputV));
        Vector3 moveDirection = (wallUp * shaped.y) + (lateralDir * shaped.x);
        moveDirection = Vector3.ClampMagnitude(moveDirection, 1f);

        ctx.ClimbingSubState = moveDirection == Vector3.zero
            ? FpsPlayerClimbing.ClimbingSubState.Idle
            : FpsPlayerClimbing.ClimbingSubState.Moving;

        Vector3 targetPosition = desiredBasePos + moveDirection * ((shaped.y < 0) ? config.DescentSpeed : config.ClimbSpeed) * Time.fixedDeltaTime;
        Vector3 nextPos = Vector3.Lerp(ctx.Rigidbody.position, targetPosition, 10f * Time.fixedDeltaTime);

        Quaternion targetRot = Quaternion.LookRotation(-usedNormal, wallUp);
        ctx.Transform.rotation = Quaternion.Slerp(ctx.Transform.rotation, targetRot, 10f * Time.fixedDeltaTime);

        float verticalSpeed = (shaped.y < 0f) ? config.DescentSpeed : config.ClimbSpeed;
        float lateralSpeed = config.ClimbSpeed;

        Vector3 targetVel = (wallUp * (shaped.y * verticalSpeed)) + (lateralDir * (shaped.x * lateralSpeed));
        float maxComponentSpeed = Mathf.Max(Mathf.Abs(verticalSpeed), Mathf.Abs(lateralSpeed));
        if (targetVel.magnitude > maxComponentSpeed)
            targetVel = targetVel.normalized * maxComponentSpeed;

        Vector3 currentVel = ctx.Rigidbody.linearVelocity;
        float maxDeltaSpeed = config.PushTowardWallVelDeltaPerSec * Time.fixedDeltaTime;
        Vector3 newVel = Vector3.MoveTowards(currentVel, targetVel, maxDeltaSpeed);
        ctx.Rigidbody.linearVelocity = newVel;

        // Pull-up check (как было)
        if (ctx.ClimbingSubState == FpsPlayerClimbing.ClimbingSubState.Moving)
        {
            float playerHeight = ctx.PlayerHeight;
            Vector3 headPos = ctx.Transform.position + Vector3.up * (playerHeight * 0.9f);
            Vector3 dir45 = ((-hit.normal).normalized + Vector3.down).normalized;
            if (Physics.Raycast(headPos, dir45, out RaycastHit ledgeHit, ctx.PullupCheckDistance, ctx.ClimbableLayers))
            {
                if (ledgeHit.normal.y > config.MaxAllowedNormalY)
                {
                    Vector3 upImpulse = Vector3.up * 12f;
                    Vector3 forwardImpulse = (-hit.normal).normalized * 4f;
                    var req = new PullUpRequest { UpImpulse = upImpulse, ForwardImpulse = forwardImpulse };
                    ctx.RequestPullUp?.Invoke(req);
                    ctx.Rigidbody.AddForce(upImpulse + forwardImpulse, ForceMode.VelocityChange);
                    ctx.RequestTransitionToFalling?.Invoke();
                }
            }
        }

        // Debug draws
        foreach (var h in allHits)
            Debug.DrawRay(h.point, h.normal * 0.35f, Color.yellow, debugRayDuration);

        Debug.DrawRay(hit.point, hit.normal * 0.4f, Color.red, debugRayDuration);
        Debug.DrawRay(hit.point, usedNormal * 0.4f, Color.green, debugRayDuration);
        Debug.DrawRay(ctx.Transform.position + Vector3.up * (ctx.PlayerHeight * 0.5f), lateralDir * 0.6f, Color.cyan, debugRayDuration);

        Debug.Log($"3Cast_hits={allHits.Count} sampled=? edge={edgeDetected} used={usedNormal} lateral={lateralDir}");
    }
}
