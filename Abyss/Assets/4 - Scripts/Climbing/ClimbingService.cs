using System;
using UnityEngine;
public class ClimbingService
{
    private IClimbHandler activeHandler;

    // High-level events/hooks. PullUp passes data through PullUpRequest.
    public event Action<PullUpRequest> OnPullUpRequested;
    public event Action OnTransitionToFallingRequested;
    public event Action OnNoStaminaRequested; // if needed
    private readonly ClimbHandlerFactory factory;
    public ClimbingService(ClimbHandlerFactory factory) { this.factory = factory; }

    public void HandleClimbing(ClimbingContext ctx)
    {
        if (ctx == null) return;

        var handler = factory.GetHandler(ctx);
        // Bind external delegates so that the handler can request actions.
        // If the caller already set them, they will be overwritten here.
        ctx.RequestPullUp = (req) => OnPullUpRequested?.Invoke(req);
        ctx.RequestTransitionToFalling = () => OnTransitionToFallingRequested?.Invoke();
        ctx.RequestNoStamina = () => OnNoStaminaRequested?.Invoke();
        Debug.Log(handler);
        if (handler != activeHandler)
        {
            activeHandler = handler;
            // можешь вызвать OnEnter/OnExit у handler'ов при необходимости
        }

        if (activeHandler != null)
        {
            bool alive = activeHandler.Handle(ctx);
            if (!alive)
            {
                activeHandler = null;
            }
        }
    }
}


// Context object passed into ClimbingService for a single FixedUpdate
public class ClimbingContext
{
    public Rigidbody Rigidbody;
    public Transform Transform;
    public LayerMask ClimbableLayers;
    public float ClimbDetectionDistance;
    public float InputH;
    public float InputV;
    public bool JumpPressed;
    public float StaminaMoveCost;
    public float StaminaIdleCost;
    public float PlayerHeight = 0.7f;
    public float PullupCheckDistance = 1.5f;
    public FpsPlayerClimbing.ClimbingSubState ClimbingSubState;

    public ClimbableObject CurrentClimbable = null;   // null — обычная стена
    public bool IsClimbingObject => CurrentClimbable != null;

    // delegate for consuming stamina (returns true if consumption succeeded)
    public Func<float, bool> ConsumeStamina;

    // requests hooks to the orchestrator
    public Action RequestNoStamina;
    public Action RequestTransitionToFalling;
    public Action<PullUpRequest> RequestPullUp;
}
public class PullUpRequest { public Vector3 UpImpulse; public Vector3 ForwardImpulse; }
public class ClimbHandlerFactory
{
    private readonly WallClimbHandler wallHandler;
    private readonly ObjectClimbHandler objectHandler;
    private ClimbConfig climbConfig;


    public ClimbHandlerFactory(WallClimbHandler wallHandler, ObjectClimbHandler objectHandler)
    {
        this.wallHandler = wallHandler;
        this.objectHandler = objectHandler;
    }

    public IClimbHandler GetHandler(ClimbingContext ctx)
    {
        return ctx.IsClimbingObject ? objectHandler : wallHandler;
    }

}