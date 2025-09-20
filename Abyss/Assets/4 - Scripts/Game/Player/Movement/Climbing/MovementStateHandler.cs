using Game.Player.Movement.Ground;
using Game.Player.Stamina;
using UnityEngine;

namespace Game.Player.Movement.Climbing
{
    public class MovementStateHandler
    {
        private readonly FpsPlayerClimbing _owner;
        private readonly MovementService _movementService;

        public MovementStateHandler(FpsPlayerClimbing owner)
        {
            this._owner = owner;
            _movementService = new MovementService
            {
                JumpForce = owner.jumpForce,
                MaxFallSpeed = owner.maxFallSpeed,
                GravityMultiplier = 1f
            };
        }

        public FpsPlayerClimbing.PlayerState? HandleState(FpsPlayerClimbing.PlayerState state,
            Rigidbody rb, FpsPlayerController fpsController, bool jumpPressed,
            PlayerStamina playerStamina,
            LayerMask climbableLayers, Transform transform, float climbDetectionDistance)
        {
            switch (state)
            {
                case FpsPlayerClimbing.PlayerState.Walking:
                    playerStamina?.RemoveClimbingDebuff();
                    var maybe = _movementService.HandleWalking(rb, fpsController, jumpPressed);
                    if (maybe.HasValue) return maybe.Value;

                    // allow direct transition to climbing on click (owner.Update handles input click),
                    // here we only handle possible auto-detection while walking (kept minimal)
                    break;

                case FpsPlayerClimbing.PlayerState.Falling:
                    var ns = _movementService.HandleFalling(rb, fpsController, jumpPressed, climbableLayers, transform, climbDetectionDistance);
                    if (ns.HasValue) return ns.Value;
                    break;

                case FpsPlayerClimbing.PlayerState.NoStaminaFalling:
                    var n2 = _movementService.HandleFalling(rb, fpsController, jumpPressed, climbableLayers, transform, climbDetectionDistance);
                    if (n2.HasValue) return n2.Value;

                    if (playerStamina != null && playerStamina.GetCurrentStamina() > playerStamina.GetCurrentMaxBaseStamina() * 0.3f)
                    {
                        return FpsPlayerClimbing.PlayerState.Falling;
                    }

                    break;
            }

            return null;
        }
    }
}