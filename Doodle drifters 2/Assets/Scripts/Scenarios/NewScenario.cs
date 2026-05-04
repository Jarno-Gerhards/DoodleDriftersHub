using UnityEngine;

public class NewScenario : BaseScenario
{
    public override string scenarioSpecificPrompt => 
    "Generate an atmospheric description of a room in a dungeon." +
    "End with a clear challenge which the players must overcome";

    public override void Generate()
    {
        SendLLMRequest(scenarioSpecificPrompt);
    }
}
