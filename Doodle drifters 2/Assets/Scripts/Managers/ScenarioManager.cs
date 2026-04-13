using UnityEngine;

public class ScenarioManager : MonoBehaviour
{
    private string personalityPrompt;
    private string scenarioPrompt;
    private string judgePrompt;
    private string solvePrompt;
    private string failPrompt;
    private string adjustPrompt;
    private string bossPrompt;
    private string previousScenario;
    private int roomCount;
    private int roomFailureCount;
    private int requestFailureCount;
    private string fallbackText;

    void Start()
    {
        //
        // Initialize the LLM and such
        //
    }

    private void SendLLMRequest(string prompt) // Sends a request to the LLM and returns the response
    {
        
    }

    public void StartNewScenario() // Called when the player enters a new room
    {
        
    }

    public void JudgeObject() // Judges the chosen object and determines if it solves or fails the scenario
    {
        
    }

    public void SolveScenario() // Called when the player successfully solves the scenario
    {
        
    }

    public void FailScenario() // Called when the player fails the scenario
    {
        
    }

    public void AdjustScenario() // When the player fails a scenario, the LLM will adjust the scenario to give players a second chance
    {
        
    }

    public void BossScenario() // After X amount of rooms the players will face a boss
    {
        
    }

    private void FallbackIntermission() // Called when the LLM fails to generate a response
    {
        
    }

    private void CriticalFailure() // Called when the LLM fails multiple times in a row and forcefully ends the game
    {
        
    }


}
