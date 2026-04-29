using System.Threading.Tasks;
using System.Xml.Serialization;
using LLMUnity;
using TMPro;
using Unity.AppUI.UI;
using UnityEngine;

public class ScenarioTemp : MonoBehaviour
{
    [SerializeField] private LLMAgent agent;
    [SerializeField] private TextMeshProUGUI text;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        test();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    async private void test()
    {
        text.text = "Generating scene...";
        // The line below causes the reply to be shown as it is being generated
        string reply = await agent.Chat("Give me a description of a room in a dungeon. End with a clear objective for players to overcome. Only use 2-3 sentences.", ShowTextOverTime);

        // The lines below cause the reply to be shown only after it has been fully generated
        //string reply = await agent.Chat("Give me a description of a room in a dungeon, in 2-3 sentences.");
        //text.text = reply;
    }

    private void ShowTextOverTime(string reply)
    {
        text.text = reply;
    }
}
