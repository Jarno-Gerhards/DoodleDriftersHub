using UnityEngine;
using UnityEngine.UIElements;

public class DrawingState : IState
{
    private StoryScreen storyUIScreen;
    private UIDocument storyUI;
    private Label drawPrompt;
    private Label timer;
    private float drawTime = 30f;
    private float timeRemaining = 0f;
    public DrawingState()
    {
        storyUIScreen = GameObject.Find("StoryPages").GetComponent<StoryScreen>();
        storyUI = GameObject.Find("StoryPages").GetComponent<UIDocument>();
        drawPrompt = storyUI.rootVisualElement.Q<Label>("DrawPrompt");
        timer = storyUI.rootVisualElement.Q<Label>("Timer");
    }

    public void Enter(GameContext context)
    {
        // Send signal to clients to start drawing phase

        drawPrompt.text = "Draw your object!";
        timeRemaining = drawTime;
    }

    public void Exit(GameContext context)
    {
        // Send signal to clients to end drawing phase

        timer.style.display = DisplayStyle.None;
        drawPrompt.style.display = DisplayStyle.None;
        storyUI.rootVisualElement.style.display = DisplayStyle.None;
    }

    public void Update(GameContext context)
    {
        if (timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
            storyUIScreen.UpdateTimer(timeRemaining);
        }
        else
        {
            context.stateMachine.SwitchState(StateMachine.GameStateType.Voting);
        }
    }

    public void FixedUpdate(GameContext context)
    {
        
    }
}