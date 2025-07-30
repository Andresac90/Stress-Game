// FallBox.cs
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class FallBox : MonoBehaviour
{
    [Tooltip("Optional: assign a custom respawn Transform (e.g., checkpoint)")]
    public Transform respawnPoint;

    void Reset()
    {
        // Make sure our collider is a trigger
        GetComponent<Collider2D>().isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Look up the PlayerController2D on this or a parent object
        var player = other.GetComponentInParent<PlayerController2D>();
        if (player == null)
            return;

        // If you've assigned a custom respawnPoint, use that
        if (respawnPoint != null)
            player.RespawnAt(respawnPoint);
        else
            player.Respawn();
    }
}
