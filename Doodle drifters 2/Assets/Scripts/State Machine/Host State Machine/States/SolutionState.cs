using UnityEngine;

public class SolutionState : IState
{
    public SolutionState()
    {
        // Constructor logic if needed
    }

    public void Enter(GameContext context)
    {
        Debug.Log("Entering solution state.");
    }

    public void Exit(GameContext context)
    {
        Debug.Log("Exiting solution state.");
    }

    public void Update(GameContext context)
    {
        Debug.Log("Updating solution state.");
    }

    public void FixedUpdate(GameContext context)
    {
        Debug.Log("FixedUpdating solution state");
    }
}
