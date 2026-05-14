using LLMUnity;
using UnityEngine;

public class NewScenario : BaseScenario
{
    [TextArea(5, 10), Chat, SerializeField]
    public string scenarioSpecificPrompt;
    // Backup prompt in case the inspector field is empty which sometimes happens after code changes and recompilation.
    private string backupPrompt =
    "Generate an atmospheric description of a room in a dungeon. End with a clear obstacle the players must overcome. Keep it short, 2 to 3 sentences. Never use more than 700 characters.";
    public override string GetPrompt()
    {
        return scenarioSpecificPrompt ?? backupPrompt;
    }
}
