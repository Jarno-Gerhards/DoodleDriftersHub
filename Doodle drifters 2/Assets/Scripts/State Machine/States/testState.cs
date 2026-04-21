using UnityEngine;

public class testState : IState
{
    public testState()
    {
        // Constructor logic if needed
    }

    public void Enter()
    {
        Debug.Log("Entering test state.");
    }

    public void Exit()
    {
        Debug.Log("Exiting test state.");
    }

    public void Update()
    {
        Debug.Log("Updating test state.");
    }
}