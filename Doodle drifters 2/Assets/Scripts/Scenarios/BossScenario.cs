using UnityEngine;

public class BossScenario : BaseScenario
{
    public override string scenarioSpecificPrompt => 
    "Generate an atmospheric description of am imposing boss room in a dungeon." +
    "Generate a unique and challenging boss for the players to face." +
    "End with a clear weakness which the players can exploit to defeat the boss";

    public override void Generate()
    {
        SendLLMRequest(scenarioSpecificPrompt);
    }
}
