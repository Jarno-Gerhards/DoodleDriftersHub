using LLMUnity;
using UnityEngine;

public class AdjustedScenario : BaseScenario
{
    
    [TextArea(5, 10), Chat, SerializeField]
    public string scenarioSpecificPrompt;
    // Backup prompt in case the inspector field is empty which sometimes happens after code changes and recompilation.
    private string backupPrompt =
    "The players have failed the scenario. Make adjustments to the scenario based on their failure and give them a second chance. End with a clear new challenge which the players must overcome";

    public override string GetPrompt()
    {
        return scenarioSpecificPrompt ?? backupPrompt;
    }
}
