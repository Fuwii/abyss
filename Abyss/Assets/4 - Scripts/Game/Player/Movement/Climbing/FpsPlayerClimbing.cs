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

        [Header("State")]
        public PlayerState state = PlayerState.Walking;
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
        private Rigidbody _rb;
        private MovementStateHandler _movementHandler;
        private ClimbingService _climbingService;
        private ClimbConfig _climbConfig;
        private ClimbingContext _climbingContext;

        // input cache
        private float _inputH, _inputV;
        private bool _jumpPressed, _climbPressed;

        private const float MinWallAngleDeg = 70f;
        private const float LookAlignmentThreshold = 0.7f;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            if (!cameraTransform && Camera.main) cameraTransform = Camera.main.transform;
            if (!fpsController) fpsController = GetComponent<FpsPlayerController>();
            if (!playerStamina) playerStamina = GetComponent<PlayerStamina>();

            BuildConfigAndServices();
        }

        private void Update()
        {
            _inputH = UnityEngine.Input.GetAxis("Horizontal");
            _inputV = UnityEngine.Input.GetAxis("Vertical");
            _jumpPressed = UnityEngine.Input.GetButtonDown("Jump");
            _climbPressed = UnityEngine.Input.GetMouseButtonDown(0);

            if (state != PlayerState.Climbing) CheckForGrounded();

            if (_climbPressed && state != PlayerState.Climbing && CanClimb())
                EnterClimbing();

            if (_jumpPressed && state == PlayerState.Climbing)
                ExitClimbingToFallingWithJump();
        }

        private void FixedUpdate()
        {
            // keep one-frame sampling
            _jumpPressed = UnityEngine.Input.GetButtonDown("Jump");

            // delegate walking/falling logic
            var newState = _movementHandler.HandleState(state, _rb, fpsController, _jumpPressed, playerStamina, climbableLayers, transform, climbDetectionDistance);
            if (newState.HasValue) state = newState.Value;

            // climbing branch (delegated)
            if (state == PlayerState.Climbing)
            {
                EnsureClimbingContext();
                playerStamina?.ApplyClimbingDebuff(); // apply while in climbing
                _climbingService.HandleClimbing(_climbingContext);
                climbingState = _climbingContext.ClimbingSubState;

                // climbingService can call RequestNoStamina -> we map it to TransitionToNoStamina below via context
                if (!_climbingContext.IsClimbingObject && _climbingContext.CurrentClimbable == null)
                {
                    // nothing extra here (object climb cleared)
                }
            }

            _rb.useGravity = state != PlayerState.Climbing;
            _jumpPressed = false;
        }

        // transitions & helpers
        private void EnterClimbing()
        {
            var prev = state;
            state = PlayerState.Climbing;
            climbingState = ClimbingSubState.Idle;
            Debug.Log($"Transitioned from {prev} to CLIMBING");
        }

        private void ExitClimbingToFallingWithJump()
        {
            TransitionToFalling();
            _rb.AddForce(Vector3.up * climbJumpForce, ForceMode.VelocityChange);
        }

        public void TransitionToFalling()
        {
            ExitClimbingCleanup();
            if (fpsController != null) fpsController.enabled = true;
            state = PlayerState.Falling;
            if (fpsController != null && fpsController.IsGrounded()) state = PlayerState.Walking;
        }

        public void TransitionToNoStaminaFalling()
        {
            ExitClimbingCleanup();
            if (fpsController != null) fpsController.enabled = true;
            state = PlayerState.NoStaminaFalling;
            if (fpsController != null && fpsController.IsGrounded()) state = PlayerState.Walking;
        }

        private void ExitClimbingCleanup()
        {
            if (_climbingContext == null) EnsureClimbingContext(); // ensure exists
            _climbingContext.CurrentClimbable = null;
            playerStamina?.RemoveClimbingDebuff();
        }

        private bool CanClimb() => ClimbDetector.CanClimb(transform, cameraTransform, climbDetectionDistance, climbableLayers, MinWallAngleDeg, LookAlignmentThreshold);

        private void CheckForGrounded()
        {
            if (state == PlayerState.Walking && !fpsController.IsGrounded()) state = PlayerState.Falling;
            else if (state == PlayerState.Falling && fpsController.IsGrounded()) state = PlayerState.Walking;
        }

        private void BuildConfigAndServices()
        {
            _climbConfig = new ClimbConfig();
            var wallHandler = new WallClimbHandler(_climbConfig);
            var objHandler = new ObjectClimbHandler(_climbConfig);
            var factory = new ClimbHandlerFactory(wallHandler, objHandler);
            _climbingService = new ClimbingService(factory);
            _climbingService.OnTransitionToFallingRequested += TransitionToFalling;

            _movementHandler = new MovementStateHandler(this); // pass self for transitions & stamina checks
            EnsureClimbingContext();
        }

        private void EnsureClimbingContext()
        {
            if (_climbingContext == null)
                _climbingContext = new ClimbingContext();

            _climbingContext.Rigidbody = _rb;
            _climbingContext.Transform = transform;
            _climbingContext.ClimbableLayers = climbableLayers;
            _climbingContext.ClimbDetectionDistance = climbDetectionDistance;
            _climbingContext.WallStickDistance = wallStickDistance;

            _climbingContext.InputH = _inputH;
            _climbingContext.InputV = _inputV;
            _climbingContext.JumpPressed = _jumpPressed;
            _climbingContext.StaminaMoveCost = staminaClimbCost;
            _climbingContext.StaminaIdleCost = staminaClimbIdleCost;
            _climbingContext.ClimbingSubState = climbingState;

            _climbingContext.ConsumeStamina = (amt) => playerStamina == null ? true : playerStamina.Consume(amt);
            _climbingContext.RequestNoStamina = () => TransitionToNoStaminaFalling();
        }

        // External object helpers
        public void StartClimbOnObject(ClimbableObject obj)
        {
            if (obj == null) return;
            if (_climbingContext == null) EnsureClimbingContext();
            _climbingContext.CurrentClimbable = obj;
            EnterClimbing();
            _rb.linearVelocity = Vector3.zero;
        }

        public void StopClimbOnObject()
        {
            if (_climbingContext == null) EnsureClimbingContext();
            _climbingContext.CurrentClimbable = null;
        }
    }
}