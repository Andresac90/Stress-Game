// PlayerController2D.cs
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Central player controller handling 2D movement, jumping, grappling,
/// basic animation parameters, respawning, and integrated footstep SFX.
/// </summary>
[DisallowMultipleComponent]
public class PlayerController2D : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("How quickly the player accelerates horizontally.")]
    public float acceleration = 50f;
    [Tooltip("How quickly the player slows down when no input.")]
    public float deceleration = 50f;
    [Tooltip("Maximum horizontal speed.")]
    public float maxSpeed = 8f;
    [Tooltip("Initial vertical velocity when jumping.")]
    public float jumpForce = 15f;
    [Tooltip("Custom gravity applied per second (positive number).")]
    public float gravity = 40f;
    [Tooltip("Feet position used for ground checks.")]
    public Transform groundCheck;
    [Tooltip("Radius of the overlap circle used to detect ground.")]
    public float groundCheckRadius = 0.1f;
    [Tooltip("Layer(s) considered ground.")]
    public LayerMask groundLayer;

    [Header("Grapple Settings")]
    [Tooltip("How far from the player the hook origin sits (for visuals/aiming).")]
    public float pointerDistance = 1.5f;
    [Tooltip("Hook projectile travel speed.")]
    public float hookSpeed = 25f;
    [Tooltip("Maximum hook travel distance before auto-cancel.")]
    public float hookMaxDistance = 12f;
    [Tooltip("Speed at which the player is pulled toward the hook point.")]
    public float hookPullSpeed = 20f;

    [Header("Input & References")]
    [Tooltip("Input action providing horizontal move (x). Use a Vector2 binding.")]
    public InputActionProperty moveAction;
    [Tooltip("Input action for jumping (button).")]
    public InputActionProperty jumpAction;
    [Tooltip("Input action for grappling (button). Press to fire/cancel.")]
    public InputActionProperty fireAction;
    [Tooltip("Input action with screen-space pointer position (Vector2).")]
    public InputActionProperty pointerAction;
    [Tooltip("Transform where the hook spawns from and points to.")]
    public Transform hookOrigin;
    [Tooltip("Animator that owns parameters: Speed (float), IsGrounded (bool), Jump (trigger).")]
    public Animator animator;
    [Tooltip("SpriteRenderer used for left/right flip.")]
    public SpriteRenderer spriteRenderer;

    [Header("Footstep SFX")]
    [Tooltip("Enable/disable footstep playback from the controller.")]
    public bool enableFootsteps = true;
    [Tooltip("SFX enum to use for footsteps.")]
    public SoundType footstepSound = SoundType.FOOTSTEP;
    [Range(0f, 1f)]
    [Tooltip("Default volume for each footstep.")]
    public float footstepVolume = 0.8f;
    [Tooltip("How to handle overlapping steps.\nSkipIfPlaying avoids stacking; Cut will interrupt previous.")]
    public SoundManager.SoundOverlap footstepOverlap = SoundManager.SoundOverlap.SkipIfPlaying;
    [Tooltip("Normalized speed (0..1) below which no steps play.")]
    [Range(0f, 1f)] public float minSpeedForSteps = 0.15f;
    [Tooltip("Step interval (seconds) when walking (at minSpeedForSteps).")]
    public float intervalAtMinSpeed = 0.55f;
    [Tooltip("Step interval (seconds) when running (near max speed).")]
    public float intervalAtMaxSpeed = 0.28f;

    // --- State machine instances ---
    [HideInInspector] public IPlayerState groundedState;
    [HideInInspector] public IPlayerState airborneState;
    [HideInInspector] public IPlayerState grapplingState;
    private IPlayerState currentState;

    // --- Shared data used by states ---
    [HideInInspector] public Vector2 velocity;        // Current velocity (units/sec)
    [HideInInspector] public bool isGrounded;         // True when touching ground
    [HideInInspector] public Vector2 grapplePoint;    // Hook latch position
    [HideInInspector] public GrapplingHook2D currentHook; // Active hook instance

    // --- Respawn support ---
    private Vector3 startPosition;                    // Spawn point

    // --- Animator parameter hashes ---
    private int speedHash;
    private int isGroundedHash;
    public int jumpHash; // public so states can trigger it

    // --- Footstep runtime ---
    private float _footstepTimer;

    void Awake()
    {
        // Create states
        groundedState = new GroundedState();
        airborneState = new AirborneState();
        grapplingState = new GrapplingState();

        // Auto-assign common components if missing
        if (!animator) TryGetComponent(out animator);
        if (!spriteRenderer) TryGetComponent(out spriteRenderer);

        // Cache parameter ids (avoid string lookups every frame)
        speedHash = Animator.StringToHash("Speed");
        isGroundedHash = Animator.StringToHash("IsGrounded");
        jumpHash = Animator.StringToHash("Jump");
    }

    void Start()
    {
        startPosition = transform.position;
        SwitchState(groundedState);
    }

    void OnEnable()
    {
        moveAction.action.Enable();
        jumpAction.action.Enable();
        fireAction.action.Enable();
        pointerAction.action.Enable();
    }

    void OnDisable()
    {
        moveAction.action.Disable();
        jumpAction.action.Disable();
        fireAction.action.Disable();
        pointerAction.action.Disable();
    }

    void Update()
    {
        CheckGround();

        currentState.HandleInput();
        currentState.LogicUpdate();

        // Integrate velocity (character controller style).
        transform.position += (Vector3)(velocity * Time.deltaTime);

        UpdateAnimation();
        UpdateFootsteps(); // <-- play steps here
    }

    /// <summary>
    /// Switch to a new state, ensuring proper exit/enter calls.
    /// </summary>
    public void SwitchState(IPlayerState next)
    {
        currentState?.ExitState();
        currentState = next;
        currentState.EnterState(this);
    }

    /// <summary>
    /// Sets isGrounded via an overlap check. If grounded while falling,
    /// snap vertical velocity to zero to avoid sticky accumulation.
    /// </summary>
    private void CheckGround()
    {
        if (!groundCheck)
        {
            isGrounded = false;
            return;
        }

        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        if (isGrounded && velocity.y < 0f)
            velocity.y = 0f;
    }

    /// <summary>
    /// Updates animator parameters and flips the sprite based on horizontal motion.
    /// </summary>
    private void UpdateAnimation()
    {
        if (animator)
        {
            // If you prefer normalized speed for a Blend Tree, replace with:
            // float speedNorm = Mathf.Clamp01(velocity.magnitude / maxSpeed);
            // animator.SetFloat(speedHash, speedNorm);
            animator.SetFloat(speedHash, Mathf.Abs(velocity.x));
            animator.SetBool(isGroundedHash, isGrounded);
        }

        if (spriteRenderer)
        {
            if (velocity.x > 0.1f) spriteRenderer.flipX = false;
            else if (velocity.x < -0.1f) spriteRenderer.flipX = true;
        }
    }

    /// <summary>
    /// Footstep cadence scaled by current speed. Plays only when grounded and not grappling.
    /// </summary>
    private void UpdateFootsteps()
    {
        if (!enableFootsteps) { _footstepTimer = 0f; return; }
        if (!isGrounded) { _footstepTimer = 0f; return; }
        if (currentState == grapplingState) { _footstepTimer = 0f; return; }

        // Use total speed if you move in 4 directions; otherwise use Mathf.Abs(velocity.x)
        float speedNorm = Mathf.Clamp01(velocity.magnitude / Mathf.Max(0.001f, maxSpeed));
        if (speedNorm < minSpeedForSteps)
        {
            _footstepTimer = 0f; // reset so the next move triggers quickly
            return;
        }

        // Interpolate cadence between walk and run
        float interval = Mathf.Lerp(intervalAtMinSpeed, intervalAtMaxSpeed, Mathf.InverseLerp(minSpeedForSteps, 1f, speedNorm));

        if (_footstepTimer <= 0f)
        {
            SoundManager.PlaySound(footstepSound, footstepVolume, footstepOverlap);
            _footstepTimer = Mathf.Max(0.05f, interval);
        }
        else
        {
            _footstepTimer -= Time.deltaTime;
        }
    }

    /// <summary>
    /// Positions and orients the hook origin from the pointer input.
    /// </summary>
    public void UpdatePointer()
    {
        if (!hookOrigin) return;
        var cam = Camera.main;
        if (!cam) return;

        Vector2 screenPos = pointerAction.action.ReadValue<Vector2>();
        Vector3 worldPos = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, cam.nearClipPlane));
        Vector2 dir = ((Vector2)(worldPos - transform.position)).normalized;

        hookOrigin.right = dir;
        hookOrigin.position = (Vector2)transform.position + dir * pointerDistance;
    }

    /// <summary>
    /// Fires the hook if none is active; otherwise cancels the active hook.
    /// </summary>
    public void ToggleGrapple()
    {
        if (currentHook == null)
        {
            var hook = HookPool.Instance.GetHook();
            hook.transform.position = hookOrigin ? hookOrigin.position : transform.position;
            hook.OnHookHit += OnGrappleHit;
            hook.OnEnded += OnHookEnded; // track auto-cancel/end
            hook.Initialize(hookOrigin ? (Vector2)hookOrigin.right : Vector2.right, hookSpeed, hookMaxDistance);
            currentHook = hook;

            // Play immediately when firing (not on hit)
            SoundManager.PlaySound(SoundType.GRAPPLING_HOOK, 1f, SoundManager.SoundOverlap.Cut);
        }
        else
        {
            // This will invoke OnHookEnded → cleanup + state restore.
            currentHook.Cancel();
        }
    }

    /// <summary>
    /// Hook callback when a surface is latched.
    /// </summary>
    private void OnGrappleHit(Vector2 hitPoint)
    {
        grapplePoint = hitPoint;
        SwitchState(grapplingState);
    }

    /// <summary>
    /// Hook callback for any end-of-life (missed, exceeded distance, or cancel).
    /// Ensures references are released and state returns to locomotion.
    /// </summary>
    private void OnHookEnded()
    {
        if (currentHook != null)
        {
            currentHook.OnHookHit -= OnGrappleHit;
            currentHook.OnEnded -= OnHookEnded;
            currentHook = null;
        }

        if (currentState == grapplingState)
            SwitchState(isGrounded ? groundedState : airborneState);
    }

    /// <summary>Teleports the player back to the original spawn point and resets state.</summary>
    public void Respawn()
    {
        transform.position = startPosition;
        velocity = Vector2.zero;
        SwitchState(groundedState);
    }

    /// <summary>Teleports the player to a specified spawn point and resets state.</summary>
    public void RespawnAt(Transform spawnTransform)
    {
        if (!spawnTransform) return;
        startPosition = spawnTransform.position;
        Respawn();
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (groundCheck)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
#endif
}
