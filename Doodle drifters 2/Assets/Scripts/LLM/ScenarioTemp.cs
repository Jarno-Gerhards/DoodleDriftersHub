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
    [TextArea(1, 10), Chat, SerializeField]
    public string SolutionItem;
    private string SolutionPrompt;
    public TTS tts;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        text = uiDocument.rootVisualElement.Q<Label>("StoryText");
        WarmUpAgent();
    }

    async private void WarmUpAgent()
    {
        await DungeonMaster.Warmup(WarmUpNotification);
    }

    async public void GenerateScene()
    {
        text.text = "Generating scene...";
        // The line below causes the reply to be shown as it is being generated
        string reply = await DungeonMaster.Chat(GenerationPrompt, ShowTextOverTime);
        tts.SpeakText(reply);

        // The lines below cause the reply to be shown only after it has been fully generated
        // string reply = await DungeonMaster.Chat(GenerationPrompt);
        // text.text = reply;
    }

    async public void GenerateSolution()
    {
        text.text = "Generating solution...";
        SolutionPrompt = $"The players use the item: {SolutionItem}. Solve the challenge using the item. The scenario must be fully completed in this one turn. Keep it short, 2 to 3 sentences. Never use more than 700 characters.";
        // The line below causes the reply to be shown as it is being generated
        string reply = await DungeonMaster.Chat(SolutionPrompt, ShowTextOverTime);
        tts.SpeakText(reply);
        
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
