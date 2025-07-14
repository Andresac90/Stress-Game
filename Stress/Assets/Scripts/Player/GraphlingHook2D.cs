// GrapplingHook2D.cs
using UnityEngine;
using System;

[RequireComponent(typeof(Collider2D))]
public class GrapplingHook2D : MonoBehaviour
{
    public event Action<Vector2> OnHookHit;

    Vector2 direction;
    float   speed;
    float   maxDistance;
    Vector2 startPos;
    bool    hasHooked = false;

    /// <summary>
    /// Called by the pool when launching this hook.
    /// </summary>
    public void Initialize(Vector2 dir, float spd, float maxDist)
    {
        direction   = dir;
        speed       = spd;
        maxDistance = maxDist;
        startPos    = transform.position;
        hasHooked   = false;
    }

    void Update()
    {
        if (hasHooked) return;

        transform.position += (Vector3)(direction * speed * Time.deltaTime);

        if (Vector2.Distance(startPos, transform.position) > maxDistance)
            EndHook();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var hookable = other.GetComponent<IHookable>();
        if (hookable != null)
        {
            hasHooked = true;
            Vector2 hitPoint = transform.position;
            hookable.OnHooked(hitPoint);
            OnHookHit?.Invoke(hitPoint);
            // leave the hook graphic in place if you like:
            // transform.SetParent(other.transform, worldPositionStays: true);
        }
    }

    void EndHook()
    {
        HookPool.Instance.ReturnHook(this);
    }

    /// <summary>
    /// Call this to cancel mid-flight or after retracting.
    /// </summary>
    public void Cancel()
    {
        hasHooked = true;
        HookPool.Instance.ReturnHook(this);
    }
}
