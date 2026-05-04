using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Serialization;
using LLMUnity;
using TMPro;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.UIElements;

public class ScenarioManager : MonoBehaviour
{
    [SerializeField] private LLMAgent DungeonMaster;
    public UIDocument uiDocument;
    private Label text;
    [TextArea(1, 10), Chat, SerializeField]
    public string SolutionItem;
    public TTS tts;
    private Dictionary<ScenarioType, BaseScenario> scenarios = new Dictionary<ScenarioType, BaseScenario>();
    private enum ScenarioType
    {
        New,
        Solve,
        Fail,
        Adjust,
        Boss
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        InitializeScenarios();
        WarmUpAgent();
        text = uiDocument.rootVisualElement.Q<Label>("StoryText");
    }

    async private void WarmUpAgent()
    {
        await DungeonMaster.Warmup(WarmUpNotification);
    }

    async public void GenerateScene()
    {
        text.text = "Generating scene...";
        // The line below causes the reply to be shown as it is being generated
        string reply = await DungeonMaster.Chat(scenarios[ScenarioType.New].GetPrompt(), ShowTextOverTime);
        tts.SpeakText(reply);

        // The lines below cause the reply to be shown only after it has been fully generated
        // string reply = await DungeonMaster.Chat(GenerationPrompt);
        // text.text = reply;
    }

    async public void GenerateSolution()
    {
        text.text = "Generating solution...";
        // The line below causes the reply to be shown as it is being generated
        string reply = await DungeonMaster.Chat(SolutionItem + scenarios[ScenarioType.Solve].GetPrompt(), ShowTextOverTime);
        tts.SpeakText(reply);
        
        // The lines below cause the reply to be shown only after it has been fully generated
        //string reply = await DungeonMaster.Chat("What is a good solution to the problem described in the scene?");
        //text.text = reply;
    }

    private void ShowTextOverTime(string reply)
    {
        text.text = reply;
    }

    private void InitializeScenarios()
    {
        scenarios.Add(ScenarioType.New, gameObject.GetComponent<NewScenario>());
        scenarios.Add(ScenarioType.Solve, gameObject.GetComponent<SolveScenario>());
        scenarios.Add(ScenarioType.Fail, gameObject.GetComponent<FailScenario>());
        scenarios.Add(ScenarioType.Adjust, gameObject.GetComponent<AdjustedScenario>());
        scenarios.Add(ScenarioType.Boss, gameObject.GetComponent<BossScenario>());
    }

    private void WarmUpNotification()
    {
        Debug.Log("Warmup complete!");
    }    
}
