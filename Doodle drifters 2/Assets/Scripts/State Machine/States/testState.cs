using UnityEngine;

public class testState : IState
{
    public testState()
    {
        // Constructor logic if needed
    }

    public void Enter(GameContext context)
    {
        Debug.Log("Entering test state.");
    }

    public void Exit(GameContext context)
    {
        Debug.Log("Exiting test state.");
    }

    public void Update(GameContext context)
    {
        Debug.Log("Updating test state.");
    }

    public void FixedUpdate(GameContext context)
    {
        Debug.Log("FixedUpdating test state");
    }
}