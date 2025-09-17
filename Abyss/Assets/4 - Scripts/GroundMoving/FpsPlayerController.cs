using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
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

    private Rigidbody rb;
    private float yaw;
    private float pitch;
    [SerializeField] private bool isGrounded;

    // input state (подписываемся на события)
    private Vector2 inputMove = Vector2.zero;
    private Vector2 inputLook = Vector2.zero;
    private bool jumpPressed = false;
    private bool leftClickHeld = false;

    // components/services
    private LookHandler lookHandler;
    private GroundChecker groundChecker;
    private PlayerMover mover;
    private FpsPlayerClimbing climbingSystem;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.freezeRotation = true;

        if (!playerCamera) playerCamera = GetComponentInChildren<Camera>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        yaw = transform.eulerAngles.y;
        pitch = playerCamera ? playerCamera.transform.localEulerAngles.x : 0f;

        climbingSystem = GetComponent<FpsPlayerClimbing>();

        lookHandler = GetComponent<LookHandler>() ?? gameObject.AddComponent<LookHandler>();
        lookHandler.Setup(transform, playerCamera, mouseSensitivity, pitchMin, pitchMax);

        groundChecker = GetComponent<GroundChecker>() ?? gameObject.AddComponent<GroundChecker>();

        Debug.Log(groundChecker+"GROUNDCHECKER");
        groundChecker.Setup(groundCheck, groundMask, groundCheckDistance, groundCheckRadius, minGroundNormalY);

        var cfg = new PlayerMover.MovementConfig
        {
            MoveSpeed = moveSpeed,
            AirControlMul = airControlMul,
            MaxAccelGround = maxAccelGround,
            MaxAccelAir = maxAccelAir,
            MaxWalkableAngle = maxWalkableAngle,
            SlideSpeed = slideSpeed
        };
        mover = new PlayerMover(rb, cfg);

        if (groundCheck == null)
        {
            Transform check = transform.Find("GroundCheck");
            if (check != null)
            {
                groundCheck = check;
                groundChecker.groundCheck = groundCheck;
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
    }

    private void OnDisable()
    {
        InputManager.OnMoveAxisChanged -= HandleMoveChanged;
        InputManager.OnLookDeltaChanged -= HandleLookChanged;
        InputManager.OnJumpPressed -= HandleJumpPressed;
        InputManager.OnLeftClickStarted -= HandleLeftClickStart;
        InputManager.OnLeftClickCanceled -= HandleLeftClickCancel;
    }

    private void HandleMoveChanged(Vector2 v) => inputMove = Vector2.ClampMagnitude(v, 1f);
    private void HandleLookChanged(Vector2 v) => inputLook = v;
    private void HandleJumpPressed() => jumpPressed = true;
    private void HandleLeftClickStart() => leftClickHeld = true;
    private void HandleLeftClickCancel() => leftClickHeld = false;

    void Update()
    {
        // Look: передаём inputLook в LookHandler (там инверсия по Y уже сделана)
        lookHandler.ApplyLook(inputLook, ref yaw, ref pitch, climbingSystem);

        // Jump immediate (как в оригинале): Update может вызвать прыжок при isGrounded
        if (jumpPressed && isGrounded &&
            (climbingSystem == null || climbingSystem.state == FpsPlayerClimbing.PlayerState.WALKING))
        {
            Jump();
            // сброс флага — в FixedUpdate (чтобы имитировать ресэмплинг)
        }

        if (climbingSystem && climbingSystem.state != FpsPlayerClimbing.PlayerState.WALKING)
            return;
    }

    void FixedUpdate()
    {
        groundChecker.CheckGround(out isGrounded, out groundNormal);

        if (climbingSystem &&
            (climbingSystem.state == FpsPlayerClimbing.PlayerState.CLIMBING ||
             climbingSystem.state == FpsPlayerClimbing.PlayerState.NO_STAMINA_FALLING))
            return;

        rb.linearDamping = isGrounded ? groundDrag : 0f;

        Vector3 wishDir = (transform.forward * inputMove.y + transform.right * inputMove.x);
        if (wishDir.sqrMagnitude > 1f) wishDir.Normalize();

        Vector3 currVel = rb.linearVelocity;

        mover.ApplyMovement(wishDir, isGrounded, groundNormal, currVel, Time.fixedDeltaTime);

        // очистка одноразового события
        jumpPressed = false;
    }

    private void Jump()
    {
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        isGrounded = false;
    }

    public bool IsGrounded()
    {
        groundChecker.CheckGround(out bool g, out Vector3 n);
        return g;
    }
}
