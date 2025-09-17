using UnityEngine;

public class WallClimbHandler : IClimbHandler
{
    private readonly ClimbConfig config;

    public WallClimbHandler(ClimbConfig cfg) { config = cfg; }

    public bool Handle(ClimbingContext ctx)
    {
        // ctx contains: Rigidbody rb, Transform transform, inputs, climbable mask, stamina logic delegate, etc.
        if (ctx == null) return false;

        // Stamina handling: delegate to caller via ctx.ConsumeStamina(amount) -> bool
        float cost = 0f;
        switch (ctx.ClimbingSubState)
        {
            case FpsPlayerClimbing.ClimbingSubState.Moving: cost = ctx.StaminaMoveCost * Time.fixedDeltaTime; break;
            case FpsPlayerClimbing.ClimbingSubState.Idle: cost = ctx.StaminaIdleCost * Time.fixedDeltaTime; break;
            case FpsPlayerClimbing.ClimbingSubState.Hanging: cost = 0f; break;
        }
        if (cost > 0f && ctx.ConsumeStamina != null && !ctx.ConsumeStamina(cost))
        {
            // request transition to NO_STAMINA_FALLING handled by orchestrator
            ctx.RequestNoStamina();
            return false;
        }

        // Detect wall
        if (!ClimbDetector.TryRay(ctx.Transform.position, ctx.Transform.forward, ctx.ClimbDetectionDistance, ctx.ClimbableLayers, out RaycastHit hit) &&
            !ClimbDetector.TryRay(ctx.Transform.position + Vector3.up * 0.5f, ctx.Transform.forward, ctx.ClimbDetectionDistance, ctx.ClimbableLayers, out hit))
        {
            // can't find enough wall -> fall
            ctx.RequestTransitionToFalling?.Invoke();
            return false;
        }

        // If surface too horizontal -> fall
        if (hit.normal.y > config.MaxAllowedNormalY)
        {
            ctx.RequestTransitionToFalling?.Invoke();
            return false;
        }

        // Compute wall up/right
        Vector3 wallUp = Vector3.ProjectOnPlane(Vector3.up, hit.normal);
        if (wallUp.sqrMagnitude < 1e-4f)
        {
            wallUp = Vector3.Cross(hit.normal, ctx.Transform.right).normalized;
            if (wallUp.sqrMagnitude < 1e-4f) wallUp = ctx.Transform.up;
        }
        wallUp.Normalize();
        Vector3 wallRight = Vector3.Cross(wallUp, hit.normal).normalized;

        // plane positioning
        Vector3 currentOffsetOnPlane = Vector3.ProjectOnPlane(ctx.Rigidbody.position - hit.point, hit.normal);
        Vector3 desiredBasePos = hit.point + currentOffsetOnPlane + hit.normal * config.WallStickDistance;

        // input shaping
        Vector2 shaped = Utilities.SquareToCircle(new Vector2(ctx.InputH, ctx.InputV));
        Vector3 moveDirection = (wallUp * shaped.y) + (-wallRight * shaped.x);
        moveDirection = Vector3.ClampMagnitude(moveDirection, 1f);

        // set substate
        ctx.ClimbingSubState = moveDirection == Vector3.zero ? FpsPlayerClimbing.ClimbingSubState.Idle : FpsPlayerClimbing.ClimbingSubState.Moving;

        // compute target position and smooth
        Vector3 targetPosition = desiredBasePos + moveDirection * ((shaped.y < 0) ? config.DescentSpeed : config.ClimbSpeed) * Time.fixedDeltaTime;
        Vector3 nextPos = Vector3.Lerp(ctx.Rigidbody.position, targetPosition, 10f * Time.fixedDeltaTime);

        // rotation toward wall
        Quaternion targetRot = Quaternion.LookRotation(-hit.normal, wallUp);
        ctx.Transform.rotation = Quaternion.Slerp(ctx.Transform.rotation, targetRot, 10f * Time.fixedDeltaTime);

        // velocity smoothing (we only touch horizontal/planar component)
        float currentSpeed = (shaped.y < 0) ? config.DescentSpeed : config.ClimbSpeed;
        Vector3 targetVel = moveDirection * currentSpeed;
        Vector3 currentVel = ctx.Rigidbody.linearVelocity;
        float maxDeltaSpeed = config.PushTowardWallVelDeltaPerSec * Time.fixedDeltaTime;
        Vector3 newVel = Vector3.MoveTowards(currentVel, targetVel, maxDeltaSpeed);
        ctx.Rigidbody.linearVelocity = newVel;

        // Pull-up detection (same logic): cast from head at 45deg
        if (ctx.ClimbingSubState == FpsPlayerClimbing.ClimbingSubState.Moving)
        {
            float playerHeight = ctx.PlayerHeight;
            Vector3 headPos = ctx.Transform.position + Vector3.up * (playerHeight * 0.9f);
            Vector3 dir45 = ((-hit.normal).normalized + Vector3.down).normalized;
            if (Physics.Raycast(headPos, dir45, out RaycastHit ledgeHit, ctx.PullupCheckDistance, ctx.ClimbableLayers))
            {
                if (ledgeHit.normal.y > config.MaxAllowedNormalY)
                {
                    // apply impulse if requested by ctx
                    Vector3 upImpulse = Vector3.up * 12f;
                    Vector3 forwardImpulse = (-hit.normal).normalized * 4f;
                    var req = new PullUpRequest
                    {
                        UpImpulse = upImpulse,
                        ForwardImpulse = forwardImpulse,

                    };
                    ctx.RequestPullUp?.Invoke(req);
                    ctx.Rigidbody.AddForce(upImpulse + forwardImpulse, ForceMode.VelocityChange);

                    ctx.RequestTransitionToFalling?.Invoke();
                }
            }
        }

        // Note: position correction can be applied by caller if desired (MovePosition)
        return true; // остаёмся активны до выхода из climbing
    }
}
