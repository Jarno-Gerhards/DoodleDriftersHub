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
    private Button nextSceneButton;
    private VisualElement itemDisplay;
    private Label itemName;
    private VisualElement itemImage;
    void Awake()
    {
        document = GetComponent<UIDocument>();
    }
    void Start()
    {
        storyText = document.rootVisualElement.Q<Label>("StoryText");
        continueButton = document.rootVisualElement.Q<Button>("Continue");
        nextSceneButton = document.rootVisualElement.Q<Button>("NextScene");
        nextSceneButton.style.display = DisplayStyle.None;
        drawPrompt = document.rootVisualElement.Q<Label>("DrawPrompt");
        drawPrompt.style.display = DisplayStyle.None;
        timer = document.rootVisualElement.Q<Label>("Timer");
        timer.style.display = DisplayStyle.None;
        continueButton.clicked += OnContinueClick;
        nextSceneButton.clicked += OnNextSceneClick;
        itemDisplay = document.rootVisualElement.Q<VisualElement>("ItemContainer");
        itemName = document.rootVisualElement.Q<Label>("ItemName");
        itemImage = document.rootVisualElement.Q<VisualElement>("ItemImage");
        itemDisplay.style.display = DisplayStyle.None;
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

    private void OnNextSceneClick()
    {
        nextSceneButton.style.display = DisplayStyle.None;
        stateMachine.SwitchState(StateMachine.GameStateType.NewScene);
    }

    private void OnDisable()
    {
        continueButton.clicked -= OnContinueClick;
        nextSceneButton.clicked -= OnNextSceneClick;
    }

    public void ShowChosenItem(string name, Texture2D image)
    {
        itemName.text = name;
        itemImage.style.backgroundImage = image;
    }
}
