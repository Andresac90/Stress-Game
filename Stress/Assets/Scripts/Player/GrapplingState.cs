// GrapplingState.cs
using UnityEngine;

//Player is being pulled toward an active grapple point.
public class GrapplingState : IPlayerState
{
    private PlayerController2D player;

    public void EnterState(PlayerController2D player) => this.player = player;

    public void HandleInput()
    {
        if (player.fireAction.action.triggered)
            player.ToggleGrapple();
    }

    public void LogicUpdate()
    {
        Vector2 toTarget = player.grapplePoint - (Vector2)player.transform.position;
        float dist = toTarget.magnitude;
        if (dist > 0.001f)
        {
            Vector2 dir = toTarget / dist;
            player.velocity = dir * player.hookPullSpeed;
        }

        player.UpdatePointer();

        // Is it Close enough? Auto-cancel 
        if (dist < 0.3f && player.currentHook != null)
            player.ToggleGrapple();
    }

    public void ExitState() { }
}