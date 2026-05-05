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

    private void Awake()
    {
        text = uiDocument.rootVisualElement.Q<Label>("StoryText");
    }

    void Start()
    {
        InitializeScenarios();
        WarmUpAgent();
    }

    private void InitializeScenarios()
    {
        scenarios.Add(ScenarioType.New, gameObject.GetComponent<NewScenario>());
        scenarios.Add(ScenarioType.Solve, gameObject.GetComponent<SolveScenario>());
        scenarios.Add(ScenarioType.Fail, gameObject.GetComponent<FailScenario>());
        scenarios.Add(ScenarioType.Adjust, gameObject.GetComponent<AdjustedScenario>());
        scenarios.Add(ScenarioType.Boss, gameObject.GetComponent<BossScenario>());
    }

    async private void WarmUpAgent()
    {
        await DungeonMaster.Warmup(WarmUpNotification);
    }

        private void WarmUpNotification()
    {
        Debug.Log("Warmup complete!");
    }    

    async public void GenerateNewScene()
    {
        text.text = "Generating scene...";
        await Task.Delay(1000); // Optional: Add a short delay to ensure the "Generating scene..." message is visible before the new text starts appearing
        // The line below causes the reply to be shown as it is being generated
        string reply = await DungeonMaster.Chat(scenarios[ScenarioType.New].GetPrompt(), ShowTextOverTime);
        tts.SpeakText(reply);

        // The lines below cause the reply to be shown only after it has been fully generated
        // string reply = await DungeonMaster.Chat(scenarios[ScenarioType.New].GetPrompt());
        // text.text = reply;
    }

    async public void GenerateSolution()
    {
        text.text = "Generating solution...";
        // The line below causes the reply to be shown as it is being generated
        string reply = await DungeonMaster.Chat(SolutionItem + scenarios[ScenarioType.Solve].GetPrompt(), ShowTextOverTime);
        tts.SpeakText(reply);
        
        // The lines below cause the reply to be shown only after it has been fully generated
        //string reply = await DungeonMaster.Chat(SolutionItem + scenarios[ScenarioType.Solve].GetPrompt());
        //text.text = reply;
    }

    async public void GenerateFailure()
    {
        text.text = "Generating failure...";
        // The line below causes the reply to be shown as it is being generated
        string reply = await DungeonMaster.Chat(SolutionItem + scenarios[ScenarioType.Fail].GetPrompt(), ShowTextOverTime);
        tts.SpeakText(reply);
        
        // The lines below cause the reply to be shown only after it has been fully generated
        //string reply = await DungeonMaster.Chat(SolutionItem + scenarios[ScenarioType.Fail].GetPrompt());
        //text.text = reply;
    }

    async public void GenerateAdjustedScene()
    {
        text.text = "Generating adjusted scene...";
        // The line below causes the reply to be shown as it is being generated
        string reply = await DungeonMaster.Chat(scenarios[ScenarioType.Adjust].GetPrompt(), ShowTextOverTime);
        tts.SpeakText(reply);

        // The lines below cause the reply to be shown only after it has been fully generated
        // string reply = await DungeonMaster.Chat(scenarios[ScenarioType.Adjust].GetPrompt());
        // text.text = reply;
    }

    async public void GenerateBossScene()
    {
        text.text = "Generating boss scene...";
        // The line below causes the reply to be shown as it is being generated
        string reply = await DungeonMaster.Chat(scenarios[ScenarioType.Boss].GetPrompt(), ShowTextOverTime);
        tts.SpeakText(reply);

        // The lines below cause the reply to be shown only after it has been fully generated
        // string reply = await DungeonMaster.Chat(scenarios[ScenarioType.Boss].GetPrompt());
        // text.text = reply;
    }

    private void ShowTextOverTime(string reply)
    {
        text.text = reply;
    }
}
