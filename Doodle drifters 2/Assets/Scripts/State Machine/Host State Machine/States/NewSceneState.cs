using UnityEngine;
using UnityEngine.UIElements;

public class NewSceneState : IState
{
    private UIDocument storyUI;
    private ScenarioManager scenarioManager;
    private Button continueButton;
    public NewSceneState()
    {
        storyUI = GameObject.Find("StoryPages").GetComponent<UIDocument>();
        scenarioManager = GameObject.Find("ScenarioManager").GetComponent<ScenarioManager>();
        storyUI.rootVisualElement.style.display = DisplayStyle.None;
        continueButton = storyUI.rootVisualElement.Q<Button>("Continue");
    }

    public void Enter(GameContext context)
    {
        storyUI.rootVisualElement.style.display = DisplayStyle.Flex;
        continueButton.style.display = DisplayStyle.Flex;
        scenarioManager.GenerateNewScene();
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