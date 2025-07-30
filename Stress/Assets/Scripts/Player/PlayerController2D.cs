// PlayerController2D.cs
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles player movement, jumping, grappling hook, animation, and respawn logic.
/// </summary>
public class PlayerController2D : MonoBehaviour
{
    [Header("Movement Settings")]
    public float acceleration = 50f;      // How quickly the player accelerates horizontally
    public float deceleration = 50f;      // How quickly the player slows down when no input
    public float maxSpeed = 8f;       // Maximum horizontal speed
    public float jumpForce = 15f;      // Initial vertical velocity when jumping
    public float gravity = 40f;      // Custom gravity applied per second
    public Transform groundCheck;              // Empty Transform used to detect feet position
    public float groundCheckRadius = 0.1f;     // Radius of the ground‑check overlap circle
    public LayerMask groundLayer;              // Layer(s) considered “ground”

    [Header("Grapple Settings")]
    public float pointerDistance = 1.5f;      // How far from player the pointer sits
    public float hookSpeed = 25f;       // Travel speed of the hook projectile
    public float hookMaxDistance = 12f;       // Max travel distance before hook retracts
    public float hookPullSpeed = 20f;       // Speed at which player is pulled in

    [Header("Input & References")]
    public InputActionProperty moveAction;     // Maps to a Vector2 (x = horizontal)
    public InputActionProperty jumpAction;     // Button press to jump
    public InputActionProperty fireAction;     // Button press to fire/cancel grapple
    public InputActionProperty pointerAction;  // Mouse / stick position for hook aim
    public Transform hookOrigin;               // Where the hook spawns from
    public Animator animator;                  // Controls animation states
    public SpriteRenderer spriteRenderer;      // For flipping sprite left/right

    // State machine instances
    [HideInInspector] public IPlayerState groundedState;
    [HideInInspector] public IPlayerState airborneState;
    [HideInInspector] public IPlayerState grapplingState;
    private IPlayerState currentState;

    // Shared data used by all states
    [HideInInspector] public Vector2 velocity;      // Current velocity
    [HideInInspector] public bool isGrounded;    // True if player is touching ground
    [HideInInspector] public Vector2 grapplePoint;  // Point where hook latched
    [HideInInspector] public GrapplingHook2D currentHook;// Reference to active hook

    // Used for respawning
    private Vector3 startPosition;                 // Where player returns on death/fall

    // Animator parameter hashes for efficiency
    private int speedHash;
    private int isGroundedHash;
    public int jumpHash;    // public so states can trigger it

    void Awake()
    {
        // Instantiate our three possible states
        groundedState = new GroundedState();
        airborneState = new AirborneState();
        grapplingState = new GrapplingState();

        // Auto‑assign Animator & SpriteRenderer if not set in Inspector
        if (animator == null) animator = GetComponent<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

        // Create fast integer hashes for animation parameters
        speedHash = Animator.StringToHash("Speed");
        isGroundedHash = Animator.StringToHash("IsGrounded");
        jumpHash = Animator.StringToHash("Jump");
    }

    void Start()
    {
        // Record the player's initial spawn position
        startPosition = transform.position;
        // Begin in the grounded state
        SwitchState(groundedState);
    }

    void OnEnable()
    {
        // Enable all input actions
        moveAction.action.Enable();
        jumpAction.action.Enable();
        fireAction.action.Enable();
        pointerAction.action.Enable();
    }

    void OnDisable()
    {
        // Disable actions to avoid stray input callbacks
        moveAction.action.Disable();
        jumpAction.action.Disable();
        fireAction.action.Disable();
        pointerAction.action.Disable();
    }

    void Update()
    {
        // Check if the player is on the ground
        CheckGround();
        // Let the current state process inputs
        currentState.HandleInput();
        // Let the current state update logic (gravity, checks, etc.)
        currentState.LogicUpdate();

        // Apply velocity to position
        transform.position += (Vector3)(velocity * Time.deltaTime);

        // Update animation parameters and sprite flip
        UpdateAnimation();
    }

    /// <summary>
    /// Transitions to a new state, calling exit and enter hooks.
    /// </summary>
    public void SwitchState(IPlayerState next)
    {
        currentState?.ExitState();
        currentState = next;
        currentState.EnterState(this);
    }

    /// <summary>
    /// Performs a circle overlap at the groundCheck to set isGrounded.
    /// Zeroes downward velocity when touching.
    /// </summary>
    void CheckGround()
    {
        isGrounded = Physics2D.OverlapCircle(
            groundCheck.position,
            groundCheckRadius,
            groundLayer
        );
        if (isGrounded && velocity.y < 0f)
            velocity.y = 0f;
    }

    /// <summary>
    /// Updates animator float/bool parameters and flips the 
    /// sprite based on horizontal movement direction.
    /// </summary>
    void UpdateAnimation()
    {
        animator.SetFloat(speedHash, Mathf.Abs(velocity.x));
        animator.SetBool(isGroundedHash, isGrounded);

        if (velocity.x > 0.1f) spriteRenderer.flipX = false;
        else if (velocity.x < -0.1f) spriteRenderer.flipX = true;
    }

    /// <summary>
    /// Positions and orients the hook origin based on pointer input.
    /// </summary>
    public void UpdatePointer()
    {
        Vector2 screenPos = pointerAction.action.ReadValue<Vector2>();
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPos);
        Vector2 dir = (worldPos - (Vector3)transform.position).normalized;

        hookOrigin.right = dir;
        hookOrigin.position = (Vector2)transform.position + dir * pointerDistance;
    }

    /// <summary>
    /// Fires or cancels the grappling hook.
    /// </summary>
    public void ToggleGrapple()
    {
        if (currentHook == null)
        {
            // Spawn or reuse a hook and initialize it
            currentHook = HookPool.Instance.GetHook();
            currentHook.transform.position = hookOrigin.position;
            currentHook.Initialize(hookOrigin.right, hookSpeed, hookMaxDistance);
            currentHook.OnHookHit += OnGrappleHit;
            SoundManager.PlaySound(SoundType.GRAPPLING_HOOK);
        }
        else
        {
            // Cancel and return to pool
            currentHook.Cancel();
            currentHook = null;
            // Switch back to grounded or airborne
            SwitchState(isGrounded ? groundedState : airborneState);
        }
    }

    /// <summary>
    /// Hook callback: save hit point and enter grappling state.
    /// </summary>
    void OnGrappleHit(Vector2 hitPoint)
    {
        grapplePoint = hitPoint;
        SwitchState(grapplingState);
        currentHook.OnHookHit -= OnGrappleHit;
    }

    /// <summary>
    /// Teleports back to the recorded startPosition and resets velocity/state.
    /// </summary>
    public void Respawn()
    {
        transform.position = startPosition;
        velocity = Vector2.zero;
        SwitchState(groundedState);
    }

    /// <summary>
    /// Teleports to a custom respawnPoint and updates startPosition.
    /// Useful for checkpoints.
    /// </summary>
    public void RespawnAt(Transform spawnTransform)
    {
        startPosition = spawnTransform.position;
        Respawn();
    }
}
