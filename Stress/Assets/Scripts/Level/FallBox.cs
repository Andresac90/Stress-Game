// FallBox.cs
using UnityEngine;

// Respawns the player when they enter this trigger (e.g., death pits).
[RequireComponent(typeof(Collider2D))]
public class FallBox : MonoBehaviour
{
    [Tooltip("Optional: override respawn location (e.g., checkpoint)")]
    public Transform respawnPoint;

    void Reset()
    {
        // Ensure the collider acts as a trigger volume.
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Support child colliders on the player hierarchy.
        var player = other.GetComponentInParent<PlayerController2D>();
        if (player == null) return;

        if (respawnPoint != null)
            player.RespawnAt(respawnPoint);
        else
            player.Respawn();
    }
}