using Core.Utils;
using Game.Player.Input;
using Game.Player.Movement.Climbing;
using UnityEngine;

namespace Game.Player.Movement.Ground
{
    [RequireComponent(typeof(Rigidbody))]
    public class FpsPlayerController : MonoBehaviour
    {
        [Header("Movement")]
        public float moveSpeed = 6f;
        [Tooltip("Максимальное изменение горизонтальной скорости (units/sec)")]
        public float airControlMul = 0.5f;
        public float jumpForce = 5f;
        public float groundDrag = 6f;
        public float maxAccelGround = 20f;
        public float maxAccelAir = 8f;
        public float maxWalkableAngle = 45f;
        public float slideSpeed = 6f;

        [Header("Ground Check")]
        public LayerMask groundMask;
        public Transform groundCheck;
        public float groundCheckDistance = 0.12f;
        public float groundCheckRadius = 0.25f;
        [Range(0f, 1f)] public float minGroundNormalY = 0.65f;
        public Vector3 groundNormal = Vector3.up;

        [Header("Look")]
        public Camera playerCamera;
        public float mouseSensitivity = 1.2f;
        public float pitchMin = -85f;
        public float pitchMax = 85f;

        private Rigidbody _rb;
        private float _yaw;
        private float _pitch;
        [SerializeField] private bool isGrounded;

        // input state 
        private Vector2 _inputMove = Vector2.zero;
        private Vector2 _inputLook = Vector2.zero;
        private bool _jumpPressed;
        private bool _leftClickHeld;

        // components/services
        private LookHandler _lookHandler;
        private GroundChecker _groundChecker;
        private PlayerMover _mover;
        private FpsPlayerClimbing _climbingSystem;
        [Header("Step")]
        public float stepHeight = 0.30f;
        public float minStepHeight = 0.006f;
        public float stepDuration = 0.38f;
        public float checkDistance = 0.4f;
        public float colliderRadius = 0.25f;
        public float colliderYOffset = -0.75f;
        public float lowOriginOffset = -0.22f; 


        private void Start()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.useGravity = true;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.freezeRotation = true;

            if (!playerCamera) playerCamera = GetComponentInChildren<Camera>();

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            _yaw = transform.eulerAngles.y;
            _pitch = playerCamera ? playerCamera.transform.localEulerAngles.x : 0f;

            _climbingSystem = GetComponent<FpsPlayerClimbing>();

            _lookHandler = GetComponent<LookHandler>() ?? gameObject.AddComponent<LookHandler>();
            _lookHandler.Setup(transform, playerCamera, mouseSensitivity, pitchMin, pitchMax);

            _groundChecker = GetComponent<GroundChecker>() ?? gameObject.AddComponent<GroundChecker>();

            Debug.Log(_groundChecker + "GROUNDCHECKER");
            _groundChecker.Setup(groundCheck, groundMask, groundCheckDistance, groundCheckRadius, minGroundNormalY);

            var cfg = new PlayerMover.MovementConfig
            {
                MoveSpeed = moveSpeed,
                AirControlMul = airControlMul,
                MaxAccelGround = maxAccelGround,
                MaxAccelAir = maxAccelAir,
                MaxWalkableAngle = maxWalkableAngle,
                SlideSpeed = slideSpeed,

                StepHeight = stepHeight,
                MinStepHeight = minStepHeight,
                StepDuration = stepDuration,
                CheckDistance = checkDistance,
                ColliderRadius = colliderRadius,
                ColliderYOffset = colliderYOffset,
                LowOriginOffset = lowOriginOffset,
                GroundMask = groundMask,
            };
            _mover = new PlayerMover(_rb, cfg);


            if (groundCheck == null)
            {
                var check = transform.Find("GroundCheck");
                if (check != null)
                {
                    groundCheck = check;
                    _groundChecker.groundCheck = groundCheck;
                    Debug.Log("GroundCheck automatically assigned");
                }
                else
                {
                    Debug.LogWarning("GroundCheck transform is not assigned! Ground detection will not work.");
                }
            }
        }

        private void OnEnable()
        {
            InputManager.OnMoveAxisChanged += HandleMoveChanged;
            InputManager.OnLookDeltaChanged += HandleLookChanged;
            InputManager.OnJumpPressed += HandleJumpPressed;
            InputManager.OnLeftClickStarted += HandleLeftClickStart;
            InputManager.OnLeftClickCanceled += HandleLeftClickCancel;

            playerCamera.SetActive(true);
        }

        private void OnDisable()
        {
            InputManager.OnMoveAxisChanged -= HandleMoveChanged;
            InputManager.OnLookDeltaChanged -= HandleLookChanged;
            InputManager.OnJumpPressed -= HandleJumpPressed;
            InputManager.OnLeftClickStarted -= HandleLeftClickStart;
            InputManager.OnLeftClickCanceled -= HandleLeftClickCancel;

            playerCamera.SetActive(false);
        }

        private void HandleMoveChanged(Vector2 v) => _inputMove = Vector2.ClampMagnitude(v, 1f);
        private void HandleLookChanged(Vector2 v) => _inputLook = v;
        private void HandleJumpPressed() => _jumpPressed = true;
        private void HandleLeftClickStart() => _leftClickHeld = true;
        private void HandleLeftClickCancel() => _leftClickHeld = false;

        private void Update()
        {
            // Look: 
            _lookHandler.ApplyLook(_inputLook, ref _yaw, ref _pitch, _climbingSystem);

            // Jump immediate
            if (_jumpPressed && isGrounded &&
                (_climbingSystem == null || _climbingSystem.state == FpsPlayerClimbing.PlayerState.Walking))
            {
                Jump();
            }

            if (_climbingSystem && _climbingSystem.state != FpsPlayerClimbing.PlayerState.Walking)
                return;
        }

        private void FixedUpdate()
        {
            _groundChecker.CheckGround(out isGrounded, out groundNormal);

            if (_climbingSystem &&
                (_climbingSystem.state == FpsPlayerClimbing.PlayerState.Climbing ||
                 _climbingSystem.state == FpsPlayerClimbing.PlayerState.NoStaminaFalling))
                return;

            //Не уверен что необходимо, нужны тесты
            //_rb.linearDamping = isGrounded ? groundDrag : 0f;

            var wishDir = (transform.forward * _inputMove.y + transform.right * _inputMove.x);
            if (wishDir.sqrMagnitude > 1f) wishDir.Normalize();

            var currVel = _rb.linearVelocity;

            _mover.ApplyMovement(wishDir, isGrounded, groundNormal, currVel, Time.fixedDeltaTime);

            _jumpPressed = false;
        }

        private void Jump()
        {
            _rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isGrounded = false;
        }

        public bool IsGrounded()
        {
            _groundChecker.CheckGround(out var g, out var n);
            return g;
        }


    }
}