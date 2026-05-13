using UnityEngine;
using UnityEngine.UIElements;

public class VotingState : IState
{
    private UIDocument votingUI;
    private Label timer;
    private float voteTime = 15f;
    private float timeRemaining = 0f;
    public VotingState()
    {
        votingUI = GameObject.Find("HostVotingUI").GetComponent<UIDocument>();
        timer = votingUI.rootVisualElement.Q<Label>("Timer");
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
            timer.text = timeRemaining.ToString("F0");
        }
        else
        {
            DecideOutcome(context);
        }
    }

    public void FixedUpdate(GameContext context)
    {
        
    }

    private void DecideOutcome(GameContext context)
    {
        int chance = Random.Range(0, 100);
        if (chance > 30)
        {
            Debug.Log("You Passed!");
            context.stateMachine.SwitchState(StateMachine.GameStateType.Solution);
        }
        else
        {
            Debug.Log("You Failed!");
            context.stateMachine.SwitchState(StateMachine.GameStateType.Fail);
        }
    }
}