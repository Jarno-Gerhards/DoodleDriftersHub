using UnityEngine;

public class TempButtonScript : MonoBehaviour
{
    [SerializeField] StateMachine sm;

    public void TestState()
    {
        sm.SwitchState(StateMachine.GameStateType.Think);
    }

    public void LobbyState()
    {

        sm.SwitchState(StateMachine.GameStateType.Lobby);
    }
}