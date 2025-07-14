// GrapplingState.cs
using UnityEngine;

public class GrapplingState : IPlayerState
{
    PlayerController2D player;

    public void EnterState(PlayerController2D player)
    {
        this.player = player;
    }

    public void HandleInput()
    {
        // Allow cancelling mid-pull
        if (player.fireAction.action.triggered)
            player.ToggleGrapple();
    }

    public void LogicUpdate()
    {
        // Pull the player toward the point
        Vector2 toTarget = player.grapplePoint - (Vector2)player.transform.position;
        float   dist     = toTarget.magnitude;
        Vector2 dir      = toTarget / dist;

        player.velocity = dir * player.hookPullSpeed;
        player.UpdatePointer();

        if (dist < 0.3f)
            player.ToggleGrapple();  // auto-cancel when close
    }

    public void ExitState() { }
}
