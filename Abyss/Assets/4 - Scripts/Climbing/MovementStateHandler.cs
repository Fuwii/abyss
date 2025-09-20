using UnityEngine;

public class MovementStateHandler
{
    private readonly FpsPlayerClimbing owner;
    private MovementService movementService;

    public MovementStateHandler(FpsPlayerClimbing owner)
    {
        this.owner = owner;
        movementService = new MovementService
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
            case FpsPlayerClimbing.PlayerState.WALKING:
                playerStamina?.RemoveClimbingDebuff();
                var maybe = movementService.HandleWalking(rb, fpsController, jumpPressed);
                if (maybe.HasValue) return maybe.Value;

                // allow direct transition to climbing on click (owner.Update handles input click),
                // here we only handle possible auto-detection while walking (kept minimal)
                break;

            case FpsPlayerClimbing.PlayerState.FALLING:
                var ns = movementService.HandleFalling(rb, fpsController, jumpPressed, climbableLayers, transform, climbDetectionDistance);
                if (ns.HasValue) return ns.Value;
                break;

            case FpsPlayerClimbing.PlayerState.NO_STAMINA_FALLING:
                var n2 = movementService.HandleFalling(rb, fpsController, jumpPressed, climbableLayers, transform, climbDetectionDistance);
                if (n2.HasValue) return n2.Value;

                if (playerStamina != null && playerStamina.GetCurrentStamina() > playerStamina.GetCurrentMaxBaseStamina() * 0.3f)
                {
                    return FpsPlayerClimbing.PlayerState.FALLING;
                }
                break;
        }

        return null;
    }
}
