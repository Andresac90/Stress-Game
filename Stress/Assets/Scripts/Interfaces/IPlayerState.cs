public interface IPlayerState
{
    //Called immediately upon entering this state
    void EnterState(PlayerController2D player);
    //Read input for this frame
    void HandleInput();
    //Per-frame logic (movement, timers, checks)
    void LogicUpdate();
    //Called immediately before leaving this state
    void ExitState();
}
