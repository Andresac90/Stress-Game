// GrapplingHook2D.cs
using UnityEngine;
using System;

/// <summary>
/// Simple kinematic hook projectile. Flies forward until it hits an IHookable
/// or exceeds max travel distance. When it latches, it STOPS at the hit point
/// and remains visible until explicitly canceled (so the player can reel in).
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class GrapplingHook2D : MonoBehaviour
{
    /// <summary>Raised when the hook latches onto a surface.</summary>
    public event Action<Vector2> OnHookHit;
    /// <summary>Raised when the hook ends (miss, timeout, or cancel).</summary>
    public event Action OnEnded;

    [Header("Latch Behavior")]
    [Tooltip("Parent the hook to the hit object so it follows moving platforms.")]
    [SerializeField] private bool parentToHit = true;
    [Tooltip("Disable the collider once latched to avoid repeated triggers.")]
    [SerializeField] private bool disableColliderWhenLatched = true;

    private Vector2 direction;
    private float speed;
    private float maxDistance;
    private Vector2 startPos;
    private bool isLatched; // NEW: stops movement but keeps visual active
    private bool hasEnded;

    private Collider2D col;

    /// <summary>
    /// Initialize and launch the hook. Called by the pool user.
    /// </summary>
    public void Initialize(Vector2 dir, float spd, float maxDist)
    {
        if (!col) col = GetComponent<Collider2D>();

        transform.SetParent(null, true); // reset parent on reuse

        direction = dir.normalized;
        speed = Mathf.Max(0f, spd);
        maxDistance = Mathf.Max(0f, maxDist);
        startPos = transform.position;
        isLatched = false;
        hasEnded = false;

        col.isTrigger = true;
        col.enabled = true;
    }

    void Update()
    {
        if (hasEnded || isLatched) return;

        transform.position += (Vector3)(direction * speed * Time.deltaTime);

        if (Vector2.Distance(startPos, transform.position) > maxDistance)
            EndHook();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasEnded || isLatched) return;

        // Support child colliders on complex hookable objects.
        var hookable = other.GetComponentInParent<IHookable>();
        if (hookable == null) return;

        isLatched = true; // freeze projectile in place
        Vector2 hitPoint = transform.position;

        // Notify target & player.
        hookable.OnHooked(hitPoint);
        OnHookHit?.Invoke(hitPoint);

        // Optional: stick to the platform so the hook moves with it.
        if (parentToHit)
            transform.SetParent(other.transform, true); // keep world position

        if (disableColliderWhenLatched && col)
            col.enabled = false;

        // IMPORTANT: we DO NOT EndHook() here so the visual remains until cancel.
    }

    /// <summary>
    /// Cancels or completes the hook and returns it to the pool.
    /// </summary>
    public void Cancel() => EndHook();

    private void EndHook()
    {
        if (hasEnded) return;
        hasEnded = true;

        // Notify listeners BEFORE returning to pool.
        OnEnded?.Invoke();

        // Clear subscribers to avoid stale references when reusing from pool.
        OnHookHit = null;
        OnEnded = null;

        // Reset parenting and collider for reuse.
        transform.SetParent(null, true);
        if (col) col.enabled = true;

        HookPool.Instance.ReturnHook(this);
    }
}