using UnityEngine;

/// <summary>
/// Game configuration constants. Equivalent to GameConfig.js.
/// Attach to a GameObject or use as static reference.
/// Values can be overridden via Unity Inspector or ScriptableObject.
/// </summary>
[CreateAssetMenu(fileName = "GameConfig", menuName = "DoodleDrifters/GameConfig")]
public class GameConfig : ScriptableObject
{
    [Header("Player Limits")]
    public int MinPlayers = 3;
    public int MaxPlayers = 6;

    [Header("Round Settings")]
    public int MaxRounds = 2;

    [Header("Timer Settings (seconds)")]
    public int DrawingTimerSeconds = 60;
    public int VotingTimerSeconds = 60;

    [Header("Narrative Duration (seconds)")]
    public float RoundStartNarrativeDuration = 3f;
    public float RacingNarrativeDuration = 15f;
    public float DefaultNarrativeDuration = 5f;
    public float RacingAutoAdvanceDelay = 15f;

    [Header("UI Layout")]
    public int VotingGridImagesPerRow = 6;
    public int VotingGridSpacing = 40;

    [Header("Animation Settings (seconds)")]
    public float NarrativeTypingSpeed = 0.03f;
    public float PositionAnimationDuration = 2f;
    public float VoteResultsDisplayTime = 5f;
    public float VoteCountAnimationInterval = 0.3f;

    [Header("Feature Flags")]
    public bool DrawOwnCar = false;
    public bool AllowReconnect = true;

    [Header("Connection Settings (seconds)")]
    public float PingInterval = 2f;
    public float PingTimeout = 5f;

    [Header("Logging")]
    public bool LogImages = true;

    [Header("Networking")]
    public int ServerPort = 7777;

    [Header("OpenAI")]
    public string OpenAIApiKey = "";
    public string OpenAIModel = "gpt-4o";

    // Singleton for easy access
    private static GameConfig _instance;
    public static GameConfig Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<GameConfig>("GameConfig");
                if (_instance == null)
                {
                    Debug.LogWarning("GameConfig not found in Resources. Using defaults.");
                    _instance = CreateInstance<GameConfig>();
                }
            }
            return _instance;
        }
    }
}
