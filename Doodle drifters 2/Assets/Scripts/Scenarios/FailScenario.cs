using UnityEngine;

public class FailScenario : BaseScenario
{
    public override string scenarioSpecificPrompt => 
    "The players have failed the scenario." +
    "Describe how they used their chosen object and how it failed to solve the challenge";

    public override void Generate()
    {
        SendLLMRequest(scenarioSpecificPrompt);
    }
}
