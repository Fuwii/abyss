using Game.Player.Movement.Climbing.WallClimbing;
using Game.Player.Movement.Ground;
using Game.Player.Stamina;
using UnityEngine;

namespace Game.Player.Movement.Climbing
{
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public class FpsPlayerClimbing : MonoBehaviour
    {
        public enum PlayerState
        {
            Walking,
            Falling,
            Climbing,
            NoStaminaFalling
        }

        public enum ClimbingSubState
        {
            Moving,
            Idle,
            Hanging
        }

        [Header("State")] public PlayerState state = PlayerState.Walking;
        public ClimbingSubState climbingState = ClimbingSubState.Moving;

        [Header("Movement Settings")]
        public float climbSpeed = 2f, descentSpeed = 6f, climbJumpForce = 5f, jumpForce = 5f;
        public float climbDetectionDistance = 0.5f, wallStickDistance = 0.05f;
        public float staminaClimbCost = 5f, staminaClimbIdleCost = 1f;

        [Header("Falling")] public float maxFallSpeed = 20f;

        [Header("References")]
        public Transform cameraTransform;
        public LayerMask climbableLayers;
        public FpsPlayerController fpsController;
        public PlayerStamina playerStamina;

        Rigidbody _rb;
        bool _prevGroundedDuringClimb;
        MovementService _movementService;
        ClimbingService _climbingService;
        Vector2 _inputMove, _inputLook;
        bool _jumpPressed, _leftClickHeld;
        const float MinWallAngleDeg = 70f, LookAlignmentThreshold = 0.7f;
        ClimbConfig _climbConfig;
        ClimbingContext _climbingContext;

        private void Start()
        {
            _rb = GetComponent<Rigidbody>();
            if (!cameraTransform && Camera.main) cameraTransform = Camera.main.transform;
            if (!fpsController) fpsController = GetComponent<FpsPlayerController>();
            if (!playerStamina) playerStamina = GetComponent<PlayerStamina>();

            EnsureClimbingContext();
            _climbConfig = FpsPlayerClimbingUtils.BuildClimbConfig(wallStickDistance, climbSpeed, descentSpeed);
            _movementService = new MovementService { JumpForce = jumpForce, MaxFallSpeed = maxFallSpeed };
            var climbingFactory = new ClimbHandlerFactory(new WallClimbHandler(_climbConfig), new ObjectClimbHandler(_climbConfig));
            _climbingService = new ClimbingService(climbingFactory);
            _climbingService.OnTransitionToFallingRequested += TransitionToFalling;
            state = PlayerState.Walking;
        }

        void OnEnable() =>
            FpsPlayerClimbingUtils.SubscribeInput(HandleMoveChanged, HandleLookChanged, HandleJumpPressed, HandleLeftClickStart, HandleLeftClickCancel);

        void OnDisable() =>
            FpsPlayerClimbingUtils.UnsubscribeInput(HandleMoveChanged, HandleLookChanged, HandleJumpPressed, HandleLeftClickStart, HandleLeftClickCancel);

        void HandleMoveChanged(Vector2 v) => _inputMove = Vector2.ClampMagnitude(v, 1f);
        void HandleLookChanged(Vector2 v) => _inputLook = v;
        void HandleJumpPressed() => _jumpPressed = true;
        void HandleLeftClickStart() => _leftClickHeld = true;
        void HandleLeftClickCancel() => _leftClickHeld = false;

        void Update()
        {
            if (state != PlayerState.Climbing) CheckForGrounded();
            if (_leftClickHeld && state != PlayerState.Climbing && CanClimb()) TransitionToClimbing();
            if (_jumpPressed && state == PlayerState.Climbing)
            {
                TransitionToFalling();
                _rb.AddForce(Vector3.up * climbJumpForce, ForceMode.VelocityChange);
            }
        }

        void FixedUpdate()
        {
            _jumpPressed = UnityEngine.Input.GetButtonDown("Jump");
            switch (state)
            {
                case PlayerState.Walking:
                    playerStamina?.RemoveClimbingDebuff();
                    var maybe = _movementService.HandleWalking(_rb, fpsController, _jumpPressed);
                    if (maybe.HasValue) state = maybe.Value;
                    if (UnityEngine.Input.GetMouseButtonDown(0) && CanClimb()) TransitionToClimbing();
                    break;
                case PlayerState.Falling:
                    var newState = _movementService.HandleFalling(_rb, fpsController, _jumpPressed, climbableLayers, transform, climbDetectionDistance);
                    if (newState.HasValue) state = newState.Value;
                    break;
                case PlayerState.NoStaminaFalling:
                    var ns = _movementService.HandleFalling(_rb, fpsController, _jumpPressed, climbableLayers, transform, climbDetectionDistance);
                    if (ns.HasValue) state = ns.Value;
                    if (playerStamina != null && playerStamina.GetCurrentStamina() > playerStamina.GetCurrentMaxBaseStamina() * 0.3f) state = PlayerState.Falling;
                    break;
                case PlayerState.Climbing:
                    EnsureClimbingContext();
                    var ctx = _climbingContext;
                    if (_climbingContext != null && _climbingContext.CurrentClimbable != null) ctx.CurrentClimbable = _climbingContext.CurrentClimbable;
                    _climbingService.HandleClimbing(ctx);
                    climbingState = ctx.ClimbingSubState;
                    if (!ctx.IsClimbingObject && _climbingContext != null) _climbingContext.CurrentClimbable = null;
                    break;
            }

            _rb.useGravity = state != PlayerState.Climbing;
            _jumpPressed = false;
        }

        bool CanClimb() => ClimbDetector.CanClimb(transform, cameraTransform, climbDetectionDistance, climbableLayers, MinWallAngleDeg, LookAlignmentThreshold);

        void CheckForGrounded()
        {
            if (state == PlayerState.Walking && !fpsController.IsGrounded()) state = PlayerState.Falling;
            else if (state == PlayerState.Falling && fpsController.IsGrounded()) state = PlayerState.Walking;
        }

        void TransitionToFalling()
        {
            StopClimbOnObject();
            if (fpsController != null) fpsController.enabled = true;
            state = PlayerState.Falling;
            if (fpsController != null && fpsController.IsGrounded()) state = PlayerState.Walking;
        }

        void TransitionToNoStaminaFalling()
        {
            if (fpsController != null) fpsController.enabled = true;
            state = PlayerState.NoStaminaFalling;
            if (fpsController != null && fpsController.IsGrounded()) state = PlayerState.Walking;
        }

        void TransitionToClimbing()
        {
            var prev = state;
            state = PlayerState.Climbing;
            climbingState = ClimbingSubState.Idle;
            _prevGroundedDuringClimb = fpsController != null && fpsController.IsGrounded();
            Debug.Log($"Transitioned from {prev} to CLIMBING");
        }

        void EnsureClimbingContext()
        {
            if (_climbingContext == null) _climbingContext = new ClimbingContext();
            FpsPlayerClimbingUtils.UpdateClimbingContext(_climbingContext, _rb, transform, climbableLayers, wallStickDistance, climbDetectionDistance, _inputMove, _jumpPressed, staminaClimbCost, staminaClimbIdleCost, climbingState, () => fpsController != null && fpsController.IsGrounded(), (amt) => playerStamina == null ? true : playerStamina.Consume(amt), TransitionToNoStaminaFalling);
        }

        public void StartClimbOnObject(ClimbableObject obj)
        {
            if (obj == null) return;
            if (_climbingContext == null) EnsureClimbingContext();
            _climbingContext.CurrentClimbable = obj;
            TransitionToClimbing();
        }

        public void StopClimbOnObject()
        {
            if (_climbingContext == null) EnsureClimbingContext();
            _climbingContext.CurrentClimbable = null;
        }
    }
}