using UnityEngine;

/// <summary>
/// Data class that holds everything belonging to one player's solution submission.
/// Created by SolutionSubmitBridge and stored in VotingManager.
/// </summary>
[System.Serializable]
public class SolutionSubmission
{
    /// <summary>Unique identifier for this player (e.g. device ID or network ID).</summary>
    public string PlayerId { get; set; }

    /// <summary>Display name of the player.</summary>
    public string PlayerName { get; set; }

    /// <summary>The drawn solution texture.</summary>
    public Texture2D Texture { get; set; }

    /// <summary>Short description the player gave their drawing.</summary>
    public string Description { get; set; }

    /// <summary>How many votes this submission received.</summary>
    public int VoteCount { get; set; } = 0;

    public SolutionSubmission(string playerId, string playerName,
                               Texture2D texture, string description)
    {
        PlayerId    = playerId;
        PlayerName  = playerName;
        Texture     = texture;
        Description = description;
    }
}
