public interface IPlayerState
{
    void EnterState(PlayerController2D player);
    void HandleInput();
    void LogicUpdate();
    void ExitState();
}
