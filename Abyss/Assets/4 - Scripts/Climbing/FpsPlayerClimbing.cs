using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class FpsPlayerClimbing : MonoBehaviour
{
    public enum PlayerState { WALKING, FALLING, CLIMBING, NO_STAMINA_FALLING }
    public enum ClimbingSubState { Moving, Idle, Hanging }

    [Header("State")]
    public PlayerState state = PlayerState.WALKING;
    public ClimbingSubState climbingState = ClimbingSubState.Moving;

    [Header("Movement Settings")]
    public float climbSpeed = 2f;
    public float descentSpeed = 6f;
    public float climbJumpForce = 5f;
    public float jumpForce = 5f;
    public float climbDetectionDistance = 0.5f;
    public float wallStickDistance = 0.05f;
    public float staminaClimbCost = 5f;
    public float staminaClimbIdleCost = 1f;

    [Header("Falling")]
    public float maxFallSpeed = 20f;

    [Header("References")]
    public Transform cameraTransform;
    public LayerMask climbableLayers;
    public FpsPlayerController fpsController;
    public PlayerStamina playerStamina;

    private Rigidbody rb;

    private MovementService movementService;
    private ClimbingService climbingService;

    // input cache (от InputManager)
    private Vector2 rawMove = Vector2.zero;
    private bool jumpPressed = false;
    private bool leftClickHeld = false;

    private const float MinWallAngleDeg = 70f;
    private const float LookAlignmentThreshold = 0.7f;

    private ClimbConfig climbConfig;
    private ClimbingContext climbingContext;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (!cameraTransform && Camera.main) cameraTransform = Camera.main.transform;
        if (!fpsController) fpsController = GetComponent<FpsPlayerController>();
        if (!playerStamina) playerStamina = GetComponent<PlayerStamina>();

        BuildContext();
        BuildConfig();
        movementService = new MovementService { JumpForce = jumpForce, MaxFallSpeed = maxFallSpeed };
        var climbingFactory = new ClimbHandlerFactory(new WallClimbHandler(climbConfig), new ObjectClimbHandler(climbConfig));
        climbingService = new ClimbingService(climbingFactory);
        climbingService.OnTransitionToFallingRequested += () => TransitionToFalling();

        state = PlayerState.WALKING;
    }

    private void OnEnable()
    {
        InputManager.OnMoveAxisChanged += HandleMove;
        InputManager.OnJumpPressed += HandleJump;
        InputManager.OnLeftClickStarted += HandleLeftClickStart;
        InputManager.OnLeftClickCanceled += HandleLeftClickCancel;
    }

    private void OnDisable()
    {
        InputManager.OnMoveAxisChanged -= HandleMove;
        InputManager.OnJumpPressed -= HandleJump;
        InputManager.OnLeftClickStarted -= HandleLeftClickStart;
        InputManager.OnLeftClickCanceled -= HandleLeftClickCancel;
    }

    private void HandleMove(Vector2 v) => rawMove = v;
    private void HandleJump() => jumpPressed = true;
    private void HandleLeftClickStart() => leftClickHeld = true;
    private void HandleLeftClickCancel() => leftClickHeld = false;

    private void Update()
    {
        if (state != PlayerState.CLIMBING) CheckForGrounded();

        if (leftClickHeld && state != PlayerState.CLIMBING && CanClimb())
        {
            TransitionToClimbing();
        }

        if (jumpPressed && state == PlayerState.CLIMBING)
        {
            TransitionToFalling();
            rb.AddForce(Vector3.up * climbJumpForce, ForceMode.VelocityChange);
        }
    }

    private void FixedUpdate()
    {
        switch (state)
        {
            case PlayerState.WALKING:
                playerStamina?.RemoveClimbingDebuff();
                var maybe = movementService.HandleWalking(rb, fpsController, jumpPressed);
                if (maybe.HasValue) state = maybe.Value;
                if (leftClickHeld && CanClimb()) TransitionToClimbing();
                break;

            case PlayerState.FALLING:
                var newState = movementService.HandleFalling(rb, fpsController, jumpPressed, climbableLayers, transform, climbDetectionDistance);
                if (newState.HasValue) state = newState.Value;
                break;

            case PlayerState.NO_STAMINA_FALLING:
                var ns = movementService.HandleFalling(rb, fpsController, jumpPressed, climbableLayers, transform, climbDetectionDistance);
                if (ns.HasValue) state = ns.Value;
                if (playerStamina != null && playerStamina.GetCurrentStamina() > playerStamina.GetCurrentMaxBaseStamina() * 0.3f)
                {
                    state = PlayerState.FALLING;
                }
                break;

            case PlayerState.CLIMBING:
                var ctx = new ClimbingContext
                {
                    Rigidbody = rb,
                    Transform = transform,
                    ClimbableLayers = climbableLayers,
                    ClimbDetectionDistance = climbDetectionDistance,
                    InputH = rawMove.x,
                    InputV = rawMove.y,
                    JumpPressed = jumpPressed,
                    StaminaMoveCost = staminaClimbCost,
                    StaminaIdleCost = staminaClimbIdleCost,
                    ClimbingSubState = climbingState,
                    ConsumeStamina = (amt) => playerStamina == null ? true : playerStamina.Consume(amt),
                    RequestNoStamina = () => TransitionToNoStaminaFalling()
                };

                if (climbingContext != null && climbingContext.CurrentClimbable != null)
                {
                    ctx.CurrentClimbable = climbingContext.CurrentClimbable;
                }

                climbingService.HandleClimbing(ctx);
                climbingState = ctx.ClimbingSubState;

                if (!ctx.IsClimbingObject)
                {
                    if (climbingContext != null) climbingContext.CurrentClimbable = null;
                }
                break;
        }

        rb.useGravity = state != PlayerState.CLIMBING;
        jumpPressed = false;
    }

    private bool CanClimb() => ClimbDetector.CanClimb(transform, cameraTransform, climbDetectionDistance, climbableLayers, MinWallAngleDeg, LookAlignmentThreshold);

    private void CheckForGrounded()
    {
        if (state == PlayerState.WALKING && !fpsController.IsGrounded())
            state = PlayerState.FALLING;
        else if (state == PlayerState.FALLING && fpsController.IsGrounded())
            state = PlayerState.WALKING;
    }

    private void TransitionToFalling()
    {
        StopClimbOnObject();
        if (fpsController != null) fpsController.enabled = true;
        state = PlayerState.FALLING;
        if (fpsController != null && fpsController.IsGrounded())
        {
            state = PlayerState.WALKING;
        }
    }

    private void TransitionToNoStaminaFalling()
    {
        if (fpsController != null) fpsController.enabled = true;
        state = PlayerState.NO_STAMINA_FALLING;
        if (fpsController != null && fpsController.IsGrounded())
        {
            state = PlayerState.WALKING;
        }
    }

    private void TransitionToClimbing()
    {
        var prev = state;
        state = PlayerState.CLIMBING;
        climbingState = ClimbingSubState.Idle;
        Debug.Log($"Transitioned from {prev} to CLIMBING");
    }

    private void BuildContext()
    {
        var ctx = new ClimbingContext
        {
            Rigidbody = rb,
            Transform = transform,
            ClimbableLayers = climbableLayers,
            ClimbDetectionDistance = climbDetectionDistance,
            InputH = rawMove.x,
            InputV = rawMove.y,
            JumpPressed = jumpPressed,
            StaminaMoveCost = staminaClimbCost,
            StaminaIdleCost = staminaClimbIdleCost,
            ClimbingSubState = climbingState,
            ConsumeStamina = (amt) => playerStamina == null ? true : playerStamina.Consume(amt),
            RequestNoStamina = () => TransitionToNoStaminaFalling()
        };
        climbingContext = ctx;
    }

    private void BuildConfig()
    {
        climbConfig = new ClimbConfig();
    }

    public void StartClimbOnObject(ClimbableObject obj)
    {
        if (obj == null) return;
        if (climbingContext == null) BuildContext();
        climbingContext.CurrentClimbable = obj;
        TransitionToClimbing();
        rb.linearVelocity = Vector3.zero;
    }

    public void StopClimbOnObject()
    {
        if (climbingContext == null) BuildContext();
        climbingContext.CurrentClimbable = null;
    }
}
