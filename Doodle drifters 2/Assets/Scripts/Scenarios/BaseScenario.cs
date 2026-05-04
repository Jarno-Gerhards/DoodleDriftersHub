using UnityEngine;

public abstract class BaseScenario : MonoBehaviour
{
    private const string personalityPrompt = 
    "You are the Game Master for a fantasy game in which players navigate a dangerous dungeon." + 
    "You must be creative, atmospheric and engaging in your descriptions." +
    "You will keep it short (around 3 sentences) and make sure it is easy to understand when spoken aloud by a text-to-speech system.";

    public abstract string scenarioSpecificPrompt { get; } 


    private string previousScenario;
    private int roomCount;
    private int roomFailureCount;
    private int requestFailureCount;
    private const string fallbackText = 
    "While moving to the next room, you find a quiet area and decide to take a moment to rest"; // Plays when the LLM fails to generate a response
    private const string criticalFailureText =
    "The dungeon has become unstable and collapses around you, forcing you to flee for your life. You barely escape with your life, but the dungeon is lost forever."; // Plays when the LLM fails multiple times in a row

    // private const string judgePrompt = 
    // "The players have chosen a single object which they will use to solve the scenario" +
    // "Based on the scenario and the chosen object, determine if the players have solved the scenario or failed it.";


    void Start()
    {
        //
        // Initialize the LLM and such
        //
    }

    protected void SendLLMRequest(string prompt) // Sends a request to the LLM and returns the response
    {
        // Use the personalityPrompt and the specific prompt to generate a response from the LLM
    }

    public abstract void Generate();

    private void FallbackIntermission() // Called when the LLM fails to generate a response
    {
        print(fallbackText);
    }

    private void CriticalFailure() // Called when the LLM fails multiple times in a row and forcefully ends the game
    {
        print(criticalFailureText);
    }

    // public void JudgeObject() // Judges the chosen object and determines if it solves or fails the scenario
    // {
    //     SendLLMRequest(judgePrompt);
    // }
}
