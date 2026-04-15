using UnityEngine;

public class SolveScenario : BaseScenario
{
    public override string scenarioSpecificPrompt => 
    "The players have successfully solved the scenario." + 
    "Describe how they used their chosen object to overcome the challenge";

    public override void Generate()
    {
        SendLLMRequest(scenarioSpecificPrompt);
    }
}
