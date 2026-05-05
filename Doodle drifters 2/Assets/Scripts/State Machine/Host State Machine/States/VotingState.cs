using UnityEngine;
using UnityEngine.UIElements;

public class VotingState : IState
{
    private UIDocument votingUI;
    private float voteTime = 10f;
    private float timeRemaining = 0f;
    public VotingState()
    {
        votingUI = GameObject.Find("HostVotingUI").GetComponent<UIDocument>();
        votingUI.rootVisualElement.style.display = DisplayStyle.None;
    }

    public void Enter(GameContext context)
    {
        votingUI.rootVisualElement.style.display = DisplayStyle.Flex;
        timeRemaining = voteTime;
    }

    public void Exit(GameContext context)
    {
        votingUI.rootVisualElement.style.display = DisplayStyle.None;
    }

    public void Update(GameContext context)
    {
        if (timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
        }
        else
        {
            context.stateMachine.SwitchState(StateMachine.GameStateType.Solution);
        }
    }

    public void FixedUpdate(GameContext context)
    {
        
    }
}