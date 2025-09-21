using UnityEngine;

namespace Game.Player.Movement.Climbing
{
    public class ObjectClimbHandler : IClimbHandler
    {
        private readonly ClimbConfig _config;

        public ObjectClimbHandler(ClimbConfig cfg)
        {
            _config = cfg;
        }

        public bool Handle(ClimbingContext ctx)
        {
            var climbSpeedAlong = Mathf.Max(0.1f, _config.climbSpeed);
            var climbJumpUpImpulse = 8f;
            var climbJumpOutImpulse = 6f;
            var topSnapDistance = 0.05f;
            var attachPos = ctx.CurrentClimbable.GetAttachPosition(ctx.Transform.position);

            var climbDir = ctx.CurrentClimbable.GetClimbDirection().normalized; // ����������� ����� �� �������

            // Movement along the climb direction (inputV up/down)
            var move = ctx.MoveInput.x; // -1..1 : 
            if (CheckIsTopReached(ctx, topSnapDistance, climbDir))
            {
                Debug.Log("TOP");

                //hz poka ne pridumal
                if (move > 0f)
                {
                    Debug.Log("TOPBlocked");
                    move = 0f;
                }
            }

            var desiredVelAlong = climbDir * (move * climbSpeedAlong);

            // Maintain existing lateral velocity? Usually we zero lateral to keep on rope.
            // Compute new velocity as projection on climbDir
            var currVel = ctx.Rigidbody.linearVelocity;
            var projected = Vector3.Project(currVel, climbDir);
            var lateral = currVel - projected;

            lateral = Vector3.zero;

            // Smoothly move projected component toward desiredVelAlong
            var smooth = 10f * Time.fixedDeltaTime;
            var newProjected = Vector3.Lerp(projected, desiredVelAlong, smooth);

            // Compose final velocity
            ctx.Rigidbody.linearVelocity = newProjected + lateral;

            // Stick player to object: try to keep player near object's attach axis
            // Move player position slightly toward nearest point on object's axis to prevent drifting
            // simple approach: project player's position onto line passing through attachPos in climbDir
            var toPlayer = ctx.Transform.position - attachPos;
            var along = Vector3.Dot(toPlayer, climbDir);
            var nearestPoint = attachPos + climbDir * along;
            var correction = Vector3.Lerp(ctx.Rigidbody.position, nearestPoint + ctx.CurrentClimbable.transform.right * 0f /* if offset needed */, 8f * Time.fixedDeltaTime);

            // apply small position correction
            ctx.Rigidbody.position = new Vector3(correction.x, correction.y, correction.z);

            // update substate
            ctx.ClimbingSubState = Mathf.Abs(move) < 0.01f ? FpsPlayerClimbing.ClimbingSubState.Idle : FpsPlayerClimbing.ClimbingSubState.Moving;

            // Jump off the rope/chain
            if (ctx.JumpPressed)
            {
                // Jump: strong up then outward away from the object
                //Vector3 outDir = (ctx.Transform.position - ctx.CurrentClimbable.transform.position).normalized;
                var outDir = ctx.Transform.forward;

                // prefer horizontal outward component
                var outDirHor = Vector3.ProjectOnPlane(outDir, Vector3.up).normalized;
                var upImpulse = climbDir * climbJumpUpImpulse; // using climbDir as "up" of rope (could differ)
                var forwardImpulse = outDirHor * climbJumpOutImpulse;
                ctx.Rigidbody.linearVelocity = Vector3.zero; // reset to avoid weird accumulation
                ctx.Rigidbody.AddForce(upImpulse + forwardImpulse, ForceMode.VelocityChange);

                // request state transition out (or let orchestrator handle)
                ctx.RequestTransitionToFalling?.Invoke();
                return false;
            }

            // Stamina handling if present (reuse same pattern)
            if (ctx.ConsumeStamina != null)
            {
                var cost = ctx.ClimbingSubState == FpsPlayerClimbing.ClimbingSubState.Moving ? ctx.StaminaMoveCost * Time.fixedDeltaTime : ctx.StaminaIdleCost * Time.fixedDeltaTime;
                if (cost > 0f && !ctx.ConsumeStamina(cost))
                {
                    ctx.RequestTransitionToFalling?.Invoke(); // out of stamina
                    return false;
                }
            }

            return ctx.CurrentClimbable != null;
        }

        bool CheckIsTopReached(ClimbingContext ctx, float topSnapDistance, Vector3 climbDir)
        {
            // Check if reached top (attach point's top or predefined top area)
            // We'll consider top when player's projected along value is >= topThreshold
            var topThreshold = ctx.CurrentClimbable.transform.InverseTransformPoint(ctx.Transform.position).y; // optional // simpler: distance to top (attach point + some offset)
            var topPoint = ctx.CurrentClimbable.attachPoint.position + climbDir * (ctx.PlayerHeight * 0.5f);

            if (ctx.CurrentClimbable.attachPoint != null)
            {
                var dist = Vector3.Distance(ctx.Transform.position, topPoint) - ctx.CurrentClimbable.attachDistance;
                if (dist <= topSnapDistance)
                {
                    return true;
                }
            }

            return false;
        }
    }
}