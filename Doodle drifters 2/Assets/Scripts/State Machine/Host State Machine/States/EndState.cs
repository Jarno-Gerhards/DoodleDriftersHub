using UnityEngine;

public class EndState : IState
{
    public EndState()
    {
        
    }

    public void Enter(GameContext context)
    {
        Debug.Log("Game has ended.");
    }

    public void Exit(GameContext context)
    {
        
    }

    public void Update(GameContext context)
    {
        
    }

    public void FixedUpdate(GameContext context)
    {
        
    }
}