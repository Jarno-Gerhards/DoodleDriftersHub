using UnityEngine;
using UnityEngine.UIElements;

public class AdjustState : IState
{
    private UIDocument storyUI;
    private ScenarioManager scenarioManager;
    private Button drawButton;
    public AdjustState()
    {
        storyUI = GameObject.Find("StoryPages").GetComponent<UIDocument>();
        scenarioManager = GameObject.Find("ScenarioManager").GetComponent<ScenarioManager>();
        storyUI.rootVisualElement.style.display = DisplayStyle.None;
        drawButton = storyUI.rootVisualElement.Q<Button>("DrawButton");
    }

    public void Enter(GameContext context)
    {
        storyUI.rootVisualElement.style.display = DisplayStyle.Flex;
        drawButton.style.display = DisplayStyle.Flex;
        scenarioManager.GenerateAdjustedScene();
    }

    public void Exit(GameContext context)
    {
        
    }

    public void Update(GameContext context)
    {
        
    }

    public void FixedUpdate(GameContext context)
    {
        
    }
}