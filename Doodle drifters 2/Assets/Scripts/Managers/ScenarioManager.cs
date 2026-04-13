using UnityEngine;

public class ScenarioManager : MonoBehaviour
{
    private const string personalityPrompt = 
    "You are the Game Master for a fantasy game in which players navigate a dangerous dungeon." + 
    "You must be creative, atmospheric and engaging in your descriptions." +
    "You will keep it short (around 3 sentences) and make sure it is easy to understand when spoken aloud by a text-to-speech system.";
    private const string scenarioPrompt = 
    "Generate an atmospheric description of a room in a dungeon." +
    "End with a clear challenge which the players must overcome";
    private const string judgePrompt = 
    "The players have chosen a single object which they will use to solve the scenario" +
    "Based on the scenario and the chosen object, determine if the players have solved the scenario or failed it.";
    private const string solvePrompt = 
    "The players have successfully solved the scenario." + 
    "Describe how they used their chosen object to overcome the challenge";
    private const string failPrompt = 
    "The players have failed the scenario." +
    "Describe how they used their chosen object and how it failed to solve the challenge";
    private const string adjustPrompt = 
    "The players have failed the scenario." +
    "Make adjustments to the scenario based on their failure and give them a second chance" +
    "End with a clear new challenge which the players must overcome";
    private const string bossPrompt = 
    "Generate an atmospheric description of am imposing boss room in a dungeon." +
    "Generate a unique and challenging boss for the players to face." +
    "End with a clear weakness which the players can exploit to defeat the boss";
    private string previousScenario;
    private int roomCount;
    private int roomFailureCount;
    private int requestFailureCount;
    private const string fallbackText = 
    "While moving to the next room, you find a quiet area and decide to take a moment to rest"; // Plays when the LLM fails to generate a response
    private const string criticalFailureText =
    "The dungeon has become unstable and collapses around you, forcing you to flee for your life. You barely escape with your life, but the dungeon is lost forever."; // Plays when the LLM fails multiple times in a row

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
        SendLLMRequest(scenarioPrompt);
    }

    public void JudgeObject() // Judges the chosen object and determines if it solves or fails the scenario
    {
        SendLLMRequest(judgePrompt);
    }

    public void SolveScenario() // Called when the player successfully solves the scenario
    {
        SendLLMRequest(solvePrompt);
    }

    public void FailScenario() // Called when the player fails the scenario
    {
        SendLLMRequest(failPrompt);
    }

    public void AdjustScenario() // When the player fails a scenario, the LLM will adjust the scenario to give players a second chance
    {
        SendLLMRequest(adjustPrompt);
    }

    public void BossScenario() // After X amount of rooms the players will face a boss
    {
        SendLLMRequest(bossPrompt);
    }

    private void FallbackIntermission() // Called when the LLM fails to generate a response
    {
        print(fallbackText);
    }

    private void CriticalFailure() // Called when the LLM fails multiple times in a row and forcefully ends the game
    {
        print(criticalFailureText);
    }
}
