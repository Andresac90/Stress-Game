// PlayerController2D.cs
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController2D : MonoBehaviour
{
    [Header("Movement Settings")]
    public float acceleration      = 50f;
    public float deceleration      = 50f;
    public float maxSpeed          = 8f;
    public float jumpForce         = 15f;
    public float gravity           = 40f;
    public Transform groundCheck;
    public float groundCheckRadius = 0.1f;
    public LayerMask groundLayer;

    [Header("Grapple Settings")]
    public float pointerDistance = 1.5f;
    public float hookSpeed       = 25f;
    public float hookMaxDistance = 12f;
    public float hookPullSpeed   = 20f;

    [Header("Input & References")]
    public InputActionProperty moveAction;     // Vector2
    public InputActionProperty jumpAction;     // Button
    public InputActionProperty fireAction;     // Button
    public InputActionProperty pointerAction;  // Vector2 screen pos
    public Transform hookOrigin;               // pointer origin
    public Animator animator;                  // your Animator
    public SpriteRenderer spriteRenderer;      // for flipping

    // State machine
    [HideInInspector] public IPlayerState groundedState;
    [HideInInspector] public IPlayerState airborneState;
    [HideInInspector] public IPlayerState grapplingState;
    private IPlayerState currentState;

    // Shared runtime data
    [HideInInspector] public Vector2 velocity;
    [HideInInspector] public bool    isGrounded;
    [HideInInspector] public Vector2 grapplePoint;
    [HideInInspector] public GrapplingHook2D currentHook;

    // Animator parameter hashes
    private int speedHash;
    private int isGroundedHash;
    public  int jumpHash;    // public so states can trigger

    void Awake()
    {
        // Setup states
        groundedState  = new GroundedState();
        airborneState  = new AirborneState();
        grapplingState = new GrapplingState();

        // Auto‑grab components if not assigned
        if (animator == null)       animator = GetComponent<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

        // Hash parameters
        speedHash      = Animator.StringToHash("Speed");
        isGroundedHash = Animator.StringToHash("IsGrounded");
        jumpHash       = Animator.StringToHash("Jump");
    }

    void Start()
    {
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

        // Apply physics
        transform.position += (Vector3)(velocity * Time.deltaTime);

        // Animate & flip
        UpdateAnimation();
    }

    public void SwitchState(IPlayerState next)
    {
        currentState?.ExitState();
        currentState = next;
        currentState.EnterState(this);
    }

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

    void UpdateAnimation()
    {
        // 4‑state Animator
        animator.SetFloat(speedHash,      Mathf.Abs(velocity.x));
        animator.SetBool(isGroundedHash, isGrounded);

        // Flip sprite
        if (velocity.x >  0.1f) spriteRenderer.flipX = false;
        else if (velocity.x < -0.1f) spriteRenderer.flipX = true;
    }

    public void UpdatePointer()
    {
        Vector2 screenPos = pointerAction.action.ReadValue<Vector2>();
        Vector3 worldPos  = Camera.main.ScreenToWorldPoint(screenPos);
        Vector2 dir       = (worldPos - (Vector3)transform.position).normalized;

        hookOrigin.right    = dir;
        hookOrigin.position = (Vector2)transform.position + dir * pointerDistance;
    }

    public void ToggleGrapple()
    {
        if (currentHook == null)
        {
            currentHook = HookPool.Instance.GetHook();
            currentHook.transform.position = hookOrigin.position;
            currentHook.Initialize(hookOrigin.right, hookSpeed, hookMaxDistance);
            currentHook.OnHookHit += OnGrappleHit;
            SoundManager.PlaySound(SoundType.GRAPPLING_HOOK);
        }
        else
        {
            currentHook.Cancel();
            currentHook = null;
            SwitchState(isGrounded ? groundedState : airborneState);
        }
    }

    void OnGrappleHit(Vector2 hitPoint)
    {
        grapplePoint = hitPoint;
        SwitchState(grapplingState);
        currentHook.OnHookHit -= OnGrappleHit;
    }
}
