// GroundedState.cs
using UnityEngine;

public class GroundedState : IPlayerState
{
    PlayerController2D player;

    public void EnterState(PlayerController2D player)
    {
        this.player = player;
    }

    public void HandleInput()
    {
        // Horizontal movement
        float x = player.moveAction.action.ReadValue<Vector2>().x;
        if (x != 0f)
        {
            player.velocity.x = Mathf.MoveTowards(player.velocity.x, x * player.maxSpeed, player.acceleration * Time.deltaTime);
        }
        else
        {
            player.velocity.x = Mathf.MoveTowards(player.velocity.x, 0f, player.deceleration * Time.deltaTime);
        }

        // Jump
        if (player.jumpAction.action.triggered)
        {
            player.velocity.y = player.jumpForce;
            player.SwitchState(player.airborneState);

            // Play jump sound
            SoundManager.PlaySound(SoundType.JUMP);
            return;
        }

        // Fire / Cancel Hook
        if (player.fireAction.action.triggered)
        {
            //Play sound
            SoundManager.PlaySound(SoundType.GRAPPLING_HOOK);
            player.ToggleGrapple();
        }
            
    }

    public void LogicUpdate()
    {
        // Landing clamp
        if (!player.isGrounded)
            player.SwitchState(player.airborneState);

        // Always update pointer
        player.UpdatePointer();
    }

    public void ExitState() { }
}
