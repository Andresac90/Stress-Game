// AirborneState.cs
using UnityEngine;

public class AirborneState : IPlayerState
{
    PlayerController2D player;

    public void EnterState(PlayerController2D player)
    {
        this.player = player;
    }

    public void HandleInput()
    {
        // Horizontal air control
        float x = player.moveAction.action.ReadValue<Vector2>().x;
        if (x != 0f)
            player.velocity.x = Mathf.MoveTowards(player.velocity.x, x * player.maxSpeed,
                                                  player.acceleration * Time.deltaTime);
        else
            player.velocity.x = Mathf.MoveTowards(player.velocity.x, 0f,
                                                  player.deceleration * Time.deltaTime);

        // Fire / Cancel Hook
        if (player.fireAction.action.triggered)
            player.ToggleGrapple();
    }

    public void LogicUpdate()
    {
        // Custom gravity
        player.velocity.y -= player.gravity * Time.deltaTime;

        // Check landing
        if (player.isGrounded)
            player.SwitchState(player.groundedState);

        player.UpdatePointer();
    }

    public void ExitState() { }
}
