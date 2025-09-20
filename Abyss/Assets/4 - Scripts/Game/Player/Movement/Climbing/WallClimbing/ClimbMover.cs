using System.Collections.Generic;
using Core.Utils;
using UnityEngine;

namespace Game.Player.Movement.Climbing.WallClimbing
{
    public class ClimbMover
    {
        private const float DebugRayDuration = 0.18f;

        public void ApplyMovement(ClimbingContext ctx, RaycastHit hit, List<RaycastHit> allHits,
            Vector3 usedNormal, Vector3 lateralDir, bool edgeDetected, ClimbConfig config)
        {
            // compute wallUp and fallback wallRight
            var wallUp = Vector3.ProjectOnPlane(Vector3.up, usedNormal);
            if (wallUp.sqrMagnitude < 1e-4f)
            {
                wallUp = Vector3.Cross(usedNormal, ctx.Transform.right).normalized;
                if (wallUp.sqrMagnitude < 1e-4f) wallUp = ctx.Transform.up;
            }

            wallUp.Normalize();
            var wallRight = Vector3.Cross(wallUp, usedNormal).normalized;

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

            var stick = config.wallStickDistance;
            if (edgeDetected) stick *= 1.12f;

            var currentOffsetOnPlane = Vector3.ProjectOnPlane(ctx.Rigidbody.position - hit.point, usedNormal);
            var desiredBasePos = hit.point + currentOffsetOnPlane + usedNormal * stick;

            var shaped = MathExtensions.SquareToCircle(new Vector2(ctx.InputH, ctx.InputV));
            var moveDirection = (wallUp * shaped.y) + (lateralDir * shaped.x);
            moveDirection = Vector3.ClampMagnitude(moveDirection, 1f);

            ctx.ClimbingSubState = moveDirection == Vector3.zero
                ? FpsPlayerClimbing.ClimbingSubState.Idle
                : FpsPlayerClimbing.ClimbingSubState.Moving;

            var targetPosition = desiredBasePos + moveDirection * ((shaped.y < 0 ? config.descentSpeed : config.climbSpeed) * Time.fixedDeltaTime);
            var nextPos = Vector3.Lerp(ctx.Rigidbody.position, targetPosition, 10f * Time.fixedDeltaTime);

            var targetRot = Quaternion.LookRotation(-usedNormal, wallUp);
            ctx.Transform.rotation = Quaternion.Slerp(ctx.Transform.rotation, targetRot, 10f * Time.fixedDeltaTime);

            var verticalSpeed = (shaped.y < 0f) ? config.descentSpeed : config.climbSpeed;
            var lateralSpeed = config.climbSpeed;

            var targetVel = (wallUp * (shaped.y * verticalSpeed)) + (lateralDir * (shaped.x * lateralSpeed));
            var maxComponentSpeed = Mathf.Max(Mathf.Abs(verticalSpeed), Mathf.Abs(lateralSpeed));
            if (targetVel.magnitude > maxComponentSpeed)
                targetVel = targetVel.normalized * maxComponentSpeed;

            var currentVel = ctx.Rigidbody.linearVelocity;
            var maxDeltaSpeed = config.pushTowardWallVelDeltaPerSec * Time.fixedDeltaTime;
            var newVel = Vector3.MoveTowards(currentVel, targetVel, maxDeltaSpeed);
            ctx.Rigidbody.linearVelocity = newVel;

            // Pull-up check (��� ����)
            if (ctx.ClimbingSubState == FpsPlayerClimbing.ClimbingSubState.Moving)
            {
                var playerHeight = ctx.PlayerHeight;
                var headPos = ctx.Transform.position + Vector3.up * (playerHeight * 0.9f);
                var dir45 = ((-hit.normal).normalized + Vector3.down).normalized;
                if (Physics.Raycast(headPos, dir45, out var ledgeHit, ctx.PullupCheckDistance, ctx.ClimbableLayers))
                {
                    if (ledgeHit.normal.y > config.maxAllowedNormalY)
                    {
                        var upImpulse = Vector3.up * 12f;
                        var forwardImpulse = (-hit.normal).normalized * 4f;
                        var req = new PullUpRequest { UpImpulse = upImpulse, ForwardImpulse = forwardImpulse };
                        ctx.RequestPullUp?.Invoke(req);
                        ctx.Rigidbody.AddForce(upImpulse + forwardImpulse, ForceMode.VelocityChange);
                        ctx.RequestTransitionToFalling?.Invoke();
                    }
                }
            }

            // Debug draws
            foreach (var h in allHits)
                Debug.DrawRay(h.point, h.normal * 0.35f, Color.yellow, DebugRayDuration);

            Debug.DrawRay(hit.point, hit.normal * 0.4f, Color.red, DebugRayDuration);
            Debug.DrawRay(hit.point, usedNormal * 0.4f, Color.green, DebugRayDuration);
            Debug.DrawRay(ctx.Transform.position + Vector3.up * (ctx.PlayerHeight * 0.5f), lateralDir * 0.6f, Color.cyan, DebugRayDuration);

            Debug.Log($"3Cast_hits={allHits.Count} sampled=? edge={edgeDetected} used={usedNormal} lateral={lateralDir}");
        }
    }
}