using System;

/// <summary>
/// Player data model. Shared between server and client.
/// </summary>
[Serializable]
public class PlayerData
{
    public string Id;
    public string Name;
    public int Score;
    public int CarId;
    public bool IsHost;
    public bool IsLeader;
    public bool Connected;
    public string CarDrawingBase64; // Custom car drawing (if DrawOwnCar enabled)

    public PlayerData()
    {
        Connected = true;
    }

    public PlayerData(string id, string name, bool isHost = false)
    {
        Id = id;
        Name = name;
        IsHost = isHost;
        Score = 0;
        CarId = 1;
        IsLeader = false;
        Connected = true;
    }
}

/// <summary>
/// Position snapshot of a player at a given round.
/// </summary>
[Serializable]
public class PlayerPositionSnapshot
{
    public string Id;
    public string Name;
    public int CarId;
    public int Score;
    public string Item;
    public bool Crashed;
    public int VotesInLastRound;

    public PlayerPositionSnapshot() { }

    public PlayerPositionSnapshot(PlayerData player, int score, string item, int votes)
    {
        Id = player.Id;
        Name = player.Name;
        CarId = player.CarId;
        Score = score;
        Item = item ?? "nothing";
        Crashed = false;
        VotesInLastRound = votes;
    }
}

/// <summary>
/// Drawing submission data.
/// </summary>
[Serializable]
public class DrawingData
{
    public string PlayerId;
    public string ImageBase64;
    public string Item;

    public DrawingData() { }

    public DrawingData(string playerId, string imageBase64, string item = "Unknown Object")
    {
        PlayerId = playerId;
        ImageBase64 = imageBase64;
        Item = item;
    }
}

/// <summary>
/// Voting result for a single player.
/// </summary>
[Serializable]
public class VotingResult
{
    public string PlayerId;
    public int VotesReceived;
    public int PreviousScore;
    public int NewScore;
}
