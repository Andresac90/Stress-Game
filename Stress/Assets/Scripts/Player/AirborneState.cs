// AirborneState.cs
using UnityEngine;

//Player locomotion state while not grounded
public class AirborneState : IPlayerState
{
    private PlayerController2D player;

    public void EnterState(PlayerController2D player) => this.player = player;

    public void HandleInput()
    {
        float x = player.moveAction.action.ReadValue<Vector2>().x;

        if (Mathf.Abs(x) > 0f)
            player.velocity.x = Mathf.MoveTowards(player.velocity.x, x * player.maxSpeed, player.acceleration * Time.deltaTime);
        else
            player.velocity.x = Mathf.MoveTowards(player.velocity.x, 0f, player.deceleration * Time.deltaTime);

        if (player.fireAction.action.triggered)
            player.ToggleGrapple();
    }

    public void LogicUpdate()
    {
        player.velocity.y -= player.gravity * Time.deltaTime;

        if (player.isGrounded)
            player.SwitchState(player.groundedState);

        player.UpdatePointer();
    }

    public void ExitState() { }
}
