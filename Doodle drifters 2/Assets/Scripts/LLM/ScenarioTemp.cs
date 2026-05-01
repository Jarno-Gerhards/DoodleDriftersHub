using System.Threading.Tasks;
using System.Xml.Serialization;
using LLMUnity;
using TMPro;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.UIElements;

public class ScenarioTemp : MonoBehaviour
{
    [SerializeField] private LLMAgent DungeonMaster;
    public UIDocument uiDocument;
    private Label text;
    [TextArea(5, 10), Chat, SerializeField]
    public string GenerationPrompt;
    [TextArea(5, 10), Chat, SerializeField]
    public string SolutionPrompt;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        text = uiDocument.rootVisualElement.Q<Label>("StoryText");
        WarmUpAgent();
    }

    async private void WarmUpAgent()
    {
        DungeonMaster.temperature = 1.1f;
        DungeonMaster.topP = 0.92f;
        DungeonMaster.repeatPenalty = 1.15f;
        DungeonMaster.mirostat = 2;
        DungeonMaster.mirostatEta = 0.1f;
        DungeonMaster.mirostatTau = 5.0f;
        await DungeonMaster.Warmup(WarmUpNotification);
    }

    async public void GenerateScene()
    {
        text.text = "Generating scene...";
        // The line below causes the reply to be shown as it is being generated
        string reply = await DungeonMaster.Chat(GenerationPrompt, ShowTextOverTime);

        // The lines below cause the reply to be shown only after it has been fully generated
        //string reply = await DungeonMaster.Chat("Give me a description of a room in a dungeon, in 2-3 sentences.");
        //text.text = reply;
    }

    async public void GenerateSolution()
    {
        text.text = "Generating solution...";
        // The line below causes the reply to be shown as it is being generated
        string reply = await DungeonMaster.Chat(SolutionPrompt, ShowTextOverTime);

        // The lines below cause the reply to be shown only after it has been fully generated
        //string reply = await DungeonMaster.Chat("What is a good solution to the problem described in the scene?");
        //text.text = reply;
    }

    private void ShowTextOverTime(string reply)
    {
        text.text = reply;
    }

    private void WarmUpNotification()
    {
        Debug.Log("Warmup complete!");
    }    
}
