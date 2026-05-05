using UnityEngine;
using UnityEngine.UIElements;

public class ThinkState : IState
{
    private StoryScreen storyUIScreen;
    private UIDocument storyUI;
    private Label thinkPrompt;
    private Label timer;
    private float thinkTime = 10f;
    private float timeRemaining = 0f;
    public ThinkState()
    {
        storyUIScreen = GameObject.Find("StoryPages").GetComponent<StoryScreen>();
        storyUI = GameObject.Find("StoryPages").GetComponent<UIDocument>();
        thinkPrompt = storyUI.rootVisualElement.Q<Label>("DrawPrompt");
        timer = storyUI.rootVisualElement.Q<Label>("Timer");
    }

    public void Enter(GameContext context)
    {
        thinkPrompt.style.display = DisplayStyle.Flex;
        thinkPrompt.text = "Think about what you want to draw!";
        timer.style.display = DisplayStyle.Flex;
        timeRemaining = thinkTime;
    }

    public void Exit(GameContext context)
    {
        
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
            context.stateMachine.SwitchState(StateMachine.GameStateType.Drawing);
        }
    }

    public void FixedUpdate(GameContext context)
    {
        
    }
}