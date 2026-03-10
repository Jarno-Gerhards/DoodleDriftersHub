using System;
using System.Collections.Generic;

/// <summary>
/// Network message definitions for communication between host and clients.
/// All messages are serialized as JSON with a "type" field and typed payload.
/// Replaces Socket.IO events.
/// </summary>
[Serializable]
public class NetworkMessage
{
    public string type;
    public string data; // JSON-serialized payload

    public NetworkMessage() { }

    public NetworkMessage(string type, object payload = null)
    {
        this.type = type;
        this.data = payload != null ? UnityEngine.JsonUtility.ToJson(payload) : "{}";
    }

    /// <summary>
    /// Serialize a message with complex data (uses Newtonsoft-style manual JSON for dictionaries).
    /// For simple serializable objects, use the constructor above.
    /// </summary>
    public static string Serialize(string type, string jsonData = "{}")
    {
        // Manual JSON construction to avoid JsonUtility limitations with dictionaries
        return $"{{\"type\":\"{type}\",\"data\":{jsonData}}}";
    }

    public T GetPayload<T>()
    {
        return UnityEngine.JsonUtility.FromJson<T>(data);
    }
}

// ============================================================
// Messages from CLIENT -> HOST
// ============================================================

[Serializable]
public class JoinRoomRequest
{
    public string roomId;
    public string playerId;
    public string playerName;
    public bool isHost;
}

[Serializable]
public class StartGameRequest
{
    public string roomId;
}

[Serializable]
public class SubmitDrawingRequest
{
    public string roomId;
    public string playerId;
    public string drawingBase64;
}

[Serializable]
public class SubmitCarDrawingRequest
{
    public string roomId;
    public string playerId;
    public string drawingBase64;
}

[Serializable]
public class SubmitVoteRequest
{
    public string roomId;
    public string voterId;
    public string targetPlayerId;
}

[Serializable]
public class LeaveRoomRequest
{
    public string roomId;
    public string playerId;
}

// ============================================================
// Messages from HOST -> CLIENT
// ============================================================

[Serializable]
public class JoinRoomResponse
{
    public bool success;
    public string error;
    public string roomStateJson; // Serialized RoomStateSnapshot
    public bool isReconnect;
}

[Serializable]
public class GenericResponse
{
    public bool success;
    public string error;
}

[Serializable]
public class PlayerJoinedMessage
{
    public string id;
    public string name;
    public int carId;
    public int playerCount;
    public int minPlayers;
    public int maxPlayers;
    public string carDrawingBase64;
}

[Serializable]
public class PlayerLeftMessage
{
    public string playerId;
    public string playerName;
    public string newLeaderId;
    public int playerCount;
    public int minPlayers;
    public int maxPlayers;
}

[Serializable]
public class PlayerConnectionChangedMessage
{
    public string playerId;
    public bool connected;
}

[Serializable]
public class GameStartedMessage
{
    public int round;
    public string status;
    public string positionsJson; // Serialized positions
}

[Serializable]
public class RoundStartedMessage
{
    public int round;
    public string positionsJson;
}

[Serializable]
public class PlayerSubmittedMessage
{
    public string playerId;
}

[Serializable]
public class PlayerVotedMessage
{
    public int votedCount;
    public int totalVoters;
}

[Serializable]
public class TimerStartMessage
{
    public string phase;
    public int seconds;
}

[Serializable]
public class UpdateStateMessage
{
    public string status;
    public int round;
    public string drawingsJson; // Serialized drawing list for voting
    public string positionsJson; // Serialized positions
    public string historyJson; // Serialized narrative history
    public string votingResultsJson; // Serialized voting results
    public string playerNamesJson; // Serialized player name map
    public int votedCount;
}

[Serializable]
public class DrawCarMessage
{
    public string message;
}
