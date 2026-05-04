using LLMUnity;
using UnityEngine;

public class SolveScenario : BaseScenario
{
    [TextArea(5, 10), Chat, SerializeField]
    public string scenarioSpecificPrompt;
    // Backup prompt in case the inspector field is empty which sometimes happens after code changes and recompilation.
    private string backupPrompt = 
    ", is the Item the players have chosen. Solve the challenge using the item. The scenario must be fully completed in this one turn. Keep it short, 2 to 3 sentences. Never use more than 700 characters.";
    public override string GetPrompt()
    {
        return scenarioSpecificPrompt ?? backupPrompt;
    }
}
