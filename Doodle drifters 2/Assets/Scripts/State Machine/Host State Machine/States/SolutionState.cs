using UnityEngine;
using UnityEngine.UIElements;

public class SolutionState : IState
{
    private UIDocument storyUI;
    private ScenarioManager scenarioManager;
    private Button nextSceneButton;
    private string solutionItem;
    private StoryScreen storyScreen;
    private VisualElement itemDisplay;
    public SolutionState()
    {
        storyUI = GameObject.Find("StoryPages").GetComponent<UIDocument>();
        scenarioManager = GameObject.Find("ScenarioManager").GetComponent<ScenarioManager>();
        storyScreen = GameObject.Find("StoryPages").GetComponent<StoryScreen>();
        storyUI.rootVisualElement.style.display = DisplayStyle.None;
        nextSceneButton = storyUI.rootVisualElement.Q<Button>("NextScene");
        itemDisplay = storyUI.rootVisualElement.Q<VisualElement>("ItemContainer");
    }

    public void Enter(GameContext context)
    {
        solutionItem = scenarioManager.SolutionItem;
        storyUI.rootVisualElement.style.display = DisplayStyle.Flex;
        nextSceneButton.style.display = DisplayStyle.Flex;
        scenarioManager.GenerateSolution(context.latestTitle);
        storyScreen.ShowChosenItem(context.latestTitle, context.latestDrawing);
        itemDisplay.style.display = DisplayStyle.Flex;
    }

    public void Exit(GameContext context)
    {
        nextSceneButton.style.display = DisplayStyle.None;
        itemDisplay.style.display = DisplayStyle.None;
    }

    public void Update(GameContext context)
    {
        
    }

    public void FixedUpdate(GameContext context)
    {
        
    }
}