using Game.Player.Movement.Ground;
using UnityEngine;

namespace Game.Player.Movement.Climbing
{
    public class MovementService
    {
        public float JumpForce { get; set; } = 5f;
        public float MaxFallSpeed { get; set; } = 20f;
        public float GravityMultiplier { get; set; } = 1f;

        // Handle walking inputs & transitions. Returns new state if changed, otherwise null.
        public FpsPlayerClimbing.PlayerState? HandleWalking(Rigidbody rb, FpsPlayerController fpsController, bool jumpPressed)
        {
            if (jumpPressed)
            {
                var v = rb.linearVelocity;
                v.y = JumpForce;
                rb.linearVelocity = v;
                return FpsPlayerClimbing.PlayerState.Falling;
            }

            if (!fpsController.IsGrounded())
            {
                return FpsPlayerClimbing.PlayerState.Falling;
            }

            return null;
        }

        // Generic falling behavior (applies gravity, clamps velocity). Returns new state if landed.
        public FpsPlayerClimbing.PlayerState? HandleFalling(Rigidbody rb, FpsPlayerController fpsController, bool jumpPressed, LayerMask climbableLayers, Transform transform, float climbDetectionDistance)
        {
            // Grab while falling
            if (UnityEngine.Input.GetMouseButtonDown(0) && ClimbDetector.CanClimb(transform, Camera.main ? Camera.main.transform : null, climbDetectionDistance, climbableLayers, 70f, 0.7f))
            {
                return FpsPlayerClimbing.PlayerState.Climbing;
            }

            // Ground check
            if (fpsController != null && fpsController.IsGrounded())
            {
                if (!fpsController.enabled) fpsController.enabled = true;
                return FpsPlayerClimbing.PlayerState.Walking;
            }

            // Apply gravity
            rb.linearVelocity += Vector3.down * Physics.gravity.magnitude * GravityMultiplier * Time.fixedDeltaTime;
            if (rb.linearVelocity.y < -MaxFallSpeed)
            {
                var v = rb.linearVelocity;
                v.y = -MaxFallSpeed;
                rb.linearVelocity = v;
            }

            return null;
        }
    }
}