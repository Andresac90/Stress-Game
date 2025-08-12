// HookableSurface.cs
using UnityEngine;

//Simple example of a surface that accepts grappling hooks
[RequireComponent(typeof(Collider2D))]
public class HookableSurface : MonoBehaviour, IHookable
{
    public void OnHooked(Vector2 hookPoint)
    {
        Debug.Log($"Hook landed on {name} at {hookPoint}");
    }
}