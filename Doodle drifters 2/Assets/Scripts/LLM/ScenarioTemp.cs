using System.Threading.Tasks;
using System.Xml.Serialization;
using LLMUnity;
using UnityEngine;

public class ScenarioTemp : MonoBehaviour
{
    [SerializeField] private LLMAgent agent;
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
        string reply = await agent.Chat("Give me a description of a room in a dungeon, in 2-3 sentences.");
        Debug.Log("Agent response: " + reply);
    }
}
