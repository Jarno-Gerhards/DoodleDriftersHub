using LLMUnity;
using UnityEngine;

public class FailScenario : BaseScenario
{
    [TextArea(5, 10), Chat, SerializeField]
    public string scenarioSpecificPrompt;
    // Backup prompt in case the inspector field is empty which sometimes happens after code changes and recompilation.
    private string backupPrompt = 
    "The players have failed the scenario. Describe how they used their chosen object and how it failed to solve the challenge";
    public override string GetPrompt()
    {
        return scenarioSpecificPrompt ?? backupPrompt;
    }
}
