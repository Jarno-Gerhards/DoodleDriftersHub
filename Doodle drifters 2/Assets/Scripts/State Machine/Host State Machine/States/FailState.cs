using UnityEngine;
using UnityEngine.UIElements;

public class FailState : IState
{
    private UIDocument storyUI;
    private ScenarioManager scenarioManager;
    private Button adjustButton;
    private string solutionItem;
    public FailState()
    {
        storyUI = GameObject.Find("StoryPages").GetComponent<UIDocument>();
        scenarioManager = GameObject.Find("ScenarioManager").GetComponent<ScenarioManager>();
        adjustButton = storyUI.rootVisualElement.Q<Button>("AdjustButton");
    }

    public void Enter(GameContext context)
    {
        solutionItem = scenarioManager.SolutionItem;
        storyUI.rootVisualElement.style.display = DisplayStyle.Flex;
        adjustButton.style.display = DisplayStyle.Flex;
        scenarioManager.GenerateFailure(solutionItem);
    }

    public void Exit(GameContext context)
    {
        adjustButton.style.display = DisplayStyle.None;
    }

    public void Update(GameContext context)
    {
        
    }

    public void FixedUpdate(GameContext context)
    {
        
    }
}