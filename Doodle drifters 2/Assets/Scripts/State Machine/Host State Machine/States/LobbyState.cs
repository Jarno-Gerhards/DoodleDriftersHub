using UnityEngine;

public class LobbyState : IState
{
    public LobbyState()
    {
        // Constructor logic if needed
    }

    public void Enter(GameContext context)
    {
        Debug.Log("Entering lobby state.");
    }

    public void Exit(GameContext context)
    {
        Debug.Log("Exiting lobby state.");
    }

    public void Update(GameContext context)
    {
        Debug.Log("Updating lobby state.");
    }

    public void FixedUpdate(GameContext context)
    {
        Debug.Log("FixedUpdating lobby state");
    }
}