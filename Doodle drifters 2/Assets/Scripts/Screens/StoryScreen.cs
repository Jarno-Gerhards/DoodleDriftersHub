using System;
using UnityEngine;
using UnityEngine.UIElements;

public class StoryScreen : MonoBehaviour
{
    public StateMachine stateMachine;
    private UIDocument document;
    private Label storyText;
    private Label drawPrompt;
    private Label timer;
    private Button drawButton;
    private Button nextSceneButton;
    private Button adjustButton;
    void Awake()
    {
        document = GetComponent<UIDocument>();
    }
    void Start()
    {
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
        nextSceneButton.style.display = DisplayStyle.None;
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
