using UnityEngine;

public class AdjustedScenario : BaseScenario
{
    public override string scenarioSpecificPrompt => 
    "The players have failed the scenario." +
    "Make adjustments to the scenario based on their failure and give them a second chance" +
    "End with a clear new challenge which the players must overcome";

    public override void Generate()
    {
        SendLLMRequest(scenarioSpecificPrompt);
    }
}
