using Unity.VisualScripting;
using UnityEngine;

public class StateMachine : MonoBehaviour
{
    IState currentState;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        SwitchState(new testState());
    }

    void Start()
    {
        SwitchState(new testState());
    }

    // Update is called once per frame
    void Update()
    {
        currentState.Update();
    }

    public void SwitchState(IState newState)
    {
        if (currentState != null)
        {
            currentState.Exit();
        }

        currentState = newState;

        if (currentState != null)
        {
            currentState.Enter();
        }
    }
}