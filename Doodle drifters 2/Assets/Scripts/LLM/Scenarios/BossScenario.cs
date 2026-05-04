using LLMUnity;
using UnityEngine;

public class BossScenario : BaseScenario
{
    [TextArea(5, 10), Chat, SerializeField]
    public string scenarioSpecificPrompt;
    // Backup prompt in case the inspector field is empty which sometimes happens after code changes and recompilation.
    private string backupPrompt =
    "Generate an atmospheric description of am imposing boss room in a dungeon. Generate a unique and challenging boss for the players to face. End with a clear weakness which the players can exploit to defeat the boss";
    public override string GetPrompt()
    {
        return scenarioSpecificPrompt ?? backupPrompt;
    }
}
