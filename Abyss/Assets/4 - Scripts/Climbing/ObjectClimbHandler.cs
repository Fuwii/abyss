using UnityEngine;

public class ObjectClimbHandler : IClimbHandler
{
    private readonly ClimbConfig config;

    public ObjectClimbHandler(ClimbConfig cfg) { config = cfg; }

    public bool Handle(ClimbingContext ctx)
    {
        float climbSpeedAlong = Mathf.Max(0.1f, config.ClimbSpeed); 
        float climbJumpUpImpulse = 8f;     
        float climbJumpOutImpulse = 6f;    
        float topSnapDistance = 0.05f;     
        Vector3 attachPos = ctx.CurrentClimbable.GetAttachPosition(ctx.Transform.position);

        Vector3 climbDir = ctx.CurrentClimbable.GetClimbDirection().normalized; // направление вверх по объекту

        // Movement along the climb direction (inputV up/down)
        float move = ctx.InputV; // -1..1 : 
        if(CheckIsTopReached(ctx,topSnapDistance,climbDir))
        {
            Debug.Log("TOP");
            //hz poka ne pridumal
            if (move > 0f)
            {
                Debug.Log("TOPBlocked");
                move = 0f; 
            }
        }
        Vector3 desiredVelAlong = climbDir * (move * climbSpeedAlong);
        // Maintain existing lateral velocity? Usually we zero lateral to keep on rope.
        // Compute new velocity as projection on climbDir
        Vector3 currVel = ctx.Rigidbody.linearVelocity;
        Vector3 projected = Vector3.Project(currVel, climbDir);
        Vector3 lateral = currVel - projected;

        lateral = Vector3.zero;

        // Smoothly move projected component toward desiredVelAlong
        float smooth = 10f * Time.fixedDeltaTime;
        Vector3 newProjected = Vector3.Lerp(projected, desiredVelAlong, smooth);

        // Compose final velocity
        ctx.Rigidbody.linearVelocity = newProjected + lateral;

        // Stick player to object: try to keep player near object's attach axis
        // Move player position slightly toward nearest point on object's axis to prevent drifting
        // simple approach: project player's position onto line passing through attachPos in climbDir
        Vector3 toPlayer = ctx.Transform.position - attachPos;
        float along = Vector3.Dot(toPlayer, climbDir);
        Vector3 nearestPoint = attachPos + climbDir * along;
        Vector3 correction = Vector3.Lerp(ctx.Rigidbody.position, nearestPoint + ctx.CurrentClimbable.transform.right * 0f /* if offset needed */, 8f * Time.fixedDeltaTime);
        // apply small position correction
        ctx.Rigidbody.position = new Vector3(correction.x, correction.y, correction.z);

        // update substate
        ctx.ClimbingSubState = Mathf.Abs(move) < 0.01f ? FpsPlayerClimbing.ClimbingSubState.Idle : FpsPlayerClimbing.ClimbingSubState.Moving;

        // Jump off the rope/chain
        if (ctx.JumpPressed)
        {
            // Jump: strong up then outward away from the object
            //Vector3 outDir = (ctx.Transform.position - ctx.CurrentClimbable.transform.position).normalized;
            Vector3 outDir = ctx.Transform.forward; 
            // prefer horizontal outward component
            Vector3 outDirHor = Vector3.ProjectOnPlane(outDir, Vector3.up).normalized;
            Vector3 upImpulse = climbDir * climbJumpUpImpulse; // using climbDir as "up" of rope (could differ)
            Vector3 forwardImpulse = outDirHor * climbJumpOutImpulse;
            ctx.Rigidbody.linearVelocity = Vector3.zero; // reset to avoid weird accumulation
            ctx.Rigidbody.AddForce(upImpulse + forwardImpulse, ForceMode.VelocityChange);

            // request state transition out (or let orchestrator handle)
            ctx.RequestTransitionToFalling?.Invoke();
            return false;
        }

       

        // Stamina handling if present (reuse same pattern)
        if (ctx.ConsumeStamina != null)
        {
            float cost = ctx.ClimbingSubState == FpsPlayerClimbing.ClimbingSubState.Moving ? ctx.StaminaMoveCost * Time.fixedDeltaTime : ctx.StaminaIdleCost * Time.fixedDeltaTime;
            if (cost > 0f && !ctx.ConsumeStamina(cost))
            {
                ctx.RequestTransitionToFalling?.Invoke(); // out of stamina
                return false;
            }
        }
        return ctx.CurrentClimbable != null;
    }
    bool CheckIsTopReached(ClimbingContext ctx,float topSnapDistance, Vector3 climbDir)
    {
        // Check if reached top (attach point's top or predefined top area)
        // We'll consider top when player's projected along value is >= topThreshold
        float topThreshold = ctx.CurrentClimbable.transform.InverseTransformPoint(ctx.Transform.position).y; // optional                                                                                             // simpler: distance to top (attach point + some offset)
        Vector3 topPoint = ctx.CurrentClimbable.attachPoint.position + climbDir * (ctx.PlayerHeight * 0.5f);

        if (ctx.CurrentClimbable.attachPoint != null)
        {
            float dist = Vector3.Distance(ctx.Transform.position, topPoint) - ctx.CurrentClimbable.attachDistance;
            if (dist <= topSnapDistance)
            {
                return true;
            }

        }
        return false; 
    }

}