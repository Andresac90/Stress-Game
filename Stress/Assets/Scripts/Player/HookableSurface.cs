// HookableSurface.cs
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class HookableSurface : MonoBehaviour, IHookable
{
    public void OnHooked(Vector2 hookPoint)
    {
        // Example reaction: log and spawn a little “hook” graphic
        Debug.Log($"Hook landed on {name} at {hookPoint}");
        // You could instantiate a small sprite here, parented to this transform, etc.
    }
}
