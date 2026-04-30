using UnityEngine;

public interface IState
{
    void Enter(GameContext context);
    void Exit(GameContext context);
    void Update(GameContext context);
    void FixedUpdate(GameContext context);
}