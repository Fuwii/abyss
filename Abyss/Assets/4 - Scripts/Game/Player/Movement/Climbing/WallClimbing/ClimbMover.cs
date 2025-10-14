using System.Collections.Generic;
using Core.Utils;
using UnityEngine;

namespace Game.Player.Movement.Climbing.WallClimbing
{
    public class ClimbMover
    {
        public void ApplyMovement(ClimbingContext ctx, RaycastHit hit, List<RaycastHit> allHits,
            Vector3 usedNormal, Vector3 lateralDir, bool edgeDetected, ClimbConfig config)
        {
            // Compute wallUp and wallRight 
            var wallUp = Vector3.ProjectOnPlane(Vector3.up, usedNormal);
            if (wallUp.sqrMagnitude < 1e-4f)
            {
                wallUp = Vector3.Cross(usedNormal, ctx.Transform.right).normalized;
                if (wallUp.sqrMagnitude < 1e-4f) wallUp = ctx.Transform.up;
            }
            wallUp.Normalize();
            var wallRight = Vector3.Cross(wallUp, usedNormal).normalized;

            // Compute deterministic lateralDir based on player input
            Vector3 playerRightOnWall = Vector3.ProjectOnPlane(ctx.Transform.right, usedNormal);
            if (playerRightOnWall.sqrMagnitude < 1e-6f)
                playerRightOnWall = wallRight;
            playerRightOnWall.Normalize();

            // Remove any vertical component along wallUp to avoid "off-wall" movement
            lateralDir = Vector3.ProjectOnPlane(playerRightOnWall, wallUp);
            if (lateralDir.sqrMagnitude < 1e-6f)
                lateralDir = wallRight;
            lateralDir.Normalize();

            // Ensure usedNormal is normalized
            usedNormal = usedNormal.normalized;

            // Prefer Transform position as reference
            var refPos = ctx.Transform != null ? ctx.Transform.position
                : ctx.Rigidbody != null ? ctx.Rigidbody.position : Vector3.zero;

            // Flip normal if pointing wrong way
            if (Vector3.Dot(usedNormal, refPos - hit.point) < 0f)
                usedNormal = -usedNormal;

            // Compute target position
            var currentOffsetOnPlane = Vector3.ProjectOnPlane(refPos - hit.point, usedNormal);
            var stick = ctx.WallStickDistance;
            if (edgeDetected) stick *= 1.12f;
            var desiredBasePos = hit.point + currentOffsetOnPlane + usedNormal * stick;

            var shaped = MathExtensions.SquareToCircle(ctx.MoveInput);
            var moveDirection = wallUp * shaped.y + lateralDir * shaped.x;
            moveDirection = Vector3.ClampMagnitude(moveDirection, 1f);
            if (Mathf.Abs(ctx.MoveInput.x) > 0.2f || edgeDetected)
            {
                float moveAlongLat = Vector3.Dot(moveDirection, lateralDir);
                float moveAlongUp = Vector3.Dot(moveDirection, wallUp);
                var targetVelPreview = wallUp * (shaped.y * ((shaped.y < 0) ? config.descentSpeed : config.climbSpeed))
                                       + lateralDir * (shaped.x * config.climbSpeed);
            }
            ctx.ClimbingSubState = moveDirection == Vector3.zero
                ? FpsPlayerClimbing.ClimbingSubState.Idle
                : FpsPlayerClimbing.ClimbingSubState.Moving;

            var targetPosition = desiredBasePos + moveDirection * ((shaped.y < 0 ? config.descentSpeed : config.climbSpeed) * Time.fixedDeltaTime);
            var nextPos = Vector3.Lerp(ctx.Rigidbody.position, targetPosition, 10f * Time.fixedDeltaTime);

            var targetRot = Quaternion.LookRotation(-usedNormal, wallUp);
            ctx.Transform.rotation = Quaternion.Slerp(ctx.Transform.rotation, targetRot, 10f * Time.fixedDeltaTime);

            //Compute velocity along wall
            var verticalSpeed = shaped.y < 0f ? config.descentSpeed : config.climbSpeed;
            var lateralSpeed = config.climbSpeed;

            var bodyCenter = ctx.Rigidbody != null ? ctx.Rigidbody.worldCenterOfMass : ctx.Transform.position;
            var currentAlongNormal = Vector3.Dot(bodyCenter - hit.point, usedNormal);
            var desiredAlongNormal = stick;
            var distError = desiredAlongNormal - currentAlongNormal;

            var normalSpringK = 12f;
            var maxNormalSpeed = 5f;
            var desiredNormalVel = Mathf.Clamp(distError * normalSpringK, -maxNormalSpeed, maxNormalSpeed);

            var targetVel = wallUp * (shaped.y * verticalSpeed) + lateralDir * (shaped.x * lateralSpeed) + usedNormal * desiredNormalVel;

            var maxComponentSpeed = Mathf.Max(Mathf.Abs(verticalSpeed), Mathf.Abs(lateralSpeed)) + maxNormalSpeed;
            if (targetVel.magnitude > maxComponentSpeed)
                targetVel = targetVel.normalized * maxComponentSpeed;

            var currentVel = ctx.Rigidbody.linearVelocity;
            var maxDeltaSpeed = config.pushTowardWallVelDeltaPerSec * Time.fixedDeltaTime;
            ctx.Rigidbody.linearVelocity = Vector3.MoveTowards(currentVel, targetVel, maxDeltaSpeed);

            //Pull-up check
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
        }
    }
}
