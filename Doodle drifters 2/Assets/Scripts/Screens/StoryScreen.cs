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
    private Button continueButton;
    void Awake()
    {
        document = GetComponent<UIDocument>();
    }
    void Start()
    {
        storyText = document.rootVisualElement.Q<Label>("StoryText");
        continueButton = document.rootVisualElement.Q<Button>("Continue");
        drawPrompt = document.rootVisualElement.Q<Label>("DrawPrompt");
        drawPrompt.style.display = DisplayStyle.None;
        timer = document.rootVisualElement.Q<Label>("Timer");
        timer.style.display = DisplayStyle.None;
        continueButton.clicked += OnContinueClick;
    }

    public void SetStoryText(string text)
    {
        storyText.text = text;
    }

    public void UpdateTimer(float seconds)
    {
        timer.text = seconds.ToString("F0");
    }

    private void OnContinueClick()
    {
        continueButton.style.display = DisplayStyle.None;
        stateMachine.SwitchState(StateMachine.GameStateType.Think);
    }

    private void OnDisable()
    {
        continueButton.clicked -= OnContinueClick;
    }
}
