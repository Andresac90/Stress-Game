// GroundedState.cs
using UnityEngine;

public class GroundedState : IPlayerState
{
    PlayerController2D player;

    public void EnterState(PlayerController2D player) =>
        this.player = player;

    public void HandleInput()
    {
        float x = player.moveAction.action.ReadValue<Vector2>().x;
        if (x != 0f)
            player.velocity.x = Mathf.MoveTowards(
                player.velocity.x, x * player.maxSpeed,
                player.acceleration * Time.deltaTime
            );
        else
            player.velocity.x = Mathf.MoveTowards(
                player.velocity.x, 0f,
                player.deceleration * Time.deltaTime
            );

        if (player.jumpAction.action.triggered)
        {
            player.velocity.y = player.jumpForce;
            player.animator.SetTrigger(player.jumpHash);
            player.SwitchState(player.airborneState);
            SoundManager.PlaySound(SoundType.JUMP);
            return;
        }

        if (player.fireAction.action.triggered)
            player.ToggleGrapple();
    }

    public void LogicUpdate()
    {
        if (!player.isGrounded)
            player.SwitchState(player.airborneState);
        player.UpdatePointer();
    }

    public void ExitState() { }
}
