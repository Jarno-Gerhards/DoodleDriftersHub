using UnityEngine;
using UnityEngine.UIElements;

public class SolutionState : IState
{
    private UIDocument storyUI;
    private ScenarioManager scenarioManager;
    private Button nextSceneButton;
    private string solutionItem;
    public SolutionState()
    {
        storyUI = GameObject.Find("StoryPages").GetComponent<UIDocument>();
        scenarioManager = GameObject.Find("ScenarioManager").GetComponent<ScenarioManager>();
        storyUI.rootVisualElement.style.display = DisplayStyle.None;
        nextSceneButton = storyUI.rootVisualElement.Q<Button>("NextScene");
    }

    public void Enter(GameContext context)
    {
        solutionItem = scenarioManager.SolutionItem;
        storyUI.rootVisualElement.style.display = DisplayStyle.Flex;
        nextSceneButton.style.display = DisplayStyle.Flex;
        scenarioManager.GenerateSolution(solutionItem);
    }

    public void Exit(GameContext context)
    {
        nextSceneButton.style.display = DisplayStyle.None;
    }

    public void Update(GameContext context)
    {
        
    }

    public void FixedUpdate(GameContext context)
    {
        
    }
}