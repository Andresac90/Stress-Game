// PlayerController2D.cs
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController2D : MonoBehaviour
{
    [Header("Movement Settings")]
    public float acceleration     = 50f;
    public float deceleration     = 50f;
    public float maxSpeed         = 8f;
    public float jumpForce        = 15f;
    public float gravity          = 40f;
    public Transform groundCheck;
    public float groundCheckRadius = 0.1f;
    public LayerMask groundLayer;

    [Header("Grapple Settings")]
    public float pointerDistance  = 1.5f;
    public float hookSpeed        = 25f;
    public float hookMaxDistance  = 12f;
    public float hookPullSpeed    = 20f;

    [Header("References")]
    public Transform hookOrigin;            // your pointer sprite
    public InputActionProperty moveAction;  // Vector2
    public InputActionProperty jumpAction;  // Button
    public InputActionProperty fireAction;  // Button
    public InputActionProperty pointerAction; // Vector2 screen pos

    // State machine
    [HideInInspector] public IPlayerState groundedState;
    [HideInInspector] public IPlayerState airborneState;
    [HideInInspector] public IPlayerState grapplingState;
    IPlayerState currentState;

    // Shared data
    [HideInInspector] public Vector2 velocity;
    [HideInInspector] public bool isGrounded;
    [HideInInspector] public Vector2 grapplePoint;
    [HideInInspector] public GrapplingHook2D currentHook;

    void Awake()
    {
        groundedState  = new GroundedState();
        airborneState  = new AirborneState();
        grapplingState = new GrapplingState();
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

        transform.position += (Vector3)(velocity * Time.deltaTime);
    }

    public void SwitchState(IPlayerState next)
    {
        currentState?.ExitState();
        currentState = next;
        currentState.EnterState(this);
    }

    public void CheckGround()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        if (isGrounded && velocity.y < 0f)
            velocity.y = 0f;
    }

    public void UpdatePointer()
    {
        Vector2 screenPos = pointerAction.action.ReadValue<Vector2>();
        Vector3 worldPos  = Camera.main.ScreenToWorldPoint(screenPos);
        Vector2 dir       = (worldPos - (Vector3)transform.position).normalized;

        hookOrigin.right    = dir;
        hookOrigin.position = (Vector2)transform.position + dir * pointerDistance;
    }

    /// <summary>
    /// Called by states when the user clicks to either fire or cancel.
    /// </summary>
    public void ToggleGrapple()
    {
        if (currentHook == null)
        {
            // Fire from pool
            currentHook = HookPool.Instance.GetHook();
            currentHook.transform.position = hookOrigin.position;
            currentHook.Initialize(hookOrigin.right, hookSpeed, hookMaxDistance);
            currentHook.OnHookHit += OnGrappleHit;
        }
        else
        {
            // Cancel / return to pool
            currentHook.Cancel();
            currentHook = null;
            // switch back to grounded or airborne
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
