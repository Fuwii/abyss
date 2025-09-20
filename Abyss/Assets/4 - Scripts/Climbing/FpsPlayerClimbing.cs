using System;
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

    [Header("Refs")]
    public Transform cameraTransform;
    public LayerMask climbableLayers;
    public FpsPlayerController fpsController;
    public PlayerStamina playerStamina;

    // internals
    private Rigidbody rb;
    private MovementStateHandler movementHandler;
    private ClimbingService climbingService;
    private ClimbConfig climbConfig;
    private ClimbingContext climbingContext;

    // input cache
    private float inputH, inputV;
    private bool jumpPressed, climbPressed;

    private const float MinWallAngleDeg = 70f;
    private const float LookAlignmentThreshold = 0.7f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (!cameraTransform && Camera.main) cameraTransform = Camera.main.transform;
        if (!fpsController) fpsController = GetComponent<FpsPlayerController>();
        if (!playerStamina) playerStamina = GetComponent<PlayerStamina>();

        BuildConfigAndServices();
    }

    private void Update()
    {
        inputH = Input.GetAxis("Horizontal");
        inputV = Input.GetAxis("Vertical");
        jumpPressed = Input.GetButtonDown("Jump");
        climbPressed = Input.GetMouseButtonDown(0);

        if (state != PlayerState.CLIMBING) CheckForGrounded();

        if (climbPressed && state != PlayerState.CLIMBING && CanClimb())
            EnterClimbing();

        if (jumpPressed && state == PlayerState.CLIMBING)
            ExitClimbingToFallingWithJump();
    }

    private void FixedUpdate()
    {
        // keep one-frame sampling
        jumpPressed = Input.GetButtonDown("Jump");

        // delegate walking/falling logic
        var newState = movementHandler.HandleState(state, rb, fpsController, jumpPressed, playerStamina, climbableLayers, transform, climbDetectionDistance);
        if (newState.HasValue) state = newState.Value;

        // climbing branch (delegated)
        if (state == PlayerState.CLIMBING)
        {
            EnsureClimbingContext();
            playerStamina?.ApplyClimbingDebuff(); // apply while in climbing
            climbingService.HandleClimbing(climbingContext);
            climbingState = climbingContext.ClimbingSubState;
            // climbingService can call RequestNoStamina -> we map it to TransitionToNoStamina below via context
            if (!climbingContext.IsClimbingObject && climbingContext.CurrentClimbable == null)
            {
                // nothing extra here (object climb cleared)
            }
        }

        rb.useGravity = state != PlayerState.CLIMBING;
        jumpPressed = false;
    }

    // transitions & helpers
    private void EnterClimbing()
    {
        var prev = state;
        state = PlayerState.CLIMBING;
        climbingState = ClimbingSubState.Idle;
        Debug.Log($"Transitioned from {prev} to CLIMBING");
    }

    private void ExitClimbingToFallingWithJump()
    {
        TransitionToFalling();
        rb.AddForce(Vector3.up * climbJumpForce, ForceMode.VelocityChange);
    }

    public void TransitionToFalling()
    {
        ExitClimbingCleanup();
        if (fpsController != null) fpsController.enabled = true;
        state = PlayerState.FALLING;
        if (fpsController != null && fpsController.IsGrounded()) state = PlayerState.WALKING;
    }

    public void TransitionToNoStaminaFalling()
    {
        ExitClimbingCleanup();
        if (fpsController != null) fpsController.enabled = true;
        state = PlayerState.NO_STAMINA_FALLING;
        if (fpsController != null && fpsController.IsGrounded()) state = PlayerState.WALKING;
    }

    private void ExitClimbingCleanup()
    {
        if (climbingContext == null) EnsureClimbingContext(); // ensure exists
        climbingContext.CurrentClimbable = null;
        playerStamina?.RemoveClimbingDebuff();
    }

    private bool CanClimb() => ClimbDetector.CanClimb(transform, cameraTransform, climbDetectionDistance, climbableLayers, MinWallAngleDeg, LookAlignmentThreshold);

    private void CheckForGrounded()
    {
        if (state == PlayerState.WALKING && !fpsController.IsGrounded()) state = PlayerState.FALLING;
        else if (state == PlayerState.FALLING && fpsController.IsGrounded()) state = PlayerState.WALKING;
    }

    private void BuildConfigAndServices()
    {
        climbConfig = new ClimbConfig();
        var wallHandler = new WallClimbHandler(climbConfig);
        var objHandler = new ObjectClimbHandler(climbConfig);
        var factory = new ClimbHandlerFactory(wallHandler, objHandler);
        climbingService = new ClimbingService(factory);
        climbingService.OnTransitionToFallingRequested += TransitionToFalling;

        movementHandler = new MovementStateHandler(this); // pass self for transitions & stamina checks
        EnsureClimbingContext();
    }

    private void EnsureClimbingContext()
    {
        if (climbingContext == null)
            climbingContext = new ClimbingContext();

        climbingContext.Rigidbody = rb;
        climbingContext.Transform = transform;
        climbingContext.ClimbableLayers = climbableLayers;
        climbingContext.ClimbDetectionDistance = climbDetectionDistance;
        climbingContext.WallStickDistance = wallStickDistance;

        climbingContext.InputH = inputH;
        climbingContext.InputV = inputV;
        climbingContext.JumpPressed = jumpPressed;
        climbingContext.StaminaMoveCost = staminaClimbCost;
        climbingContext.StaminaIdleCost = staminaClimbIdleCost;
        climbingContext.ClimbingSubState = climbingState;

        climbingContext.ConsumeStamina = (amt) => playerStamina == null ? true : playerStamina.Consume(amt);
        climbingContext.RequestNoStamina = () => TransitionToNoStaminaFalling();
    }

    // External object helpers
    public void StartClimbOnObject(ClimbableObject obj)
    {
        if (obj == null) return;
        if (climbingContext == null) EnsureClimbingContext();
        climbingContext.CurrentClimbable = obj;
        EnterClimbing();
        rb.linearVelocity = Vector3.zero;
    }

    public void StopClimbOnObject()
    {
        if (climbingContext == null) EnsureClimbingContext();
        climbingContext.CurrentClimbable = null;
    }
}
