using System;
using UnityEngine;
using UnityEngine.UIElements;

public class StoryScreen : MonoBehaviour
{
    public StateMachine stateMachine;
    private ScenarioManager scenarioManager;
    private UIDocument document;
    private Label storyText;
    private Label drawPrompt;
    private Label timer;
    private Button drawButton;
    private Button nextSceneButton;
    private Button adjustButton;
    private int gameLength;
    private int currentScene = 1;
    private bool finalScene = false;
    void Awake()
    {
        document = GetComponent<UIDocument>();
    }
    void Start()
    {
        scenarioManager = GameObject.Find("ScenarioManager").GetComponent<ScenarioManager>();
        storyText = document.rootVisualElement.Q<Label>("StoryText");
        drawButton = document.rootVisualElement.Q<Button>("DrawButton");
        adjustButton = document.rootVisualElement.Q<Button>("AdjustButton");
        adjustButton.style.display = DisplayStyle.None;
        nextSceneButton = document.rootVisualElement.Q<Button>("NextScene");
        nextSceneButton.style.display = DisplayStyle.None;
        drawPrompt = document.rootVisualElement.Q<Label>("DrawPrompt");
        drawPrompt.style.display = DisplayStyle.None;
        timer = document.rootVisualElement.Q<Label>("Timer");
        timer.style.display = DisplayStyle.None;
        drawButton.clicked += OnDrawClick;
        nextSceneButton.clicked += OnNextSceneClick;
        adjustButton.clicked += OnAdjustClick;
    }

    public void SetStoryText(string text)
    {
        storyText.text = text;
    }

    public void SetGameLength(int length)
    {
        gameLength = length;
        Debug.Log("Game length set to: " + gameLength);
    }

    public void UpdateTimer(float seconds)
    {
        timer.text = seconds.ToString("F0");
    }

    private void OnDrawClick()
    {
        drawButton.style.display = DisplayStyle.None;
        stateMachine.SwitchState(StateMachine.GameStateType.Think);
    }

    private void OnNextSceneClick()
    {
        currentScene++;
        nextSceneButton.style.display = DisplayStyle.None;
        if (currentScene >= gameLength && !finalScene)
        {
            finalScene = true;
            nextSceneButton.text = "End Game";
            stateMachine.SwitchState(StateMachine.GameStateType.Boss);
            return;
        }
        if (finalScene)
        {
            stateMachine.SwitchState(StateMachine.GameStateType.End);
            return;
        }
        stateMachine.SwitchState(StateMachine.GameStateType.NewScene);
    }

    private void OnAdjustClick()
    {
        adjustButton.style.display = DisplayStyle.None;
        stateMachine.SwitchState(StateMachine.GameStateType.Adjust);
    }

    private void OnDisable()
    {
        drawButton.clicked -= OnDrawClick;
        nextSceneButton.clicked -= OnNextSceneClick;
        adjustButton.clicked -= OnAdjustClick;
    }
}
